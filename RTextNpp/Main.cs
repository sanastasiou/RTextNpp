using RTextNppPlugin.Automate;
using RTextNppPlugin.Forms;
using RTextNppPlugin.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.WpfControlHost;
using RTextNppPlugin.WpfControls;
using System;
using System.Collections.Generic;
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
        private static Point _triggerPoint                                              = new Point();
        private static AutoCompletionWindow _autoCompletionForm                         = new AutoCompletionWindow(); //!< The link targets window instance
        public const string PluginName                                                  = "RTextNpp";
        static Bitmap tbBmp                                                             = Properties.Resources.ConsoleIcon;
        static Bitmap tbBmp_tbTab                                                       = Properties.Resources.ConsoleIcon;
        static Icon tbIcon                                                              = null;
        static bool _consoleInitialized                                                 = false;
        static bool _invokeInProgress                                                   = false;
        static bool _requestAutoCompletion                                              = false;
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
            
            System.Diagnostics.Debugger.Launch();
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
                            var handler = internalShortcuts[shortcut];
                            handled = !_autoCompletionForm.IsVisible;
                            if (!_autoCompletionForm.IsVisible)
                            {
                                _requestAutoCompletion = true;
                                var res = AsyncInvoke(handler.Item2);
                            }
                            return;
                        }
                    }
                }
                //if any modifier key is pressed - ignore this key press
                if (modifiers.IsCtrl || modifiers.IsShift || modifiers.IsAlt)
                {
                    return;
                }

                //auto complete Ctrl+Space is handled above - here we handle single characters
                switch (key)
                {
                    case Keys.A:case Keys.B:case Keys.C:case Keys.D:case Keys.D0:case Keys.D1:case Keys.D2:case Keys.D3:case Keys.D4:
                    case Keys.D5:case Keys.D6:case Keys.D7:case Keys.D8:case Keys.D9:case Keys.Decimal:case Keys.E:case Keys.F:case Keys.G:
                    case Keys.H:case Keys.I:case Keys.J:case Keys.K:case Keys.L:case Keys.M:case Keys.N:case Keys.NumPad0:case Keys.NumPad1:
                    case Keys.NumPad2:case Keys.NumPad3:case Keys.NumPad4:case Keys.NumPad5:case Keys.NumPad6:case Keys.NumPad7:case Keys.NumPad8:
                    case Keys.NumPad9:case Keys.O:case Keys.OemBackslash:case Keys.OemCloseBrackets:case Keys.OemMinus:case Keys.OemOpenBrackets:
                    case Keys.OemPeriod:case Keys.OemPipe:case Keys.OemQuestion:case Keys.OemQuotes:case Keys.OemSemicolon:case Keys.Oemcomma:
                    case Keys.Oemplus:case Keys.Oemtilde:case Keys.P:case Keys.Q:case Keys.R:case Keys.S:case Keys.Space:case Keys.Subtract:case Keys.T:
                    case Keys.U:case Keys.V:case Keys.W:case Keys.X:case Keys.Y:case Keys.Z:
                        //character needs to be entered
                        handled = false;
                        if (!_autoCompletionForm.IsVisible)
                        {                        
                            var res = AsyncInvoke(StartAutoCompleteSession);                            
                        }
                        break;
                    case Keys.Return:
                    case Keys.Tab:
                        CommitAutoCompletion(true);
                        break;
                    case Keys.Back:
                        //move auto completion one char to the left and refilter it without requesting new set of options
                        break;
                    case Keys.Escape:
                    case Keys.Cancel:
                        CommitAutoCompletion(false);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                //allow event to fall through
                handled = false;
            }
        }

        private static void CommitAutoCompletion(bool replace)
        {
            if(replace)
            {
                //use current selected item to replace token
            }
            else
            {
                //just close auto completion session and do nothing
                _autoCompletionForm.Hide();
            }
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
                        int aLineNumber = CSScriptIntellisense.Npp.GetLineNumber();                                          
                        //if auto completion is inside comment, notation, name, string jusr return
                        Tokenizer aTokenizer = new Tokenizer(aLineNumber);
                        int aCurrentPosition = CSScriptIntellisense.Npp.GetCaretPosition();
                        if (aCurrentPosition >= 0)
                        {
                            Tokenizer.TokenTag? aCurrentToken = null;
                            foreach (var t in aTokenizer.Tokenize(RTextTokenTypes.Boolean, RTextTokenTypes.Comma,
                                                                  RTextTokenTypes.Command, RTextTokenTypes.Float,
                                                                  RTextTokenTypes.Integer, RTextTokenTypes.Label,
                                                                  RTextTokenTypes.LeftAngleBrakcet, RTextTokenTypes.LeftBracket,
                                                                  RTextTokenTypes.Reference, RTextTokenTypes.RightAngleBracket,
                                                                  RTextTokenTypes.RightBrakcet, RTextTokenTypes.RTextName,
                                                                  RTextTokenTypes.Template))
                            {
                                if (aCurrentPosition >= t.BufferPosition && aCurrentPosition <= t.BufferPosition + (t.EndColumn - t.StartColumn))
                                {
                                    aCurrentToken = t;
                                    break;
                                }
                            }
                            if (aCurrentToken.HasValue)
                            {
                                System.Diagnostics.Trace.WriteLine( String.Format("Autocompletion Token line : {0}\nsc : {1}\nec : {2}\npos : {3}",
                                                                    aCurrentToken.Value.Line,
                                                                    aCurrentToken.Value.StartColumn,
                                                                    aCurrentToken.Value.EndColumn,
                                                                    aCurrentToken.Value.BufferPosition));
                            }
                            //if a token is found then the window should appear at the start of it, else it should appear at the caret
                            Point aCaretPoint = CSScriptIntellisense.Npp.GetCaretScreenLocationForForm();
                            if(aCurrentToken.HasValue)
                            {
                                aCaretPoint = CSScriptIntellisense.Npp.GetCaretScreenLocationRelativeToPosition(aCurrentToken.Value.BufferPosition);
                            }
                            
                            _autoCompletionForm.Left = aCaretPoint.X;
                            _autoCompletionForm.Top  = aCaretPoint.Y;
                            Utilities.Visual.SetOwnerFromNppPlugin(_autoCompletionForm);
                            //get text from start till current line end
                            string aContextBlock = CSScriptIntellisense.Npp.GetTextBetween(0, CSScriptIntellisense.Npp.GetLineEnd(aLineNumber));
                            ContextExtractor aExtractor = new ContextExtractor(aContextBlock, CSScriptIntellisense.Npp.GetLengthToEndOfLine());
                            _autoCompletionForm.AugmentAutoCompletion(aExtractor, aCaretPoint, aCurrentToken, ref _requestAutoCompletion);
                            _autoCompletionForm.Show();
                            //todo - handle text insertion
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
