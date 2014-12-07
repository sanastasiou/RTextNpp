using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using RTextNppPlugin.Dialogs;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Configuration;
using System.Timers;
using System.ComponentModel;

namespace RTextNppPlugin
{
    public partial class Plugin
    {
        const int ConsoleCommand = 0;


        private static Utilities.NppControlHost<ConsoleOutputForm> _consoleOutput = new Utilities.NppControlHost<ConsoleOutputForm>(Constants.CONSOLE_OUTPUT_SETTING_KEY);
        private static System.Timers.Timer _startupTimer = new System.Timers.Timer(Constants.WINDOWS_RESTORE_TIMER);

        //private static ConsoleOutputForm _consoleOutput = null;
        //private static Icon _consoleOutputIcon = null;
        //static Bitmap tbBmp_tbTab = Properties.Resources.star_bmp;
        public const string PluginName = "&RText++";

        public static int projectPanelId = -1;
        public static int outputPanelId = -1;
        public static int debugPanelId = -1;

        public static Dictionary<ShortcutKey, Tuple<string, Action>> internalShortcuts = new Dictionary<ShortcutKey, Tuple<string, Action>>();

        /**
         * Initializes command menu for the RText++ plugin.
         */
        static internal void CommandMenuInit()
        {
            _startupTimer.Elapsed += OnStartupTimerElapsed;
            _startupTimer.AutoReset = false;
            //'_' prefix in the shortcutName means "pluging action shortcut" as opposite to "plugin key interceptor action"
            SetCommand(0, "Show RText++ Console", ShowConsoleOutput, new ShortcutKey(false, true, true, Keys.R));
            //SetCommand(1, "Refresh RText++ Console", RefreshConsoleOutput, new ShortcutKey(false, true, true, Keys.Q));
            //SetCommand(projectPanelId = index++, "Run", Run, "_Run:F5");
            //SetCommand(projectPanelId = index++, "Debug", Debug, "_Debug:Alt+F5");
            //SetCommand(projectPanelId = index++, "Debug External Process", DebugEx, "_DebugExternal:Ctrl+Shift+F5");
            //SetCommand(index++, "---", null);
            //SetCommand(projectPanelId = index++, "Project Panel", DoProjectPanel, Config.Instance.ShowProjectPanel);
            //SetCommand(outputPanelId = index++, "Output Panel", DoOutputPanel, Config.Instance.ShowOutputPanel);
            //SetCommand(debugPanelId = index++, "Debug Panel", DoDebugPanel, Config.Instance.ShowDebugPanel);
            //SetCommand(index++, "---", null);
            ////LoadIntellisenseCommands(ref index);
            //SetCommand(index++, "About", ShowAbout);

            //IEnumerable<Keys> keysToIntercept = BindInteranalShortcuts();

            //KeyInterceptor.Instance.Install();

            //foreach (var key in keysToIntercept)
            //    KeyInterceptor.Instance.Add(key);
            //KeyInterceptor.Instance.Add(Keys.Tab);
            //KeyInterceptor.Instance.KeyDown += Instance_KeyDown;

            ////setup dependency injection, which may be overwritten by other plugins (e.g. NppScripts)
            //Plugin.RunScript = () => Plugin.ProjectPanel.Run();
            //Plugin.RunScriptAsExternal = () => Plugin.ProjectPanel.RunAsExternal();
            //Plugin.DebugScript = () => Plugin.ProjectPanel.Debug(false);

            _startupTimer.Start();

            //check box will have false value - check if this can be fixed via background timer
            bool wasConsoleOpen = false;
            Utilities.ConfigurationSetter.readSetting(ref wasConsoleOpen, Constants.CONSOLE_OUTPUT_SETTING_KEY);
            if (wasConsoleOpen)
            {
                ShowConsoleOutput();
            }
        }

        /**
         * \brief   Raises the elapsed event.
         *
         * \param   sender  Source of the event.
         * \param   e       Event information to send to registered event handlers.
         */
        private static void OnStartupTimerElapsed(object sender, ElapsedEventArgs e)
        {
            //update the cmd ids - cmd id's are 0 after plugin initialization
            _consoleOutput.CmdId = FuncItems.Items[ConsoleCommand]._cmdID;
        }

