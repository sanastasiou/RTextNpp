using CSScriptIntellisense;
using RTextNppPlugin.Automate;
using RTextNppPlugin.Forms;
using RTextNppPlugin.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.WpfControlHost;
using RTextNppPlugin.WpfControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RTextNppPlugin
{
    partial class Plugin
    {
        #region [Fields]
        private static PersistentWpfControlHost<ConsoleOutputForm> _consoleOutput       = new PersistentWpfControlHost<ConsoleOutputForm>(Settings.RTextNppSettings.ConsoleWindowActive);
        private static ConnectorManager _connectorManager                               = Automate.ConnectorManager.Instance;
        private static Options _options                                                 = new Forms.Options();
        private static FileModificationObserver _fileObserver                           = new FileModificationObserver();
        private static Dictionary<ShortcutKey, Tuple<string, Action>> internalShortcuts = new Dictionary<ShortcutKey, Tuple<string, Action>>();
        private static AutoCompletionWindow _autoCompletionForm                         = new AutoCompletionWindow(); //!< The link targets window instance
        public const string PluginName                                                  = "RTextNpp";
        static Bitmap tbBmp                                                             = Properties.Resources.ConsoleIcon;
        static Bitmap tbBmp_tbTab                                                       = Properties.Resources.ConsoleIcon;
        static Icon tbIcon                                                              = null;
        static bool _consoleInitialized                                                 = false;
        static bool _invokeInProgress                                                   = false;
        static bool _requestAutoCompletion                                              = false;
        static int _currentZoomLevel                                                    = 0;
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
            
            foreach(var key in Enum.GetValues(typeof(Keys)))
            {
                CSScriptIntellisense.KeyInterceptor.Instance.Add((Keys)key);
            }

            _currentZoomLevel = Npp.GetZoomLevel();
            _autoCompletionForm.OnZoomLevelChanged(_currentZoomLevel);

            Debugger.Launch();
        }

        static void OnKeyInterceptorKeyDown(Keys key, int repeatCount, ref bool handled)
        {            
            if (FileUtilities.IsAutomateFile())
            {
                CSScriptIntellisense.Modifiers modifiers = CSScriptIntellisense.KeyInterceptor.GetModifiers();                
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
                                _requestAutoCompletion = true;
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
                    return;
                }                

                //auto complete Ctrl+Space is handled above - here we handle other special cases
                switch (key)
                {
                    case Keys.Back:
                        handled = true;
                        Npp.DeleteBack(1);
                        //in case of empty trigger token form needs to close
                        _autoCompletionForm.OnKeyPressed(Constants.BACKSPACE);
                        if(_autoCompletionForm.CharProcessAction == ViewModels.AutoCompletionViewModel.CharProcessResult.ForceClose)
                        {
                            CommitAutoCompletion(false);
                        }                        
                        break;
                    case Keys.Delete:
                        handled = true;
                        Npp.DeleteFront();
                        _autoCompletionForm.OnKeyPressed();
                        break;
                    case Keys.Space:                        
                        handled = true;
                        //if completion list is visible, and there is a trigger token other than comma or space and there is some selected option
                        if (_autoCompletionForm.Completion != null && _autoCompletionForm.Completion.IsSelected)
                        {
                            CommitAutoCompletion(true);
                            Npp.AddText(Constants.SPACE.ToString());
                            return;

                        }
                        Npp.AddText(Constants.SPACE.ToString());
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
                                Npp.AddText(Constants.TAB.ToString());
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
                            Npp.AddText(Constants.COMMA.ToString());
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
                        char mappedChar   = Npp.GetAsciiCharacter((int)key, nonVirtualKey);
                        if (mappedChar != default(char))
                        {
                            handled = true;
                            Npp.AddText(new string(mappedChar, 1));
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
                    _requestAutoCompletion = true;
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
                    Npp.ReplaceWordFromToken(_autoCompletionForm.TriggerPoint, _autoCompletionForm.Completion.InsertionText);
                }
            }
            _autoCompletionForm.Hide();
            _autoCompletionForm.ClearCompletion();
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
            CSScriptIntellisense.KeyInterceptor.Instance.KeyDown -= OnKeyInterceptorKeyDown;
            _fileObserver.CleanBackup();
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
                if (FileUtilities.IsAutomateFile())
                {
                    if (!_autoCompletionForm.IsVisible)
                    {          
                        int aCurrentPosition = CSScriptIntellisense.Npp.GetCaretPosition();
                        int aStartPosition   = CSScriptIntellisense.Npp.GetLineStart(CSScriptIntellisense.Npp.GetLineNumber());
                        int aColumn = (aCurrentPosition - aStartPosition) + 1;
                        //fix extractor column bug...
                        if (aCurrentPosition >= 0)
                        {
                            int aLineNumber = CSScriptIntellisense.Npp.GetLineNumber();
                            //get text from start till current line end
                            string aContextBlock = CSScriptIntellisense.Npp.GetTextBetween(0, CSScriptIntellisense.Npp.GetLineEnd(aLineNumber));
                            ContextExtractor aExtractor = new ContextExtractor(aContextBlock, CSScriptIntellisense.Npp.GetLengthToEndOfLine());
                            //if auto completion is inside comment, notation, name, string jusr return
                            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(aLineNumber, aCurrentPosition, aExtractor.ContextColumn);                            
                            //if a token is found then the window should appear at the start of it, else it should appear at the caret
                            Point aCaretPoint = CSScriptIntellisense.Npp.GetCaretScreenLocationForForm();
                            if (aTokenizer.TriggerToken.HasValue && aTokenizer.TriggerToken.Value.Type != RTextTokenTypes.Comma)
                            {
                                aCaretPoint = CSScriptIntellisense.Npp.GetCaretScreenLocationRelativeToPosition(aTokenizer.TriggerToken.Value.BufferPosition);
                            }                                                        
                            _autoCompletionForm.Left = aCaretPoint.X;
                            _autoCompletionForm.Top  = aCaretPoint.Y;
                            Utilities.VisualUtilities.SetOwnerFromNppPlugin(_autoCompletionForm);
                            _autoCompletionForm.AugmentAutoCompletion(aExtractor, aCaretPoint, aTokenizer, ref _requestAutoCompletion);
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
                        }
                    }
                }
                else
                {
                    Win32.SendMessage(Plugin.nppData._nppHandle, (NppMsg)WinMsg.WM_COMMAND, (int)NppMenuCmd.IDM_EDIT_AUTOCOMPLETE, 0);
                }
            });
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

        /**
         * \brief Modify options callback from plugin menu.           
         */
        static void ModifyOptions()
        {
            Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_MODELESSDIALOG, (int)NppMsg.MODELESSDIALOGADD, _options.Handle.ToInt32());
            if (_options.ShowDialog(Control.FromHandle(nppData._nppHandle)) == DialogResult.OK)
            {
                _options.SaveSettings();
            }
            else
            {
                _options.RestoreSettings();
            }
            Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_MODELESSDIALOG, (int)NppMsg.MODELESSDIALOGREMOVE, _options.Handle.ToInt32());
        }

        static void ShowConsoleOutput()
        {
            if (!_consoleInitialized)
            {
                _consoleInitialized = true;
                _consoleOutput.SetNppHandle(nppData._nppHandle);

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
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);
                _consoleOutput.CmdId = _funcItems.Items[(int)Constants.NppMenuCommands.ConsoleWindow]._cmdID;
                Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
                Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK, _funcItems.Items[0]._cmdID, 1);
            }
            else
            {
                if (!_consoleOutput.Visible)
                {
                    Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, _consoleOutput.Handle);
                }
                else
                {
                    Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_DMMHIDE, 0, _consoleOutput.Handle);
                }
            }
            _consoleOutput.Focus();
        }

        #endregion

        #region [Event Handlers]

        static internal void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, _funcItems.Items[(int)Constants.NppMenuCommands.ConsoleWindow]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        /**
         * Handles file opened event to start backend process, in case the relevant  backend process is not yet started.
         */
        public static void OnFileOpened()
        {            
            string aFileOpened = FileUtilities.GetCurrentFilePath();
            _connectorManager.CreateConnector(aFileOpened);
            _fileObserver.OnFileOpened(aFileOpened);
        }

        /**
         * Occurs when undo operation exists for the current document.
         */
        public static void OnFileConsideredModified()
        {
            _fileObserver.OnFileOpened(FileUtilities.GetCurrentFilePath());
        }

        /**
         * Occurs when no undo operation exist for the current document.
         */
        public static void OnFileConsideredUnmodified()
        {
            _fileObserver.OnFileOpened(FileUtilities.GetCurrentFilePath());
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
            int aNewZoomLevel = Npp.GetZoomLevel();
            if(aNewZoomLevel != _currentZoomLevel)
            {
                _currentZoomLevel = aNewZoomLevel;
                _autoCompletionForm.OnZoomLevelChanged(_currentZoomLevel);
            }
        }

        #endregion

        #region [Helpers]

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
            //Assembly assembly = Assembly.GetExecutingAssembly();
            //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            //string version = fvi.FileVersion;

            if (Settings.Instance.Get<bool>(Utilities.Settings.RTextNppSettings.ConsoleWindowActive))
            {
                ShowConsoleOutput();
                Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK, _funcItems.Items[0]._cmdID, 1);
            }
            //Logging.Logger.Instance.Append("User settings loaded.", Logging.Logger.MessageType.Info);
        }
        #endregion
    }
}
