using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.Forms;
using RTextNppPlugin.RText;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.Utilities.WpfControlHost;
using RTextNppPlugin.WpfControls;

namespace RTextNppPlugin
{
    partial class Plugin
    {
        #region [Fields]
        private static INpp _nppHelper                                                  = Npp.Instance;
        private static IWin32 _win32                                                    = new Win32();
        private static ISettings _settings                                              = new Settings(_nppHelper);
        private static StyleConfigurationObserver _styleObserver                        = new StyleConfigurationObserver(_nppHelper);
        private static ConnectorManager _connectorManager                               = new ConnectorManager(_settings, _nppHelper);
        private static PersistentWpfControlHost<ConsoleOutputForm> _consoleOutput       = new PersistentWpfControlHost<ConsoleOutputForm>(Settings.RTextNppSettings.ConsoleWindowActive, new ConsoleOutputForm(_connectorManager, _nppHelper, _styleObserver), _settings, _nppHelper);
        private static Options _options                                                 = new Options(_settings);
        private static FileModificationObserver _fileObserver                           = new FileModificationObserver(_settings, _nppHelper);
        private static Dictionary<ShortcutKey, Tuple<string, Action>> internalShortcuts = new Dictionary<ShortcutKey, Tuple<string, Action>>();
        private static AutoCompletionWindow _autoCompletionForm                         = new AutoCompletionWindow(_connectorManager, _win32, _nppHelper);
        private static Bitmap tbBmp                                                     = Properties.Resources.ConsoleIcon;
        private static Bitmap tbBmp_tbTab                                               = Properties.Resources.ConsoleIcon;
        private static Icon tbIcon                                                      = null;
        private static bool _consoleInitialized                                         = false;
        private static bool _invokeInProgress                                           = false;
        private static int _currentZoomLevel                                            = 0;
        private static ScintillaMessageInterceptor _scintillaMainMsgInterceptor         = null;  //!< Intercepts scintilla messages.
        private static ScintillaMessageInterceptor _scintillaSecondMsgInterceptor       = null;  //!< Intercepts scintilla messages from second scintilla handle.
        private static NotepadMessageInterceptor _nppMsgInterceptpr                     = null;  //!< Intercepts notepad ++ messages.
        private static bool _hasMainScintillaFocus                                      = false; //!< Indicates if the main editor has focus.
        private static bool _hasSecondScintillaFocus                                    = false; //!< Indicates if the second editor has focus.
        private static bool _isMenuLoopInactive                                         = false; //!< Indicates that npp menu loop is active.        
        private static LinkTargetsWindow _linkTargetsWindow                             = new LinkTargetsWindow(_nppHelper, _win32, _settings, _connectorManager, _styleObserver); //!< Display reference links.
        #endregion

        #region [Startup/CleanUp]

        static internal void CommandMenuInit()
        {
            CSScriptIntellisense.KeyInterceptor.Instance.Install();

            SetCommand((int)Constants.NppMenuCommands.ConsoleWindow, Properties.Resources.RTEXT_SHOW_OUTPUT_WINDOW, ShowConsoleOutput, new ShortcutKey(false, true, true, Keys.R));
            SetCommand((int)Constants.NppMenuCommands.Options, Properties.Resources.RTEXT_SHOW_OPTIONS_WINDOW, ModifyOptions, new ShortcutKey(true, false, true, Keys.R));
            SetCommand((int)Constants.NppMenuCommands.AutoCompletion, Properties.Resources.SHOW_AUTO_COMPLETION_LIST_NAME, StartAutoCompleteSession, "Ctrl+Space");

            _connectorManager.Initialize(nppData);
            foreach(var key in BindInteranalShortcuts())
            {
                CSScriptIntellisense.KeyInterceptor.Instance.Add(key);
            }

            CSScriptIntellisense.KeyInterceptor.Instance.KeyDown += OnKeyInterceptorKeyDown;
            CSScriptIntellisense.KeyInterceptor.Instance.KeyUp   += OnKeyInterceptorKeyUp;
            
            foreach(var key in Enum.GetValues(typeof(Keys)))
            {
                CSScriptIntellisense.KeyInterceptor.Instance.Add((Keys)key);
            }
            _currentZoomLevel = Npp.Instance.GetZoomLevel();
            _autoCompletionForm.OnZoomLevelChanged(_currentZoomLevel);
            _linkTargetsWindow.OnZoomLevelChanged(_currentZoomLevel);
            _linkTargetsWindow.IsVisibleChanged += OnLinkTargetsWindowIsVisibleChanged;
            #if DEBUG
            Debugger.Launch();
            _styleObserver.EnableStylesObservation();
            #endif
        }
       
