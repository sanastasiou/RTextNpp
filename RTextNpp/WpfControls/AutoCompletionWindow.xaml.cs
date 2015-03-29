using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CSScriptIntellisense;
using RTextNppPlugin.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.ViewModels;
using System.Runtime.InteropServices;


namespace RTextNppPlugin.WpfControls
{
    class AutoCompletionMouseMonitor : GlobalMouseHook
    {
        private event EventHandler<MouseEventExtArgs> _MouseClick;

        public event EventHandler<MouseEventExtArgs> MouseClick
        {
            add
            {
                if (_MouseClick == null)
                {
                    EnsureSubscribedToGlobalMouseEvents();
                    _MouseClick += value;
                }
            }
            remove
            {
                if (_MouseClick != null)
                {
                    _MouseClick -= value;
                    TryUnsubscribeFromGlobalMouseEvents();
                }
            }
        }

        private event EventHandler<MouseEventExtArgs> _MouseWheel;

        public event EventHandler<MouseEventExtArgs> MouseWheel
        {
            add
            {
                if (_MouseWheel == null)
                {
                    EnsureSubscribedToGlobalMouseEvents();
                    _MouseWheel += value;
                }
            }
            remove
            {
                if (_MouseWheel != null)
                {
                    _MouseWheel -= value;
                    TryUnsubscribeFromGlobalMouseEvents();
                }
            }
        }

        private event EventHandler<MouseEventExtArgs> _MouseDoubleClick;

        public event EventHandler<MouseEventExtArgs> MouseDoubleClick
        {
            add
            {
                if (_MouseDoubleClick != null)
                {
                    EnsureSubscribedToGlobalMouseEvents();
                    _MouseDoubleClick += value;
                }
            }
            remove
            {
                if (_MouseDoubleClick != null)
                {
                    _MouseDoubleClick -= value;
                    TryUnsubscribeFromGlobalMouseEvents();
                }
            }
        }

        public override int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                VisualUtilities.MouseMessages aMsg = (VisualUtilities.MouseMessages)wParam.ToInt32();
                //Marshall the data from callback.
                MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

                //detect button clicked
                System.Windows.Forms.MouseButtons button = System.Windows.Forms.MouseButtons.None;
                short mouseDelta = 0;
                int clickCount = 0;
                switch (aMsg)
                {
                    case VisualUtilities.MouseMessages.WM_LBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Left;
                        clickCount = 1;
                        break;
                    case VisualUtilities.MouseMessages.WM_RBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Right;
                        clickCount = 1;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCXBUTTONDBLCLK:
                        button = System.Windows.Forms.MouseButtons.Left;
                        clickCount = 2;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCXBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Left;
                        clickCount = 1;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCMBUTTONDBLCLK:
                        button = System.Windows.Forms.MouseButtons.Middle;
                        clickCount = 2;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCMBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Middle;
                        clickCount = 1;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCRBUTTONDBLCLK:
                        button = System.Windows.Forms.MouseButtons.Right;
                        clickCount = 1;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCRBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Right;
                        clickCount = 1;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCLBUTTONDBLCLK:
                        button = System.Windows.Forms.MouseButtons.Left;
                        clickCount = 2;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCLBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Left;
                        clickCount = 1;
                        break;
                    default:
                        return CallNextHookEx(_MouseHookHandle, nCode, wParam, lParam);
                }

                //generate event 
                MouseEventExtArgs e = new MouseEventExtArgs(button, 1, mouseHookStruct.Point.X, mouseHookStruct.Point.Y, mouseDelta);

                if (_MouseClick != null && clickCount == 1)
                {
                    _MouseClick.Invoke(null, e);
                }
                if (_MouseDoubleClick != null && clickCount == 2)
                {
                    _MouseDoubleClick.Invoke(null, e);
                }

                if (e.Handled)
                {
                    return -1;
                }
            }