        public static void Debug(string s)
        {
            Win32.SendMessage(GetCurrentScintilla(), SciMsg.SCI_SETTEXT, 0, s);
        }

        public static void Hello()
        {
            // Open a new document
            Win32.SendMessage(NppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
            // Say hello now :
            // Scintilla control has no Unicode mode, so we use ANSI here (marshalled as ANSI by default)
            Win32.SendMessage(GetCurrentScintilla(), SciMsg.SCI_SETTEXT, 0, "Hello, Notepad++... from .NET!");
        }

        /**
         * \brief   Shows the console output.
         * \todo    Find a way to fix the icon. Check if menu entry can also have associated icon.         
         * \todo    Make this thing work with calibrun.micro
         */
        static void ShowConsoleOutput()
        {
            if (!_consoleOutput.Created)
            {
                //fix the command id...
                //_consoleOutput = new Utilities.NppControlHost<ConsoleOutputForm>(Constants.CONSOLE_OUTPUT_SETTING_KEY, NppData._nppHandle);
                _consoleOutput.SetNppHandle(NppData._nppHandle);

                //using (Bitmap newBmp = new Bitmap(16, 16))
                //{
                //    Graphics g = Graphics.FromImage(newBmp);
                //    ColorMap[] colorMap = new ColorMap[1];
                //    colorMap[0] = new ColorMap();
                //    colorMap[0].OldColor = Color.Fuchsia;
                //    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                //    ImageAttributes attr = new ImageAttributes();
                //    attr.SetRemapTable(colorMap);
                //    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                //    _consoleOutputIcon = Icon.FromHandle(newBmp.GetHicon());
                //}

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = _consoleOutput.Handle;
                _nppTbData.pszName = "RText++ Console Output";
                _nppTbData.dlgID = ConsoleCommand;
                // define the default docking behaviour
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                //_nppTbData.hIconTab = (uint)_consoleOutputIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);
                _consoleOutput.CmdId = FuncItems.Items[ConsoleCommand]._cmdID;
                Win32.SendMessage(NppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
            }
            else
            {
                if (!_consoleOutput.Visible)
                {
                    Win32.SendMessage(NppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, _consoleOutput.Handle);
                }
                else
                {
                    Win32.SendMessage(NppData._nppHandle, NppMsg.NPPM_DMMHIDE, 0, _consoleOutput.Handle);
                }
            }
            _consoleOutput.Focus();
        }

        static void _restoreConsoleWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            
        }

        static public Action RunScript;
        static public Action RunScriptAsExternal;
        static public Action DebugScript;

        ////must be in a separate method to allow proper assembly probing
        //private static void LoadIntellisenseCommands(ref int cmdIndex)
        //{
        //    NppPluginNET.Plugin.CommandMenuInit(ref cmdIndex,
        //         (index, name, handler, shortcut) =>
        //         {
        //             if (name == "Settings")
        //                 Plugin.SetCommand(index, name, ShowConfig, shortcut);
        //             else
        //                 Plugin.SetCommand(index, name, handler, shortcut);
        //         });
        //}

        private static void AddInternalShortcuts(string shortcutSpec, string displayName, Action handler, Dictionary<Keys, int> uniqueKeys)
        {
            ShortcutKey shortcut = shortcutSpec.ParseAsShortcutKey(displayName);

            internalShortcuts.Add(shortcut, new Tuple<string, Action>(displayName, handler));

            var key = (Keys)shortcut._key;
            if (!uniqueKeys.ContainsKey(key))
                uniqueKeys.Add(key, 0);
        }

        private static IEnumerable<Keys> BindInteranalShortcuts()
        {
            var uniqueKeys = new Dictionary<Keys, int>();

            AddInternalShortcuts("Build:F7",
                                 "Build (validate)",
                                  Build, uniqueKeys);

            AddInternalShortcuts("Stop:Shift+F5",
                                  "Stop running script",
                                  Stop, uniqueKeys);

            AddInternalShortcuts("_Run:F5",
                                 "Run",
                                  Run, uniqueKeys);

            AddInternalShortcuts("StepOver:F10",
                                 "Step Over",
                                  StepOver, uniqueKeys);

            //AddInternalShortcuts("SetNextIP:Ctrl+Shift+F10",
            //                     "Set Next Statement",
            //                      Debugger.SetInstructionPointer, uniqueKeys);

            //AddInternalShortcuts("RunToCursor:Ctrl+F10",
            //                     "Run To Cursor",
            //                      Debugger.RunToCursor, uniqueKeys);

            AddInternalShortcuts("RunAsExternal:Ctrl+F5",
                                  "Run As External Process", () =>
                                  {
                                      if (Npp.IsCurrentScriptFile())
                                          RunAsExternal();
                                  }, uniqueKeys);

            //AddInternalShortcuts("ShowNextFileLocationFromOutput:F4",
            //                     "Next File Location in Output", () =>
            //                     {
            //                         OutputPanel.TryNavigateToFileReference(toNext: true);
            //                     }, uniqueKeys);

            //AddInternalShortcuts("ShowPrevFileLocationFromOutput:Shift+F4",
            //                     "Previous File Location in Output", () =>
            //                     {
            //                         OutputPanel.TryNavigateToFileReference(toNext: false);
            //                     }, uniqueKeys);

            return uniqueKeys.Keys;
        }

        private static void Instance_KeyDown(Keys key, int repeatCount, ref bool handled)
        {
            foreach (var shortcut in internalShortcuts.Keys)
                if ((byte)key == shortcut._key)
                {
                    Modifiers modifiers = KeyInterceptor.GetModifiers();

                    if (modifiers.IsCtrl == shortcut.IsCtrl && modifiers.IsShift == shortcut.IsShift && modifiers.IsAlt == shortcut.IsAlt)
                    {
                        handled = true;
                        var handler = internalShortcuts[shortcut];
                        handler.Item2();
                    }
                }
        }

        static public void ShowConfig()
        {
            using (var form = new ConfigForm(Config.Instance))
            {
                form.ShowDialog();
                Config.Instance.Save();
            }
        }

        static public void ShowAbout()
        {
            using (var dialog = new AboutBox())
                dialog.ShowDialog();
        }

        static public OutputPanel OutputPanel;
        static public ProjectPanel ProjectPanel;
        static public CodeMapPanel CodeMapPanel;
        static public DebugPanel DebugPanel;

        static public DebugPanel GetDebugPanel()
        {
            if (Plugin.DebugPanel == null)
                Plugin.DoDebugPanel();
            return Plugin.DebugPanel;
        }

        static public ProjectPanel GetProjectPanel()
        {
            if (Plugin.ProjectPanel == null)
                Plugin.DoProjectPanel();
            return Plugin.ProjectPanel;
        }

        static public void DoOutputPanel()
        {
            Plugin.OutputPanel = ShowDockablePanel<OutputPanel>("Output", outputPanelId, NppTbMsg.DWS_DF_CONT_BOTTOM | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR);
        }

        static public void DoDebugPanel()
        {
            Plugin.DebugPanel = ShowDockablePanel<DebugPanel>("Debug", debugPanelId, NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR);
        }

        static public void ShowSecondaryPanels()
        {
            if (Plugin.OutputPanel == null)
                DoOutputPanel();

            if (Plugin.DebugPanel == null)
                DoDebugPanel();

            Plugin.SetDockedPanelVisible(Plugin.OutputPanel, outputPanelId, true);
            Plugin.SetDockedPanelVisible(Plugin.DebugPanel, debugPanelId, true);
        }

        static public void DoProjectPanel()
        {
            ProjectPanel = ShowDockablePanel<ProjectPanel>("CS-Script", projectPanelId, NppTbMsg.DWS_DF_CONT_LEFT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR);
            ProjectPanel.Focus();
        }

        static public void ShowProjectPanel()
        {
            SetDockedPanelVisible(dockedManagedPanels[projectPanelId], projectPanelId, true);
        }

        //static int toggleScondaryPanelsCount = 0;
        static public void ToggleScondaryPanels()
        {
            Plugin.ShowSecondaryPanels();
            //return;

            //if (toggleScondaryPanelsCount == 0 && (Plugin.OutputPanel == null || !Plugin.OutputPanel.Visible))
            //{
            //    Plugin.DoOutputPanel();
            //}
            //else if (toggleScondaryPanelsCount == 3 && (Plugin.DebugPanel == null || !Plugin.DebugPanel.Visible))
            //{
            //    Plugin.DoDebugPanel();
            //}
            //else if (toggleScondaryPanelsCount == 4)
            //{
            //    Plugin.DoOutputPanel();
            //    Plugin.DoDebugPanel();
            //}

            //toggleScondaryPanelsCount++;
            //if (toggleScondaryPanelsCount > 4)
            //    toggleScondaryPanelsCount = 0;
        }

        static public void Build()
        {
            if (runningScript == null)
            {
                if (Plugin.ProjectPanel == null)
                    DoProjectPanel();
                Plugin.ProjectPanel.Build();
            }
        }

        static public void StepOver()
        {
            //if (Debugger.IsRunning)
            //    Debugger.StepOver();
            //else
            //    GetProjectPanel().Debug(breakOnFirstStep: true);
        }

        static public void Run()
        {
            //if (Debugger.IsRunning)
            //{
            //    Debugger.Go();
            //}
            //else if (Npp.IsCurrentScriptFile() && runningScript == null)
            //{
            //    if (Plugin.ProjectPanel == null)
            //        DoProjectPanel();
            //    Plugin.RunScript();
            //}
        }

        static public void DebugEx()
        {
            //if (!Debugger.IsRunning)
            //{
            //    if (Plugin.ProjectPanel == null)
            //        DoProjectPanel();
            //    DebugExternal.ShowModal();
            //}
        }

        static public void Debug()
        {
            //if (!Debugger.IsRunning)
            //{
            //    if (Plugin.ProjectPanel == null)
            //        DoProjectPanel();
            //    Plugin.DebugScript();
            //}
        }

        static public void Stop()
        {
            //if (Debugger.IsRunning)
            //{
            //    Debugger.Exit();
            //}
            //else
            //{
            //    try
            //    {
            //        if (Plugin.RunningScript != null)
            //            Plugin.RunningScript.Kill();
            //    }
            //    catch (Exception ex)
            //    {
            //        Plugin.OutputPanel.DebugOutput.WriteLine(null)
            //                                      .WriteLine(ex.Message);
            //    }
            //}
        }

        static public void RunAsExternal()
        {
            if (runningScript == null)
            {
                if (Plugin.ProjectPanel == null)
                    DoProjectPanel();
                Plugin.RunScriptAsExternal();
            }
        }

        static public OutputPanel ShowOutputPanel()
        {
            if (Plugin.OutputPanel == null)
                DoOutputPanel();
            else
                SetDockedPanelVisible(Plugin.OutputPanel, outputPanelId, true);

            UpdateLocalDebugInfo();
            return Plugin.OutputPanel;
        }

        private static Process runningScript;

        public static Process RunningScript
        {
            get
            {
                return runningScript;
            }
            set
            {
                runningScript = value;
                UpdateLocalDebugInfo();
            }
        }

        private static void UpdateLocalDebugInfo()
        {
            if (runningScript == null)
                Plugin.OutputPanel.localDebugPrefix = null;
            else
                Plugin.OutputPanel.localDebugPrefix = runningScript.Id.ToString() + ": ";
        }

        static internal void OnNppReady()
        {
            if (Config.Instance.ShowProjectPanel)
                DoProjectPanel();

            if (Config.Instance.ShowOutputPanel)
                DoOutputPanel();

            if (Config.Instance.ShowDebugPanel)
                DoDebugPanel();

            StartCheckForUpdates();

            OpenAutomationChannel();
        }

        static internal void OnDocumentSaved()
        {
            if (Plugin.ProjectPanel != null)
                Plugin.ProjectPanel.RefreshProjectStructure();
        }

        /**
         * \brief   Occurs before notepad++ shuts down.
         */
        static internal void CleanUp()
        {
            _startupTimer.Elapsed -= OnStartupTimerElapsed;

            //Config.Instance.ShowProjectPanel = (dockedManagedPanels.ContainsKey(projectPanelId) && dockedManagedPanels[projectPanelId].Visible);
            //Config.Instance.ShowOutputPanel = (dockedManagedPanels.ContainsKey(outputPanelId) && dockedManagedPanels[outputPanelId].Visible);
            //Config.Instance.ShowDebugPanel = (dockedManagedPanels.ContainsKey(debugPanelId) && dockedManagedPanels[debugPanelId].Visible);
            //Config.Instance.Save();
            //OutputPanel.Clean();
            //CloseAutomationChannel();
        }

        internal static string HomeUrl = "https://NppPluginNET.codeplex.com/";

        private static void StartCheckForUpdates()
        {
            lock (typeof(Plugin))
            {
                if (Config.Instance.CheckUpdatesOnStartup)
                {
                    string date = DateTime.Now.ToString("yyyy-MM-dd");
                    if (Config.Instance.LastUpdatesCheckDate != date)
                    {
                        Config.Instance.LastUpdatesCheckDate = date;
                        Config.Instance.Save();

                        Task.Factory.StartNew(CheckForUpdates);
                    }
                }
            }
        }

        private static void CheckForUpdates()
        {
            Thread.Sleep(2000); //let Notepad++ to complete all initialization

            //string version = CSScriptHelper.GetLatestAvailableVersion();

            //if (version != null)
            //{
            //    var latestVersion = new Version(version);
            //    var nppVersion = Assembly.GetExecutingAssembly().GetName().Version;

            //    if (nppVersion < latestVersion)
            //    {
            //        using (var dialog = new UpdateOptionsPanel(version))
            //            dialog.ShowDialog();
            //    }
            //}
        }

        public static void OnNotification(SCNotification data)
        {
        }

        static public void OnCurrentFileChanged()
        {
            if (CodeMapPanel != null)
                CodeMapPanel.RefreshContent();
        }

        public static void OnToolbarUpdate()
        {
            Plugin.FuncItems.RefreshItems();
            //SetToolbarImage(Resources.Resources.css_logo_16x16_tb, projectPanelId);
        }

        static void CloseAutomationChannel()
        {
            //MessageQueue.AddAutomationCommand("automation.exit");
        }

        static void OpenAutomationChannel()
        {
            //Task.Factory.StartNew(() =>
            //{
            //    try
            //    {
            //        while (true)
            //        {
            //            string message = MessageQueue.WaitForAutomationCommand();
            //            if (message == "automation.exit")
            //                break;

            //            MessageBox.Show(message);
            //        }
            //    }
            //    catch { };
            //});
        }

        static public void ProcessCommandArgs(string args)
        {
            ////System.Diagnostics.Debug.Assert(false);
            //if (args.StartsWith("/css.attach:")) //attach to external process
            //{
            //    try
            //    {
            //        var id = int.Parse(args.Substring("/css.attach:".Length));
            //        DebugExternal.AttachTo(id);
            //    }
            //    catch { }
            //}
        }

        static public void OnCharTyped(char c)
        {
            if (Npp.IsCurrentScriptFile())
            {
                //if (c == '.')
                //    ShowSuggestionList();
                //else if (c == '(')
                //{
                //    if (!memberInfoPopup.IsShowing || memberInfoPopup.Simple)
                //        ShowMethodInfo();
                //}
                //else if (memberInfoPopup.IsShowing)
                //    memberInfoPopup.CheckIfNeedsClosing();

                //SourceCodeFormatter.OnCharTyped(c);
            }
        }
    }
}