        internal static void OnBufferActivated()
        {
            _linkTargetsWindow.CancelPendingRequest();
        }

        static void OnKeyInterceptorKeyUp(Keys key, int repeatCount, ref bool handled)
        {
            CSScriptIntellisense.Modifiers modifiers = CSScriptIntellisense.KeyInterceptor.GetModifiers();
            if (!modifiers.IsAlt || !modifiers.IsCtrl)
            {                
                _linkTargetsWindow.IsKeyboardShortCutActive(false);
            }
        }

        static void OnKeyInterceptorKeyDown(Keys key, int repeatCount, ref bool handled)
        {
            if (FileUtilities.IsRTextFile(_settings, Npp.Instance) && (Npp.Instance.GetSelections() == 1) && HasScintillaFocus() && !_isMenuLoopInactive)
            {
                CSScriptIntellisense.Modifiers modifiers = CSScriptIntellisense.KeyInterceptor.GetModifiers();                
                if(modifiers.IsAlt && modifiers.IsCtrl)
                {                    
                    _linkTargetsWindow.IsKeyboardShortCutActive(true);
                    handled = true;
                }
                foreach (var shortcut in internalShortcuts.Keys)
                {
                    if ((byte)key == shortcut._key)
                    {                       
                        if (modifiers.IsCtrl == shortcut.IsCtrl && modifiers.IsShift == shortcut.IsShift && modifiers.IsAlt == shortcut.IsAlt)
                        {
                            handled = true;
                            var handler = internalShortcuts[shortcut];
                            handled = !_autoCompletionForm.IsVisible;
                            if (!_autoCompletionForm.IsVisible)
                            {
                                var res = AsyncInvoke(handler.Item2);
                            }
                            //do nothing if form is already visible
                            return;
                        }
                    }
                }
                //if any modifier key is pressed - ignore this key press
                if (modifiers.IsCtrl || modifiers.IsAlt)
                {
                    if(_autoCompletionForm.IsVisible)
                    {
                        CommitAutoCompletion(false);
                    }
                    return;
                }

                //auto complete Ctrl+Space is handled above - here we handle other special cases
                switch (key)
                {
                    case Keys.Back:
                        handled = true;
                        Npp.Instance.DeleteBack(1);
                        //in case of empty trigger token form needs to close
                        _autoCompletionForm.OnKeyPressed(Constants.BACKSPACE);
                        if(_autoCompletionForm.CharProcessAction == ViewModels.AutoCompletionViewModel.CharProcessResult.ForceClose)
                        {
                            CommitAutoCompletion(false);
                        }                        
                        break;
                    case Keys.Delete:
                        handled = true;
                        Npp.Instance.DeleteFront();
                        _autoCompletionForm.OnKeyPressed();
                        break;
                    case Keys.Space:                        
                        handled = true;
                        //if completion list is visible, and there is a trigger token other than comma or space and there is some selected option
                        if (_autoCompletionForm.Completion != null && _autoCompletionForm.Completion.IsSelected)
                        {
                            CommitAutoCompletion(true);
                            Npp.Instance.AddText(Constants.SPACE.ToString());
                            return;

                        }
                        Npp.Instance.AddText(Constants.SPACE.ToString());
                        _autoCompletionForm.OnKeyPressed(Constants.SPACE);                        
                        break;
                    case Keys.Return:
                    case Keys.Tab:
                        if (_autoCompletionForm.IsVisible )
                        {
                            handled = true;
                            if ( _autoCompletionForm.TriggerPoint.HasValue && String.IsNullOrWhiteSpace(_autoCompletionForm.TriggerPoint.Value.Context) && 
                                (_autoCompletionForm.Completion == null || (_autoCompletionForm.Completion != null && !_autoCompletionForm.Completion.IsSelected))
                               )
                            {
                                Npp.Instance.AddText(Constants.TAB.ToString());
                                _autoCompletionForm.OnKeyPressed(Constants.TAB);                                
                            }
                            else
                            {
                                //special case when trigger point is empty -> move auto completion form after inserting tab char..
                                CommitAutoCompletion(true);
                            }
                        }                                                
                        break;
                    case Keys.Oemcomma:
                        if(_autoCompletionForm.IsVisible)
                        {
                            CommitAutoCompletion(true);
                            Npp.Instance.AddText(Constants.COMMA.ToString());
                            handled = true;
                        }
                        //start auto completion fix window position - handle insertion when token is comma i.e. do not replace comma
                        OnCharTyped(Constants.COMMA);
                        break;
                    case Keys.Escape:
                    case Keys.Cancel:
                    case Keys.Left:
                    case Keys.Right:
                        CommitAutoCompletion(false);
                        break;                        
                    default:
                        //convert virtual key to ASCII
                        int nonVirtualKey = Npp.MapVirtualKey((uint)key, 2);
                        char mappedChar   = Npp.Instance.GetAsciiCharacter((int)key, nonVirtualKey);
                        if (mappedChar != default(char))
                        {
                            handled = true;
                            Npp.Instance.AddText(new string(mappedChar, 1));
                            OnCharTyped(mappedChar);
                        }
                        break;
                }
            }
            else
            {
                //allow event to fall through
                handled = false;
            }
        }