            //call next hook
            return CallNextHookEx(_MouseHookHandle, nCode, wParam, lParam);
        }

        override protected void TryUnsubscribeFromGlobalMouseEvents()
        {
            //if no subsribers are registered unsubsribe from hook
            if (_MouseClick    == null &&
                _MouseWheel    == null)
            {
                ForceUnsunscribeFromGlobalMouseEvents();
            }
        }
    }

    public partial class AutoCompletionWindow : System.Windows.Window, IDisposable, IWin32MessageReceptor, IWindowPosition
    {
        #region [DataMembers]

        DelayedKeyEventHandler _delayedFilterEventHandler           = null;
        KeyInterceptor _keyMonitor                                  = new KeyInterceptor();
        AutoCompletionMouseMonitor _autoCompletionMouseMonitor      = new AutoCompletionMouseMonitor();

        #endregion

        #region [Interface]
        public double ZoomLevel
        {
            get
            {
                return GetModel().ZoomLevel;
            }
        }

        /**
         * \brief   Gets or sets a value indicating whether this window appears on top of a token.
         *
         */
        public bool IsOnTop { get; set; }

        public double CurrentHeight { get { return Height; } }

        public AutoCompletionWindow()
        {
            InitializeComponent();
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Down);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Up);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageUp);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageDown);
            _keyMonitor.KeyDown += OnKeyMonitorKeyDown;
            _delayedFilterEventHandler = new DelayedKeyEventHandler(this.PostProcessKeyPressed, 100);
            IsOnTop = false;
        }

        void OnAutoCompletionMouseMonitorMouseWheelMoved(object sender, MouseEventExtArgs e)
        {
            ScrollList(e.Delta > 0 ? System.Windows.Forms.Keys.Up : System.Windows.Forms.Keys.Down, 3);
            e.Handled = true;
        }

        void OnAutoCompletionMouseMonitorMouseClick(object sender, MouseEventExtArgs e)
        {
            e.Handled = false;            
            if (!IsMouseInsideWindow())
            {
                this.Hide();
            }           
            else
            {
                //if a completion is suggested but not selected, clicking on it should select it
                //since index is not changed, this cannot be done with index selection changed event 
                if(GetModel().SelectedCompletion != null)
                {
                    GetModel().SelectedCompletion.IsSelected = true;
                }
            }
        }

        void OnKeyMonitorKeyDown(System.Windows.Forms.Keys key, int repeatCount, ref bool handled)
        {
            switch (key)
            {
                case System.Windows.Forms.Keys.Up:
                case System.Windows.Forms.Keys.Down:
                    handled = true;
                    ScrollList(key);
                    break;
                case System.Windows.Forms.Keys.PageDown:
                case System.Windows.Forms.Keys.PageUp:
                    ScrollList(key, 25);
                    handled = true;
                    break;
                default:
                    return;
            }
        }

        public void AugmentAutoCompletion(ContextExtractor extractor, System.Drawing.Point caretPoint, AutoCompletionTokenizer tokenizer, ref bool request)
        {
            GetModel().AugmentAutoCompletion(extractor, caretPoint, tokenizer, ref request);
            CharProcessAction = GetModel().CharProcessAction;
            TriggerPoint      = GetModel().TriggerPoint;
        }

        public void PostProcessKeyPressed()
        {
            //handle this on UI thread since it will alter UI
            Dispatcher.Invoke(new Action(GetModel().Filter));
        }

        internal AutoCompletionViewModel.CharProcessResult CharProcessAction { get; private set; }

        public Tokenizer.TokenTag ? TriggerPoint {get;private set;}

        public void OnZoomLevelChanged(int newZoomLevel)
        {
            if (IsVisible)
            {
                //in case the form is visible - move it to the new place...
                var aCaretPoint = Npp.GetCaretScreenLocationForForm();
                if (GetModel().TriggerPoint.HasValue)
                {
                    aCaretPoint = CSScriptIntellisense.Npp.GetCaretScreenLocationRelativeToPosition(GetModel().TriggerPoint.Value.BufferPosition);
                }
                this.Left = aCaretPoint.X;
                this.Top  = aCaretPoint.Y;
            }
            Dispatcher.BeginInvoke(new Action<int>(GetModel().OnZoomLevelChanged), newZoomLevel);
        }

        /**
         * Clears the completion.
         */
        internal void ClearCompletion()
        {
            GetModel().ClearSelectedCompletion();
        }

        internal AutoCompletionViewModel.Completion Completion { get { return GetModel().SelectedCompletion; } }

        public void OnKeyPressed(char c = '\0')
        {
            CharProcessAction = AutoCompletionViewModel.CharProcessResult.NoAction;
            if (IsVisible)
            {                
                _delayedFilterEventHandler.Cancel();
                //reparse line and find new trigger token
                GetModel().OnKeyPressed(c);
                TriggerPoint = GetModel().TriggerPoint;

                CharProcessAction = GetModel().CharProcessAction;
                if(CharProcessAction == AutoCompletionViewModel.CharProcessResult.MoveToRight)
                {
                    this.Left = Npp.GetCaretScreenLocationForForm(Npp.GetCaretPosition()).X;
                    CharProcessAction = AutoCompletionViewModel.CharProcessResult.NoAction;
                }
                //only filter if auto completion form can still remain open
                if(CharProcessAction != AutoCompletionViewModel.CharProcessResult.ForceClose)
                {
                    //do heavy lifting in here -> debounce many subsequent calls
                    _delayedFilterEventHandler.TriggerHandler();
                }
            }
        }
        #endregion

        #region [Helpers]
        private AutoCompletionViewModel GetModel()
        {
            return ((AutoCompletionViewModel)this.DataContext);
        }

        /**
         * @return  true if mouse inside window, false if not.
         */
        private bool IsMouseInsideWindow()
        {
            double dWidth = -1;
            double dHeight = -1;
            System.Windows.FrameworkElement pnlClient = this.Content as System.Windows.FrameworkElement;
            if (pnlClient != null)
            {
                dWidth = pnlClient.ActualWidth;
                dHeight = pnlClient.ActualHeight;
            }
            System.Windows.Point aPoint = Mouse.GetPosition(this);
            double xStart = 0.0;
            double xEnd = xStart + dWidth;
            double yStart = 0.0;
            double yEnd = yStart + dHeight;
            if (aPoint.X < xStart || aPoint.X > xEnd || aPoint.Y < yStart || aPoint.Y > yEnd)
            {
                return false;
            }
            return true;
        }

        /**
         * Scroll list.
         *
         * \param   key     The key.
         * \param   offset  (Optional) the offset.
         */
        private void ScrollList(System.Windows.Forms.Keys key, int offset = 1)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(GetModel().CompletionList);
            int aNewPosition = 0;
            switch (key)
            {
                case System.Windows.Forms.Keys.PageDown:
                case System.Windows.Forms.Keys.Down:
                    if (view.CurrentPosition + offset < AutoCompletionDatagrid.Items.Count)
                    {
                        aNewPosition = view.CurrentPosition + offset;
                        view.MoveCurrentToPosition(aNewPosition);
                    }
                    else
                    {
                        aNewPosition = AutoCompletionDatagrid.Items.Count - 1;
                        view.MoveCurrentToLast();
                    }
                    break;
                case System.Windows.Forms.Keys.PageUp:
                case System.Windows.Forms.Keys.Up:
                    if (view.CurrentPosition - offset >= 0)
                    {
                        aNewPosition = view.CurrentPosition - offset;
                        view.MoveCurrentToPosition(view.CurrentPosition - offset);

                    }
                    else
                    {
                        aNewPosition = 0;
                        view.MoveCurrentToFirst();
                    }
                    break;
            }
            GetModel().SelectPosition(aNewPosition);
            this.AutoCompletionDatagrid.ScrollIntoView(view.CurrentItem);
        }

        private void OnAutoCompletionDatagridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetModel().SelectPosition(((DataGrid)sender).SelectedIndex);
            if (GetModel().SelectedCompletion != null)
            {
                this.AutoCompletionDatagrid.ScrollIntoView(GetModel().SelectedCompletion);
            }
        }

        private void InstallMouseMonitorHooks()
        {
            _autoCompletionMouseMonitor.MouseClick += OnAutoCompletionMouseMonitorMouseClick;
            _autoCompletionMouseMonitor.MouseWheel += OnAutoCompletionMouseMonitorMouseWheelMoved;
        }


        private void UninstallMouseMonitorHooks()
        {
            _autoCompletionMouseMonitor.MouseClick -= OnAutoCompletionMouseMonitorMouseClick;
            _autoCompletionMouseMonitor.MouseWheel -= OnAutoCompletionMouseMonitorMouseWheelMoved;
        }
        #endregion

        #region EventHandlers

        public void OnAutoCompletionWindowSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            //recalculate y position in case auto completion window is on top of word and if it is visible ( thus avoding a two X offset being applied )
            if(IsOnTop && IsVisible)
            {
                var aHeightDiff = e.PreviousSize.Height - e.NewSize.Height;
                Top += aHeightDiff;
            }
        }

        private void OnAutoCompletionFormVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                _keyMonitor.Uninstall();
                this.AutoCompletionDatagrid.SelectedIndex = -1;
                GetModel().OnAutoCompletionWindowCollapsing();
                GetModel().ClearSelectedCompletion();
                UninstallMouseMonitorHooks();
                IsOnTop = false;
            }
            else
            {
                _keyMonitor.Install();
                InstallMouseMonitorHooks();
                _delayedFilterEventHandler.Cancel();
            }
        }

        #endregion

        #region IDisposable Members

        /**
         *
         * @brief   Performs application-defined tasks associated with freeing, releasing, or resetting
         *          unmanaged resources.
         *
         * @author  Stefanos Anastasiou
         * @date    26.01.2013
         *
         * ### summary  Performs application-defined tasks associated with freeing, releasing, or
         *              resetting unmanaged resources.
         */
        public void Dispose()
        {
            _keyMonitor.Uninstall();
            UninstallMouseMonitorHooks();
        }
        #endregion 

        #region IWin32MessageReceptor Members

        public bool OnMessageReceived(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if(IsVisible)
            {
                switch((VisualUtilities.WindowsMessage)msg)
                {
                    case  VisualUtilities.WindowsMessage.WM_MOUSEWHEEL:
                    var wheelMovement = (short)(wParam.ToUInt32() >> 16);
                    int x = unchecked((short)(long)lParam);
                    int y = unchecked((short)((long)lParam >> 16));
                    OnAutoCompletionMouseMonitorMouseWheelMoved(null, new MouseEventExtArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, wheelMovement));
                    return true;
                }

            }
            return false;
        }

        #endregion

        private void OnAutoCompletionDatagridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TriggerPoint != null && !String.IsNullOrEmpty(Completion.InsertionText))
            {
                //use current selected item to replace token
                Npp.ReplaceWordFromToken(TriggerPoint, Completion.InsertionText);
            }
            Hide();
            ClearCompletion();
        }
    }
}
