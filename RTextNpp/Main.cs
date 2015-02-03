using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Timers;
using RTextNppPlugin;
using System.Reflection;
using System.Diagnostics;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.WpfControlHost;
using RTextNppPlugin.Automate;
using RTextNppPlugin.Forms;
using System.Threading.Tasks;

namespace RTextNppPlugin
{
    partial class Plugin
    {
        #region [Fields]
        private static PersistentWpfControlHost<ConsoleOutputForm> _consoleOutput = new PersistentWpfControlHost<ConsoleOutputForm>(Settings.RTextNppSettings.ConsoleWindowActive);
        private static ConnectorManager _connectorManager = Automate.ConnectorManager.Instance;
        private static Options _options = new Forms.Options();
        private static FileModificationObserver _fileObserver = new FileModificationObserver();
        private static Dictionary<ShortcutKey, Tuple<string, Action>> internalShortcuts = new Dictionary<ShortcutKey, Tuple<string, Action>>();
        private static WpfControlHostBase<AutoCompletionForm> _autoCompletionForm = new WpfControlHostBase<AutoCompletionForm>();
        private static Point _autoCompletionTriggerPoint = new Point();

        public const string PluginName = "RTextNpp";
        static Bitmap tbBmp = Properties.Resources.ConsoleIcon;
        static Bitmap tbBmp_tbTab = Properties.Resources.ConsoleIcon;
        static Icon tbIcon = null;
        static bool _consoleInitialized = false;
        static bool _invokeInProgress = false;        
        #endregion

        #region [Startup/CleanUp]

        static internal void CommandMenuInit()
        {
            CSScriptIntellisense.KeyInterceptor.Instance.Install();

            SetCommand((int)Constants.NppMenuCommands.ConsoleWindow, Properties.Resources.RTEXT_SHOW_OUTPUT_WINDOW, ShowConsoleOutput, new ShortcutKey(false, true, true, Keys.R));
            SetCommand((int)Constants.NppMenuCommands.Options, Properties.Resources.RTEXT_SHOW_OPTIONS_WINDOW, ModifyOptions, new ShortcutKey(true, false, true, Keys.R));
            SetCommand((int)Constants.NppMenuCommands.AutoCompletion, Properties.Resources.SHOW_AUTO_COMPLETION_LIST_NAME, StartAutoCompleteSession, "Ctrl+Space");

            _connectorManager.initialize(nppData);
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
                            handled = !_autoCompletionForm.ElementHost.Visible;
                            if (!_autoCompletionForm.ElementHost.Visible)
                            {
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
                        if (!_autoCompletionForm.ElementHost.Visible)
                        {
                            var lol = FileUtilities.GetAutoCompletionTriggerPoint();
                            _autoCompletionTriggerPoint = CSScriptIntellisense.Npp.GetCaretScreenLocationForForm();
                            //caret position before first character is needs - or first character in current token                            
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
                _autoCompletionForm.ElementHost.Hide();
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
                    if (!_autoCompletionForm.Visible)
                    {                                                                            
                        //todo - get backend context 
                        //todo - get response from backend
                        //todo - handle text insertion

                        Point aCaretPoint = new Point();
                        //if(_autoCompletionTriggerPoint != null)
                        //{
                        //    aCaretPoint = _autoCompletionTriggerPoint;
                        //}
                        //else
                        //{
                            aCaretPoint = CSScriptIntellisense.Npp.GetCaretScreenLocationForForm();
                        //}

                        _autoCompletionForm = new WpfControlHostBase<AutoCompletionForm>();
                        _autoCompletionForm.ElementHost.Left = aCaretPoint.X;
                        _autoCompletionForm.ElementHost.Top = aCaretPoint.Y;

                        //_autoCompletionForm.ElementHost.FormClosed += (sender, e) =>
                        //{
                        //    //if (memberInfoWasShowing)
                        //    //    NppUI.Marshal(() => Dispatcher.Shedule(100, ShowMethodInfo));
                        //};
                        //_autoCompletionForm.ElementHost.KeyPress += (sender, e) =>
                        //{
                        //    if (e.KeyChar >= ' ' || e.KeyChar == 8) //8 is backspace
                        //        On_autoCompletion_autoCompletionFormcompleteKeyPress(e.KeyChar);
                        //};
                        _autoCompletionForm.ElementHost.Show(Control.FromHandle(nppData._nppHandle));

                        //OnAutocompleteKeyPress(allowNoText: true); //to grab current word at the caret an process it as a hint
                    }
                    else
                    {
                        //already active auto completion session                                                   
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
            _connectorManager.createConnector(aFileOpened);
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