        public static void OnCharTyped(char c)
        {
            if (!Char.IsControl(c) && !Char.IsWhiteSpace(c))
            {
                if (!_autoCompletionForm.IsVisible)
                {
                    //do not start auto completion with whitespace char...
                    if (!Char.IsWhiteSpace(c))
                    {
                        var res = AsyncInvoke(StartAutoCompleteSession);
                    }
                }
                else
                {
                    _autoCompletionForm.OnKeyPressed(c);                    
                    if(_autoCompletionForm.CharProcessAction == ViewModels.AutoCompletionViewModel.CharProcessResult.ForceClose)
                    {
                        _autoCompletionForm.Hide();
                    }
                }
            }
        }

        private static void CommitAutoCompletion(bool replace)
        {
            if(replace && _autoCompletionForm.TriggerPoint != null)
            {
                //use current selected item to replace token
                if (_autoCompletionForm.Completion != null && _autoCompletionForm.Completion.IsSelected)
                {
                    Npp.Instance.ReplaceWordFromToken(_autoCompletionForm.TriggerPoint, _autoCompletionForm.Completion.InsertionText);
                }
            }
            _autoCompletionForm.Hide();
        }

        private static async Task AsyncInvoke(Action action)
        {
            if (!_invokeInProgress)
            {
                _invokeInProgress = true;
                await Task.Delay(10);
                action();
                _invokeInProgress = false;
            }
        }

        static internal void PluginCleanUp()
        {
            CSScriptIntellisense.KeyInterceptor.Instance.RemoveAll();
            _fileObserver.CleanBackup();
            _connectorManager.ReleaseConnectors();
            CSScriptIntellisense.KeyInterceptor.Instance.KeyDown -= OnKeyInterceptorKeyDown;
            CSScriptIntellisense.KeyInterceptor.Instance.KeyUp   -= OnKeyInterceptorKeyUp;
            _scintillaMainMsgInterceptor.ScintillaFocusChanged   -= OnMainScintillaFocusChanged;
            _scintillaSecondMsgInterceptor.ScintillaFocusChanged -= OnSecondScintillaFocusChanged;
            _scintillaMainMsgInterceptor.MouseWheelMoved         -= OnScintillaMouseWheelMoved;
            _scintillaSecondMsgInterceptor.MouseWheelMoved       -= OnScintillaMouseWheelMoved;
            _nppMsgInterceptpr.MenuLoopStateChanged              -= OnMenuLoopStateChanged;
            _linkTargetsWindow.IsVisibleChanged                  -= OnLinkTargetsWindowIsVisibleChanged;
        }
        #endregion

