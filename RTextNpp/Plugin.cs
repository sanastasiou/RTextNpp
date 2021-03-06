using RTextNppPlugin.DllExport;
using RTextNppPlugin.Forms;
using RTextNppPlugin.RText;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.Scintilla;
using RTextNppPlugin.Scintilla.Annotations;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.Utilities.WpfControlHost;
using RTextNppPlugin.WpfControls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RTextNppPlugin
{
    internal class Plugin
    {
        #region [Fields]
        private static volatile Plugin instance                                                = null;
        private static object syncRoot                                                         = new Object();
        private INpp _nppHelper                                                                = Npp.Instance;
        private ILineVisibilityObserver _linesVisibilityObserver                               = null;
        private ISettings _settings                                                            = null;
        private StyleConfigurationObserver _styleObserver                                      = null;
        private ConnectorManager _connectorManager                                             = null;
        private MouseDwellObserver _mouseDwellObserver                                         = null;  //!< Informs clients about mouse dwell events
        private PersistentWpfControlHost<ConsoleOutputForm> _consoleOutput                     = null;
        private Options _options                                                               = null;
        private FileModificationObserver _fileObserver                                         = null;
        private Dictionary<ShortcutKey, Tuple<string, Action, ShortcutType>> internalShortcuts = new Dictionary<ShortcutKey, Tuple<string, Action, ShortcutType>>();
        private AutoCompletionWindow _autoCompletionForm                                       = null;
        private Bitmap tbBmp                                                                   = Properties.Resources.ConsoleIcon;
        private Bitmap tbBmp_tbTab                                                             = Properties.Resources.ConsoleIcon;
        private Icon tbIcon                                                                    = null;
        private bool _consoleInitialized                                                       = false;
        private bool _invokeInProgress                                                         = false;
        private int _currentZoomLevel                                                          = 0;
        private ScintillaMessageInterceptor _scintillaMainMsgInterceptor                       = null;  //!< Intercepts scintilla messages.
        private ScintillaMessageInterceptor _scintillaSecondMsgInterceptor                     = null;  //!< Intercepts scintilla messages from second scintilla handle.
        private NotepadMessageInterceptor _nppMsgInterceptpr                                   = null;  //!< Intercepts notepad ++ messages.
        private bool _hasMainScintillaFocus                                                    = false; //!< Indicates if the main editor has focus.
        private bool _hasSecondScintillaFocus                                                  = false; //!< Indicates if the second editor has focus.
        private bool _isMenuLoopInactive                                                       = false; //!< Indicates that npp menu loop is active.
        private LinkTargetsWindow _linkTargetsWindow                                           = null;  //!< Display reference links.
        private bool _isAutoCompletionShortcutActive                                           = false; //!< Indicates the Ctrl+Space is pressed. Need this to commit auto completion in case of fuzzy matching.
        private Utilities.DelayedEventHandler<object> _actionAfterUiUpdateHandler              = new DelayedEventHandler<object>(null, 100);
        private NppData _nppData                                                               = default(NppData);
        private FuncItems _funcItems                                                           = new FuncItems();
        private INativeHelpers _nativeHelpers                                                  = new NativeHelpers();
        private int _previousDwellTimeMain                                                     = (int)SciMsg.SC_TIME_FOREVER;
        private int _previousDwellTimeSub                                                      = (int)SciMsg.SC_TIME_FOREVER;

        private enum ShortcutType
        {
            ShortcutType_AutoCompletion,
            ShortcutType_ReferenceLink
        }
        #endregion

        #region [Events]
        public delegate void FileEventDelegate(object source, string file, RTextNppPlugin.Scintilla.View View);
        public event FileEventDelegate PreviewFileClosed;
        public event FileEventDelegate BufferActivated;

        public delegate void ScintillaFocusChangedEventDelegate(IntPtr sciPtr, bool hasFocus);
        public event ScintillaFocusChangedEventDelegate ScintillaFocusChanged;

        public delegate void ScintillaZoomChangedEventDelegate(IntPtr sciPtr, int newZoomLevel);
        public event ScintillaZoomChangedEventDelegate ScintillaZoomChanged;

        public delegate void UiPainted();
        public event UiPainted ScintillaUiPainted;

        public delegate void UiUpdated(SCNotification notification);
        public event UiUpdated ScintillaUiUpdated;

        public delegate void ShutdownDelegate();
        public event ShutdownDelegate OnNotepadShutdown;

        public delegate void DwellDelegate(IntPtr sciPtr, int position, Point point);
        public event DwellDelegate OnDwellStarting;
        public event DwellDelegate OnDwellEnding;
        #endregion

        #region [Startup/CleanUp]

        private Plugin()
        {
            _linesVisibilityObserver = new LineVisibilityObserver(_nppHelper, this);
            _settings                = new Settings(_nppHelper);
            _styleObserver           = new StyleConfigurationObserver(_nppHelper);
            _connectorManager        = new ConnectorManager(_settings, _nppHelper, this);
            _mouseDwellObserver      = new MouseDwellObserver(this, _nppHelper);
            _consoleOutput           = new PersistentWpfControlHost<ConsoleOutputForm>(Settings.RTextNppSettings.ConsoleWindowActive, new ConsoleOutputForm(_connectorManager, _nppHelper, _styleObserver, _settings, _linesVisibilityObserver, _mouseDwellObserver), _settings, _nppHelper);
            _options                 = new Options(_settings);
            _fileObserver            = new FileModificationObserver(_settings, _nppHelper);
            _autoCompletionForm      = new AutoCompletionWindow(_connectorManager, _nppHelper, _nativeHelpers);
            _linkTargetsWindow       = new LinkTargetsWindow(_nppHelper, _settings, _connectorManager);
        }

        public void PluginCleanUp()
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

        public void CommandMenuInit()
        {
            CSScriptIntellisense.KeyInterceptor.Instance.Install();
            _nppHelper.InitializeNativePointers();
            SetCommand((int)Constants.NppMenuCommands.ConsoleWindow, Properties.Resources.RTEXT_SHOW_OUTPUT_WINDOW, ShowConsoleOutput, new ShortcutKey(false, true, true, Keys.R));
            SetCommand((int)Constants.NppMenuCommands.Options, Properties.Resources.RTEXT_SHOW_OPTIONS_WINDOW, ModifyOptions, new ShortcutKey(true, false, true, Keys.R));
            SetCommand((int)Constants.NppMenuCommands.AutoCompletion, Properties.Resources.AUTO_COMPLETION_DESC, StartAutoCompleteSession, Properties.Resources.AUTO_COMPLETION_SHORTCUT);
            SetCommand((int)Constants.NppMenuCommands.AutoCompletion, Properties.Resources.FIND_ALL_REFS_DESC, ShowReferenceLinks, Properties.Resources.FIND_ALL_REFS_SHORTCUT);
            _connectorManager.Initialize(_nppData);
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
            _currentZoomLevel = Npp.Instance.GetZoomLevel(_nppHelper.CurrentScintilla);
            _autoCompletionForm.OnZoomLevelChanged(_currentZoomLevel);
            _linkTargetsWindow.OnZoomLevelChanged(_currentZoomLevel);
            _linkTargetsWindow.IsVisibleChanged += OnLinkTargetsWindowIsVisibleChanged;
            _hasMainScintillaFocus              = _nativeHelpers.IGetFocus() == _nppHelper.MainScintilla;
            _hasSecondScintillaFocus            = _nativeHelpers.IGetFocus() == _nppHelper.SecondaryScintilla;
            #if DEBUG
            Debugger.Launch();
            #endif
            _styleObserver.EnableStylesObservation();
            //set up notifications
            //_nppHelper.SetModEventMask((int)(SciMsg.SC_MOD_INSERTTEXT | SciMsg.SC_MOD_DELETETEXT));
        }

        public void LoadSettings()
        {
            if (_settings.Get<bool>(Settings.RTextNppSettings.ConsoleWindowActive))
            {
                ShowConsoleOutput();
                _nppHelper.SendMessage(_nppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK, new IntPtr(_funcItems.Items[0]._cmdID), new IntPtr(1));
            }

            _nppMsgInterceptpr                                   = new NotepadMessageInterceptor(_nppData._nppHandle);
            _scintillaMainMsgInterceptor                         = new ScintillaMessageInterceptor(_nppData._scintillaMainHandle);
            _scintillaMainMsgInterceptor.ScintillaFocusChanged   += OnMainScintillaFocusChanged;
            _scintillaSecondMsgInterceptor                       = new ScintillaMessageInterceptor(_nppData._scintillaSecondHandle);
            _scintillaSecondMsgInterceptor.ScintillaFocusChanged += OnSecondScintillaFocusChanged;
            _scintillaMainMsgInterceptor.MouseWheelMoved         += OnScintillaMouseWheelMoved;
            _scintillaSecondMsgInterceptor.MouseWheelMoved       += OnScintillaMouseWheelMoved;
            _nppMsgInterceptpr.MenuLoopStateChanged              += OnMenuLoopStateChanged;
        }
                    
        #endregion
        
        #region [Commands]
        
        /**
         * Shows reference links.
         */
        void ShowReferenceLinks()
        {

        }

        /**
         * Shows the automatic completion list.
         */
        void StartAutoCompleteSession()
        {
            HandleErrors(() =>
            {
                if (FileUtilities.IsRTextFile(_settings, Npp.Instance))
                {
                    if (!_autoCompletionForm.IsVisible)
                    {
                        int aCurrentPosition = Npp.Instance.GetCaretPosition(_nppHelper.CurrentScintilla);
                        if (aCurrentPosition >= 0)
                        {
                            int aLineNumber                    = Npp.Instance.GetLineNumber(_nppHelper.CurrentScintilla);
                            int aStartPos                      = _nppHelper.GetLineStart(aLineNumber, _nppHelper.CurrentScintilla);
                            //get text from start till current line end
                            string aContextBlock = Npp.Instance.GetTextBetween(0, _nppHelper.GetLineEnd(_nppHelper.GetCaretPosition(_nppHelper.CurrentScintilla), aLineNumber, _nppHelper.CurrentScintilla));
                            ContextExtractor aExtractor        = new ContextExtractor(aContextBlock, _nppHelper.GetLengthToEndOfLine(aLineNumber, _nppHelper.GetCaretPosition(_nppHelper.CurrentScintilla)));



                            //if auto completion is inside comment, notation, name, string just return
                            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(aLineNumber, aCurrentPosition, aStartPos, Npp.Instance, _nppHelper.CurrentScintilla);
                            //if a token is found then the window should appear at the start of it, else it should appear at the caret
                            Point aCaretPoint = Npp.Instance.GetCaretScreenLocationForForm(_nppHelper.CurrentScintilla);
                            if (aTokenizer.TriggerToken.HasValue &&
                                aTokenizer.TriggerToken.Value.Type  != RTextTokenTypes.Comma &&
                                aTokenizer.TriggerToken.Value.Type  != RTextTokenTypes.Space &&
                                (aTokenizer.TriggerToken.Value.Type != RTextTokenTypes.Label ||
                                 aCurrentPosition < (aTokenizer.TriggerToken.Value.BufferPosition + aTokenizer.TriggerToken.Value.Context.Length)))
                            {
                                aCaretPoint = Npp.Instance.GetCaretScreenLocationRelativeToPosition(aTokenizer.TriggerToken.Value.BufferPosition, _nppHelper.CurrentScintilla);
                            }
                            _autoCompletionForm.Dispatcher.Invoke((MethodInvoker)(async() =>
                            {
                                _autoCompletionForm.Left = aCaretPoint.X;
                                _autoCompletionForm.Top  = aCaretPoint.Y;
                                Utilities.VisualUtilities.SetOwnerFromNppPlugin(_autoCompletionForm);
                                await _autoCompletionForm.AugmentAutoCompletion(aExtractor, aCaretPoint, aTokenizer, _isAutoCompletionShortcutActive);
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
                    _nppHelper.SendMessage(_nppData._nppHandle, (NppMsg)WinMsg.WM_COMMAND, new IntPtr((int)NppMenuCmd.IDM_EDIT_AUTOCOMPLETE));
                }
            });
        }
        
        /**
         * \brief Modify options callback from plug-in menu.
         */
        void ModifyOptions()
        {
            _nppHelper.SendMessage(_nppData._nppHandle, NppMsg.NPPM_MODELESSDIALOG, new IntPtr((int)NppMsg.MODELESSDIALOGADD), _options.Handle);
            if (_options.ShowDialog(Control.FromHandle(_nppData._nppHandle)) == DialogResult.OK)
            {
                _options.SaveSettings();
            }
            else
            {
                _options.RestoreSettings();
            }
            _nppHelper.SendMessage(_nppData._nppHandle, NppMsg.NPPM_MODELESSDIALOG, new IntPtr((int)NppMsg.MODELESSDIALOGREMOVE), _options.Handle);
        }
        
        void ShowConsoleOutput()
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
                // define the default docking behavior
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = Constants.Scintilla.PLUGIN_NAME;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);
                _consoleOutput.CmdId = _funcItems.Items[(int)Constants.NppMenuCommands.ConsoleWindow]._cmdID;
                _nppHelper.SendMessage(_nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, IntPtr.Zero, _ptrNppTbData);
                _nppHelper.SendMessage(_nppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK, new IntPtr(_funcItems.Items[0]._cmdID), new IntPtr(1));
            }
            else
            {
                if (!_consoleOutput.Visible)
                {
                    _nppHelper.SendMessage(_nppData._nppHandle, NppMsg.NPPM_DMMSHOW, IntPtr.Zero, _consoleOutput.Handle);
                }
                else
                {
                    _nppHelper.SendMessage(_nppData._nppHandle, NppMsg.NPPM_DMMHIDE, IntPtr.Zero, _consoleOutput.Handle);
                }
            }
            _consoleOutput.Focus();
        }
        #endregion

        #region [Properties]

        public static Plugin Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new Plugin();
                        }
                    }
                }
                return instance;
            }
        }

        public FuncItems FuncItems
        {
            get
            {
                return _funcItems;
            }
        }

        public NppData NppData
        {
            get
            {
                return _nppData;
            }
            set
            {
                _nppData = value;
            }
        }

        public bool HasMainSciFocus
        {
            get
            {
                return _hasMainScintillaFocus;
            }
        }

        public bool HasSecondSciFocus
        {
            get
            {
                return _hasSecondScintillaFocus;
            }
        }

        /**
         * Gets file modification observer. Can be used to save all opened files of a workspace.
         *
         * \return  The file observer.
         */
        public FileModificationObserver FileObserver
        {
            get
            {
                return _fileObserver;
            }
        }

        public VisibilityInfo MainVisibilityInfo
        {
            get
            {
                return _linesVisibilityObserver.MainVisibilityInfo;
            }
        }

        public VisibilityInfo SubVisibilityInfo
        {
            get
            {
                return _linesVisibilityObserver.SubVisibilityInfo;
            }
        }

        #endregion

        #region [Event Handlers]

        private void OnKeyInterceptorKeyDown(Keys key, int repeatCount, ref bool handled)
        {
            _isAutoCompletionShortcutActive = false;
            //do not auto complete when multi selecting, when menu loop is active, when no RText file is open
            if (FileUtilities.IsRTextFile(_settings, Npp.Instance) && (Npp.Instance.GetSelections() == 1) && HasScintillaFocus() && !_isMenuLoopInactive)
            {
                CSScriptIntellisense.Modifiers modifiers = CSScriptIntellisense.KeyInterceptor.GetModifiers();

                foreach (var shortcut in internalShortcuts.Keys)
                {
                    //modifiers check
                    if (modifiers.IsCtrl == shortcut.IsCtrl && modifiers.IsShift == shortcut.IsShift && modifiers.IsAlt == shortcut.IsAlt)
                    {
                        if ((shortcut.IsSet && (byte)key == shortcut._key) || !shortcut.IsSet)
                        {
                            //shortcut matches, find out which one it is
                            switch (internalShortcuts[shortcut].Item3)
                            {
                                case ShortcutType.ShortcutType_AutoCompletion:
                                    _isAutoCompletionShortcutActive = true;
                                    var handler = internalShortcuts[shortcut];
                                    handled = !_autoCompletionForm.IsVisible;
                                    if (!_autoCompletionForm.IsVisible)
                                    {
                                        var res = AsyncInvoke(handler.Item2);
                                    }
                                    //do nothing if form is already visible
                                    return;
                                case ShortcutType.ShortcutType_ReferenceLink:
                                    if (modifiers.IsAlt && modifiers.IsCtrl)
                                    {
                                        _linkTargetsWindow.IsKeyboardShortCutActive(true);
                                    }
                                    break;
                                default:
                                    //nothing to do
                                    break;
                            }
                        }
                    }
                }
                //if any modifier key is pressed - ignore this key press
                if (modifiers.IsCtrl || modifiers.IsAlt)
                {
                    if (_autoCompletionForm.IsVisible)
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
                        Npp.Instance.DeleteBack(1, _nppHelper.CurrentScintilla);
                        //in case of empty trigger token form needs to close
                        _autoCompletionForm.OnKeyPressed(Constants.BACKSPACE);
                        if (_autoCompletionForm.CharProcessAction == ViewModels.AutoCompletionViewModel.CharProcessResult.ForceClose)
                        {
                            CommitAutoCompletion(false);
                        }
                        break;
                    case Keys.Delete:
                        handled = true;
                        Npp.Instance.DeleteFront(_nppHelper.CurrentScintilla);
                        _autoCompletionForm.OnKeyPressed();
                        break;
                    case Keys.Space:
                        handled = true;
                        //if completion list is visible, and there is a trigger token other than comma or space and there is some selected option
                        if (_autoCompletionForm.Completion != null && _autoCompletionForm.Completion.IsSelected)
                        {
                            CommitAutoCompletion(true);
                            Npp.Instance.AddText(Constants.SPACE.ToString(), _nppHelper.CurrentScintilla);
                            return;
                        }
                        Npp.Instance.AddText(Constants.SPACE.ToString(), _nppHelper.CurrentScintilla);
                        _autoCompletionForm.OnKeyPressed(Constants.SPACE);
                        break;
                    case Keys.Return:
                    case Keys.Tab:
                        if (_autoCompletionForm.IsVisible)
                        {
                            handled = true;
                            if (_autoCompletionForm.TriggerPoint.HasValue && String.IsNullOrWhiteSpace(_autoCompletionForm.TriggerPoint.Value.Context) &&
                                (_autoCompletionForm.Completion == null || (_autoCompletionForm.Completion != null && !_autoCompletionForm.Completion.IsSelected))
                               )
                            {
                                Npp.Instance.AddText(Constants.TAB.ToString(), _nppHelper.CurrentScintilla);
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
                        if (_autoCompletionForm.IsVisible)
                        {
                            CommitAutoCompletion(true);
                            Npp.Instance.AddText(Constants.COMMA.ToString(), _nppHelper.CurrentScintilla);
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
                    case Keys.OemOpenBrackets:
                    case Keys.OemCloseBrackets:
                    case Keys.OemQuotes:
                        handled = false;
                        break;
                    default:
                        //convert virtual key to w/e it has to be converted to
                        var mappedChar = _nativeHelpers.GetCharsFromKeys(key, modifiers.IsShift || modifiers.IsCapsLock, modifiers.IsAlt && modifiers.IsCtrl);
                        //only handle letters and whitespace chars
                        if (mappedChar != string.Empty)
                        {
                            Npp.Instance.AddText(mappedChar, _nppHelper.CurrentScintilla);
                            handled = true;
                            OnCharTyped(mappedChar[0]);
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

        public void OnHotspotClicked()
        {
            _actionAfterUiUpdateHandler.TriggerHandler(new ActionWrapper<object, string, int, IntPtr>(_nppHelper.JumpToLine, _linkTargetsWindow.Targets.First().FilePath, Int32.Parse(_linkTargetsWindow.Targets.First().Line), _nppHelper.CurrentScintilla));
        }

        public void OnFileSaved()
        {
            //find out file and forward it to appropriate connector
            _connectorManager.OnFileSaved(_nppHelper.GetCurrentFilePath());
        }

        public void OnBufferActivated(IntPtr hwndFrom, int bufferid)
        {
            string aFileOpened = _nppHelper.GetPathFromBufferId(bufferid);
            var aCurrentView   = _nppHelper.CurrentView;
            //update visibility info before everything else - clients of buffer activated event can then have access to most actual visibility information
            _linesVisibilityObserver.OnBufferActivated(aFileOpened, aCurrentView);
            if (BufferActivated != null)
            {
                BufferActivated(this, aFileOpened, aCurrentView);
            }
            _fileObserver.OnFileOpened(aFileOpened);
            _linkTargetsWindow.CancelPendingRequest();
            var aSciPtr = aCurrentView == Scintilla.View.Main ? _nppHelper.MainScintilla : _nppHelper.SecondaryScintilla;
            if(Utilities.FileUtilities.IsRTextFile(aFileOpened, _settings, _nppHelper))
            {
                SetDwellTime(aSciPtr, _nppHelper.GetMouseDwellTime(aSciPtr));
                //todo dwell time must be an option
                _nppHelper.SetMouseDwellTime(aSciPtr, 500);
            }
            else
            {
                _nppHelper.SetMouseDwellTime(aSciPtr, GetDwellTime(aSciPtr));
            }
        }

        private void OnLinkTargetsWindowIsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (_linkTargetsWindow.Visibility == System.Windows.Visibility.Hidden)
            {
                //give focus back to npp
                _nppHelper.GrabFocus(_nppHelper.CurrentScintilla);
            }
        }
        
        private void OnSecondScintillaFocusChanged(object source, ScintillaMessageInterceptor.ScintillaFocusChangedEventArgs e)
        {
            HandleScintillaFocusChanged(e, ref _hasSecondScintillaFocus);
            if(ScintillaFocusChanged != null)
            {
                ScintillaFocusChanged(_nppHelper.SecondaryScintilla, _hasSecondScintillaFocus);
            }
        }
        
        private void OnMainScintillaFocusChanged(object source, ScintillaMessageInterceptor.ScintillaFocusChangedEventArgs e)
        {
            HandleScintillaFocusChanged(e, ref _hasMainScintillaFocus);
            if (ScintillaFocusChanged != null)
            {
                ScintillaFocusChanged(_nppHelper.MainScintilla, _hasMainScintillaFocus);
            }
        }
        
        private void OnMenuLoopStateChanged(object source, NotepadMessageInterceptor.MenuLoopStateChangedEventArgs e)
        {
            if ((_isMenuLoopInactive = e.IsMenuLoopActive))
            {
                CommitAutoCompletion(false);
            }
        }
        
        /**
         * \brief   Executes action when mouse wheel movement is detected.
         *          This is used to route low level windows events from scintilla to the plug-in,
         *          which would otherwise be lost. e.g. for scrolling via a touchpad.
         *
         * \param   msg     The message.
         * \param   wParam  The parameter.
         * \param   lParam  The parameter.
         *
         * \todo    Handle this for other windows as well, e.g. reference links
         */
        private void OnScintillaMouseWheelMoved(object source, ScintillaMessageInterceptor.MouseWheelMovedEventArgs e)
        {
            e.Handled = _autoCompletionForm.OnMessageReceived(e.Msg, e.WParam, e.LParam);
        }

        internal void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            _nppHelper.SendMessage(_nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, new IntPtr(_funcItems.Items[(int)Constants.NppMenuCommands.ConsoleWindow]._cmdID), pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }
        
        /**
         * Occurs when undo operation exists for the current document.
         */
        internal void OnFileConsideredModified()
        {
            _fileObserver.OnFilemodified(Npp.Instance.GetCurrentFilePath());
        }
        
        /**
         * Occurs when no undo operation exist for the current document.
         */
        internal void OnFileConsideredUnmodified()
        {
            _fileObserver.OnFileUnmodified(Npp.Instance.GetCurrentFilePath());
        }
               
        /**
         * Scintilla notification that the zoom level has been changed.
         *
         */
        internal void OnZoomLevelModified()
        {
            int aNewZoomLevel = Npp.Instance.GetZoomLevel(_nppHelper.CurrentScintilla);
            if(aNewZoomLevel != _currentZoomLevel)
            {
                _currentZoomLevel = aNewZoomLevel;
                _autoCompletionForm.OnZoomLevelChanged(_currentZoomLevel);
                _linkTargetsWindow.OnZoomLevelChanged(_currentZoomLevel);
            }
            if(ScintillaZoomChanged != null)
            {
                ScintillaZoomChanged(_nppHelper.CurrentScintilla, aNewZoomLevel);
            }
        }

        internal void OnPreviewFileClosed()
        {
            if (PreviewFileClosed != null)
            {
                PreviewFileClosed( typeof(Plugin), _nppHelper.GetCurrentFilePath(), _nppHelper.CurrentView);
            }
        }

        internal void OnScnPainted()
        {
            if(ScintillaUiPainted != null)
            {
                ScintillaUiPainted();
            }
        }

        internal void OnScnModified(SCNotification nc)
        {
            Trace.WriteLine(String.Format("Text deleted : {0}", (nc.modificationType & (int)SciMsg.SC_MOD_DELETETEXT) != 0 ));
            Trace.WriteLine(String.Format("Text added : {0}", (nc.modificationType & (int)SciMsg.SC_MOD_INSERTTEXT) != 0));
            Trace.WriteLine(String.Format("Text position : {0}", nc.position));
            Trace.WriteLine(String.Format("Text length : {0}", nc.length));

            //if text is deleted, the position denotes the final position after the deletion and the length the deleted length

            //if text is added, the position denotes the initial position and the length, the length of the added test
        }

        internal void OnScnUpdateUi(SCNotification notification)
        {
            if(ScintillaUiUpdated != null)
            {
                ScintillaUiUpdated(notification);
            }
        }

        private void OnKeyInterceptorKeyUp(Keys key, int repeatCount, ref bool handled)
        {
            CSScriptIntellisense.Modifiers modifiers = CSScriptIntellisense.KeyInterceptor.GetModifiers();
            if (!modifiers.IsAlt || !modifiers.IsCtrl)
            {
                _linkTargetsWindow.IsKeyboardShortCutActive(false);
            }
        }

        internal void BeforeShutdown()
        {
            if (OnNotepadShutdown != null)
            {
                OnNotepadShutdown();
            }
            _connectorManager.ReleaseConnectors();
        }

        internal void OnDwellEnd(IntPtr sciPtr, int p, Point point)
        {
            if(OnDwellEnding != null)
            {
                OnDwellEnding(sciPtr, p, point);
            }
        }

        internal void OnDwellStart(IntPtr sciPtr, int p, Point point)
        {
            if(OnDwellStarting != null)
            {
                OnDwellStarting(sciPtr, p, point);
            }
        }

        #endregion

        #region [Helpers]

        private void OnCharTyped(char c)
        {
            if (!char.IsControl(c) && !char.IsWhiteSpace(c))
            {
                if (!_autoCompletionForm.IsVisible)
                {
                    //do not start auto completion with whitespace char...
                    if (!char.IsWhiteSpace(c))
                    {
                        var res = AsyncInvoke(StartAutoCompleteSession);
                    }
                }
                else
                {
                    _autoCompletionForm.OnKeyPressed(c);
                    if (_autoCompletionForm.CharProcessAction == ViewModels.AutoCompletionViewModel.CharProcessResult.ForceClose)
                    {
                        _autoCompletionForm.Hide();
                    }
                }
            }
        }

        private void CommitAutoCompletion(bool replace)
        {
            if (replace && _autoCompletionForm.TriggerPoint != null)
            {
                //use current selected item to replace token
                if (_autoCompletionForm.Completion != null && _autoCompletionForm.Completion.IsSelected)
                {
                    Npp.Instance.ReplaceWordFromToken(_autoCompletionForm.TriggerPoint, _autoCompletionForm.Completion.InsertionText, _nppHelper.CurrentScintilla);
                }
            }
            _autoCompletionForm.Hide();
        }

        private async Task AsyncInvoke(Action action)
        {
            if (!_invokeInProgress)
            {
                _invokeInProgress = true;
                await Task.Delay(10);
                action();
                _invokeInProgress = false;
            }
        }

        private void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, string shortcut)
        {
            SetCommand(index, commandName, functionPointer, new ShortcutKey(shortcut), false);
        }

        private void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer)
        {
            SetCommand(index, commandName, functionPointer, new ShortcutKey(), false);
        }

        private void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, ShortcutKey shortcut)
        {
            SetCommand(index, commandName, functionPointer, shortcut, false);
        }

        private void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, bool checkOnInit)
        {
            SetCommand(index, commandName, functionPointer, new ShortcutKey(), checkOnInit);
        }

        private void SetCommand(int index, string commandName, NppFuncItemDelegate functionPointer, ShortcutKey shortcut, bool checkOnInit)
        {
            FuncItem funcItem = new FuncItem();
            funcItem._cmdID = index;
            funcItem._itemName = commandName;
            if (functionPointer != null)
            {
                funcItem._pFunc = new NppFuncItemDelegate(functionPointer);
            }
            if (shortcut._key != 0)
            {
                funcItem._pShKey = shortcut;
            }
            funcItem._init2Check = checkOnInit;
            _funcItems.Add(funcItem);
        }

        private void HandleScintillaFocusChanged(ScintillaMessageInterceptor.ScintillaFocusChangedEventArgs e, ref bool hasFocus)
        {
            hasFocus = e.Focused;
            IntPtr aWindowWithFocus = unchecked((IntPtr)(long)(ulong)e.WindowHandle);
            IntPtr aAutoCompletionWindow = VisualUtilities.HwndFromWpfWindow(_autoCompletionForm);
            if (!hasFocus && e.WindowHandle != null && aWindowWithFocus != aAutoCompletionWindow)
            {
                if (aWindowWithFocus != IntPtr.Zero && aWindowWithFocus != _nppHelper.CurrentScintilla)
                {
                    CommitAutoCompletion(false);
                }
            }
        }

        private bool HasScintillaFocus()
        {
            if (_hasMainScintillaFocus || _hasSecondScintillaFocus)
            {
                return true;
            }
            else
            {
                if(_autoCompletionForm.IsVisible)
                {
                    Npp.Instance.GrabFocus(_nppHelper.CurrentScintilla);
                    return true;
                }
            }
            return false;
        }
        
        /**
         * Enumerates bind internal shortcuts in this collection.
         *
         * \return  An enumerator that allows for-each to be used to process internal shortcuts.
         */
        private IEnumerable<Keys> BindInteranalShortcuts()
        {
            var uniqueKeys = new List<Keys>();
            AddInternalShortcuts( Properties.Resources.AUTO_COMPLETION_SHORTCUT,
                                  Properties.Resources.AUTO_COMPLETION_DESC,
                                  StartAutoCompleteSession, ShortcutType.ShortcutType_AutoCompletion, uniqueKeys);
            AddInternalShortcuts( Properties.Resources.FIND_ALL_REFS_SHORTCUT,
                                  Properties.Resources.FIND_ALL_REFS_DESC,
                                  ShowReferenceLinks, ShortcutType.ShortcutType_ReferenceLink, uniqueKeys);
            return uniqueKeys;
        }

        private void AddInternalShortcuts(string shortcutSpec, string displayName, Action handler, ShortcutType type, IList<Keys> uniqueKeys)
        {
            ShortcutKey aShortcut = new ShortcutKey(shortcutSpec);

            internalShortcuts.Add(aShortcut, new Tuple<string, Action, ShortcutType>(displayName, handler, type));
            
            if (!uniqueKeys.Contains((Keys)aShortcut._key))
            {
                uniqueKeys.Add((Keys)aShortcut._key);
            }
        }
        
        /**
         * Handles exceptions that may be thrown by the action.
         *
         * \param   action  The action to be executed.
         */
        
        private void HandleErrors(Action action)
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

        private void SetDwellTime(IntPtr sciPtr, int time)
        {
            if(sciPtr == _nppHelper.MainScintilla)
            {
                _previousDwellTimeMain = time;
            }
            else
            {
                _previousDwellTimeSub = time;
            }
        }

        private int GetDwellTime(IntPtr sciPtr)
        {
            if (sciPtr == _nppHelper.MainScintilla)
            {
                return _previousDwellTimeMain;
            }
            else
            {
                return _previousDwellTimeSub;
            }
        }

        #endregion
    }
}