        #region [Commands]

        /**
         * Shows the automatic completion list.
         */
        static void StartAutoCompleteSession()
        {
            HandleErrors(() =>
            {
                if (FileUtilities.IsRTextFile(_settings, Npp.Instance))
                {
                    if (!_autoCompletionForm.IsVisible)
                    {
                        int aCurrentPosition = Npp.Instance.GetCaretPosition();

                        if (aCurrentPosition >= 0)
                        {
                            int aLineNumber = Npp.Instance.GetLineNumber();
                            //get text from start till current line end
                            string aContextBlock = Npp.Instance.GetTextBetween(0, Npp.Instance.GetLineEnd(aLineNumber));
                            ContextExtractor aExtractor = new ContextExtractor(aContextBlock, Npp.Instance.GetLengthToEndOfLine(Npp.Instance.GetColumn()));
                            //if auto completion is inside comment, notation, name, string just return
                            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(aLineNumber, aCurrentPosition, Npp.Instance);                            
                            //if a token is found then the window should appear at the start of it, else it should appear at the caret
                            Point aCaretPoint = Npp.Instance.GetCaretScreenLocationForForm();
                            if (aTokenizer.TriggerToken.HasValue && 
                                aTokenizer.TriggerToken.Value.Type  != RTextTokenTypes.Comma &&
                                aTokenizer.TriggerToken.Value.Type  != RTextTokenTypes.Space &&
                                (aTokenizer.TriggerToken.Value.Type != RTextTokenTypes.Label ||                                 
                                 aCurrentPosition < (aTokenizer.TriggerToken.Value.BufferPosition + aTokenizer.TriggerToken.Value.Context.Length)))
                            {
                                aCaretPoint = Npp.Instance.GetCaretScreenLocationRelativeToPosition(aTokenizer.TriggerToken.Value.BufferPosition);
                            }
                            _autoCompletionForm.Dispatcher.Invoke((MethodInvoker)(async() =>
                            {
                                _autoCompletionForm.Left = aCaretPoint.X;
                                _autoCompletionForm.Top  = aCaretPoint.Y;
                                Utilities.VisualUtilities.SetOwnerFromNppPlugin(_autoCompletionForm);
                                await _autoCompletionForm.AugmentAutoCompletion(aExtractor, aCaretPoint, aTokenizer);
                                switch (_autoCompletionForm.CharProcessAction)
                                {
                                    case ViewModels.AutoCompletionViewModel.CharProcessResult.ForceClose:
                                        return;
                                    case ViewModels.AutoCompletionViewModel.CharProcessResult.ForceCommit:
                                        CommitAutoCompletion(true);
                                        break;
                                    default:
                                        _autoCompletionForm.Show();
                                        break;
                                }
                            }));
                        }
                    }
                }
                else
                {
                    _win32.ISendMessage(Plugin.nppData._nppHandle, (NppMsg)WinMsg.WM_COMMAND, (int)NppMenuCmd.IDM_EDIT_AUTOCOMPLETE, 0);
                }
            });
        }        

        /**
         * \brief Modify options callback from plugin menu.           
         */
        static void ModifyOptions()
        {
            _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_MODELESSDIALOG, (int)NppMsg.MODELESSDIALOGADD, _options.Handle.ToInt32());
            if (_options.ShowDialog(Control.FromHandle(nppData._nppHandle)) == DialogResult.OK)
            {
                _options.SaveSettings();
            }
            else
            {
                _options.RestoreSettings();
            }
            _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_MODELESSDIALOG, (int)NppMsg.MODELESSDIALOGREMOVE, _options.Handle.ToInt32());
        }

        static void ShowConsoleOutput()
        {
            if (!_consoleInitialized)
            {
                _consoleInitialized = true;

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = _consoleOutput.Handle;
                _nppTbData.pszName = Properties.Resources.RTEXT_OUTPUT_WINDOW_CAPTION;
                _nppTbData.dlgID = (int)Constants.NppMenuCommands.ConsoleWindow;
                // define the default docking behaviour
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = Constants.PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);
                _consoleOutput.CmdId = _funcItems.Items[(int)Constants.NppMenuCommands.ConsoleWindow]._cmdID;
                _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
                _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK, _funcItems.Items[0]._cmdID, 1);
            }
            else
            {
                if (!_consoleOutput.Visible)
                {
                    _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, _consoleOutput.Handle);
                }
                else
                {
                    _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_DMMHIDE, 0, _consoleOutput.Handle);
                }
            }
            _consoleOutput.Focus();
        }

        #endregion

        #region [Event Handlers]

        private static void OnLinkTargetsWindowIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (_linkTargetsWindow.Visibility == System.Windows.Visibility.Hidden)
            {
                //give focus back to npp
                _nppHelper.GrabFocus();
            }
        }

        private static void OnSecondScintillaFocusChanged(object source, ScintillaMessageInterceptor.ScintillaFocusChangedEventArgs e)
        {
            HandleScintillaFocusChange(e, ref _hasSecondScintillaFocus);
        }

        private static void OnMainScintillaFocusChanged(object source, ScintillaMessageInterceptor.ScintillaFocusChangedEventArgs e)
        {
            HandleScintillaFocusChange(e, ref _hasMainScintillaFocus);
        }

        private static void OnMenuLoopStateChanged(object source, NotepadMessageInterceptor.MenuLoopStateChangedEventArgs e)
        {
            if ((_isMenuLoopInactive = e.IsMenuLoopActive))
            {
                CommitAutoCompletion(false);
            }            
        }

        /**
         * \brief   Executes action when mouse wheel movement is detected.
         *          This is used to route low level windows events from scintilla to the plugin, 
         *          which would otherwise be lost. e.g. for scrolling via a touchpad.
         *
         * \param   msg     The message.
         * \param   wParam  The parameter.
         * \param   lParam  The parameter.
         *
         * \todo    Handle this for other windows as well, e.g. reference links          
         */
        private static void OnScintillaMouseWheelMoved(object source, ScintillaMessageInterceptor.MouseWheelMovedEventArgs e)
        {
            e.Handled = _autoCompletionForm.OnMessageReceived(e.Msg, e.WParam, e.LParam);
        }

        static internal void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, _funcItems.Items[(int)Constants.NppMenuCommands.ConsoleWindow]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }        

        /**
         * Handles file opened event to start backend process, in case the relevant  backend process is not yet started.
         */
        public static void OnFileOpened()
        {            
            string aFileOpened = Npp.Instance.GetCurrentFilePath();
            _connectorManager.CreateConnector(aFileOpened);
            _fileObserver.OnFileOpened(aFileOpened);
        }

        /**
         * Occurs when undo operation exists for the current document.
         */
        public static void OnFileConsideredModified()
        {
            _fileObserver.OnFilemodified(Npp.Instance.GetCurrentFilePath());
        }

        /**
         * Occurs when no undo operation exist for the current document.
         */
        public static void OnFileConsideredUnmodified()
        {
            _fileObserver.OnFileUnmodified(Npp.Instance.GetCurrentFilePath());
        }

        /**
         * Gets file modification observer. Can be used to save all opened files of a workspace.
         *
         * \return  The file observer.
         */
        public static FileModificationObserver GetFileObserver()
        {
            return _fileObserver;
        }

        /**
         * Scintilla notification that the zomm level has been changed.
         *
         */
        internal static void OnZoomLevelModified()
        {
            int aNewZoomLevel = Npp.Instance.GetZoomLevel();
            if(aNewZoomLevel != _currentZoomLevel)
            {
                _currentZoomLevel = aNewZoomLevel;
                _autoCompletionForm.OnZoomLevelChanged(_currentZoomLevel);
                _linkTargetsWindow.OnZoomLevelChanged(_currentZoomLevel);
            }
        }

        #endregion

        #region [Helpers]

        private static void HandleScintillaFocusChange(ScintillaMessageInterceptor.ScintillaFocusChangedEventArgs e, ref bool hasFocus)
        {
            hasFocus = e.Focused;
            IntPtr aWindowWithFocus = unchecked((IntPtr)(long)(ulong)e.WindowHandle);
            IntPtr aAutoCompletionWindow = VisualUtilities.HwndFromWpfWindow(_autoCompletionForm);
            if (!hasFocus && e.WindowHandle != null && aWindowWithFocus != aAutoCompletionWindow)
            {
                if (aWindowWithFocus != IntPtr.Zero && aWindowWithFocus != Npp.Instance.CurrentScintilla)
                {
                    CommitAutoCompletion(false);
                }
            }
        }

        static bool HasScintillaFocus()
        {
            if (_hasMainScintillaFocus || _hasSecondScintillaFocus)
            {
                return true;
            }
            else
            {
                if(_autoCompletionForm.IsVisible)// && _autoCompletionForm.IsFocused)
                {
                    Npp.Instance.GrabFocus();
                    return true;
                }
            }
            return false;
        }

        /**
         * Enumerates bind interanal shortcuts in this collection.
         *
         * \return  An enumerator that allows foreach to be used to process interanal shortcuts.
         */
        static IEnumerable<Keys> BindInteranalShortcuts()
        {
            var uniqueKeys = new Dictionary<Keys, int>();

            AddInternalShortcuts("Ctrl+Space",
                                 "Show auto-complete list",
                                  StartAutoCompleteSession, uniqueKeys);

            //AddInternalShortcuts("_FindAllReferences:Shift+F12",
            //                     "Find All References",
            //                      FindAllReferences, uniqueKeys);

            //AddInternalShortcuts("_GoToDefinition:F12",
            //                     "Go To Definition",
            //                      GoToDefinition, uniqueKeys);

            return uniqueKeys.Keys;
        }

        static void AddInternalShortcuts(string shortcutSpec, string displayName, Action handler, Dictionary<Keys, int> uniqueKeys)
        {
            ShortcutKey shortcut = new ShortcutKey(shortcutSpec);

            internalShortcuts.Add(shortcut, new Tuple<string, Action>(displayName, handler));

            var key = (Keys)shortcut._key;
            if (!uniqueKeys.ContainsKey(key))
                uniqueKeys.Add(key, 0);
        }

        static internal void LoadSettings()
        {
            if ( _settings.Get<bool>(Settings.RTextNppSettings.ConsoleWindowActive))
            {
                ShowConsoleOutput();
                _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK, _funcItems.Items[0]._cmdID, 1);
            }

            _nppMsgInterceptpr                                   = new NotepadMessageInterceptor(nppData._nppHandle);
            _scintillaMainMsgInterceptor                         = new ScintillaMessageInterceptor(nppData._scintillaMainHandle);
            _scintillaMainMsgInterceptor.ScintillaFocusChanged   += OnMainScintillaFocusChanged;
            _scintillaSecondMsgInterceptor                       = new ScintillaMessageInterceptor(nppData._scintillaSecondHandle);
            _scintillaSecondMsgInterceptor.ScintillaFocusChanged += OnSecondScintillaFocusChanged;
            _scintillaMainMsgInterceptor.MouseWheelMoved         += OnScintillaMouseWheelMoved;
            _scintillaSecondMsgInterceptor.MouseWheelMoved       += OnScintillaMouseWheelMoved;
            _nppMsgInterceptpr.MenuLoopStateChanged              += OnMenuLoopStateChanged;
        }

        /**
         * Handles exceptions that may be thrown by the action.
         *
         * \param   action  The action to be executed.
         */
        static void HandleErrors(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Logging.Logger.Instance.Append("HandleErrors exception : {0}", e.Message);
            }
        }
        #endregion
    }
}
