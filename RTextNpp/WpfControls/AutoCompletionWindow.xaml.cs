using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CSScriptIntellisense;
using RTextNppPlugin.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.ViewModels;
using System.Diagnostics;
using System.Threading.Tasks;


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

        DelayedEventHandler _delayedFilterEventHandler              = null;
        KeyInterceptor _keyMonitor                                  = new KeyInterceptor();
        AutoCompletionMouseMonitor _autoCompletionMouseMonitor      = new AutoCompletionMouseMonitor();
        ToolTip _previouslyOpenedToolTip                            = null;
        DelayedEventHandler<ToolTip> _delayedToolTipHandler         = null;
        

        #endregion

        #region [Interface]

        #region [IWindowPosition Members]
        /**
         * \brief   Gets or sets a value indicating whether this window appears on top of a token.
         *
         */
        public bool IsOnTop { get; set; }

        public double CurrentHeight { get { return Height; } }

        new double Width { get { return base.Width; } }

        new double Left
        {
            get
            {
                return base.Left;
            }
            set
            {
                base.Left = value;                
            }
        }

        new double Top
        {
            get
            {
                return base.Top;
            }
            set
            {
                base.Top = value;
            }
        }
        #endregion        

        public AutoCompletionWindow()
        {
            InitializeComponent();
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Down);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Up);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageUp);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageDown);
            _keyMonitor.KeyDown += OnKeyMonitorKeyDown;
            _delayedFilterEventHandler = new DelayedEventHandler(PostProcessKeyPressed, 100);
            _delayedToolTipHandler     = new DelayedEventHandler<System.Windows.Controls.ToolTip>(OnToolTipDelayedHandlerExpired, 1000, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            IsOnTop = false;
        }

        void OnAutoCompletionMouseMonitorMouseWheelMoved(object sender, MouseEventExtArgs e)
        {
            ScrollList(e.Delta > 0 ? System.Windows.Forms.Keys.Up : System.Windows.Forms.Keys.Down, 3);
            e.Handled = true;
        }

        void OnAutoCompletionMouseMonitorMouseClick(object sender, MouseEventExtArgs e)
        {
     
            if (!IsMouseInsideFrameworkElement(Content as System.Windows.FrameworkElement))
            {
                Hide();
            }           
            else
            {                
                if (IsMouseInsideFrameworkElement(AutoCompletionDatagrid as System.Windows.FrameworkElement))
                {
                    //if a completion is suggested but not selected, clicking on it should select it
                    //since index is not changed, this cannot be done with index selection changed event 
                    if (GetModel().SelectedCompletion != null)
                    {
                        GetModel().SelectedCompletion.IsSelected = true;
                    }
                }
                else
                {
                    //click is made outside of grid but inside window! - leave focus to editor i.e. make those areas not focusable
                    e.Handled = true;
                    Npp.GrabFocus();
                }
            }            
        }
        
        public new void Hide()
        {
            base.Hide();
            GetModel().OnAutoCompletionWindowCollapsing();
        }

        public async Task AugmentAutoCompletion(ContextExtractor extractor, System.Drawing.Point caretPoint, AutoCompletionTokenizer tokenizer)
        {
            await GetModel().AugmentAutoCompletion(extractor, caretPoint, tokenizer);
            CharProcessAction = GetModel().CharProcessAction;
            TriggerPoint      = GetModel().TriggerPoint;
        }

        public void PostProcessKeyPressed()
        {
            //handle this on UI thread since it will alter UI
            Dispatcher.Invoke(new Action(GetModel().Filter));
            if(GetModel().SelectedCompletion != null)
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(GetModel().CompletionList);
                if (view.CurrentItem != null)
                {
                    AutoCompletionDatagrid.ScrollIntoView(view.CurrentItem);
                }
            }
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
                Left = aCaretPoint.X;
                Top  = aCaretPoint.Y;
            }
            Dispatcher.BeginInvoke(new Action<int>(GetModel().OnZoomLevelChanged), newZoomLevel);
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
                    Left = Npp.GetCaretScreenLocationForForm(Npp.GetCaretPosition()).X;
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

        private void HideActiveTooltip(Border border)
        {
            ToolTip tp = border.ToolTip as ToolTip;
            if (tp != null)
            {
                tp.IsOpen = false;
            }
        }

        private void HidePreviouslyOpenedTooltip(ToolTip toolTip)
        {
            if (_previouslyOpenedToolTip != null)
            {
                _previouslyOpenedToolTip.IsOpen = false;
            }
            _previouslyOpenedToolTip = toolTip;
        }

        private double CalculateTooltipOffset()
        {
            double aCalculatedOffset = 0.0;
            var scrollViewer = GetScrollViewer(AutoCompletionDatagrid);
            System.Diagnostics.Trace.WriteLine(String.Format("Window width : {0}\nBorder width : {1}\nViewport width : {2}", Width, AutoCompletionListBorder.ActualWidth, scrollViewer.ViewportWidth));
            if ((Left + Width + Constants.MAX_AUTO_COMPLETION_TOOLTIP_WIDTH) > Npp.GetClientRectFromPoint(new System.Drawing.Point((int)Left, (int)Top)).Right)
            {
                if (scrollViewer.ComputedHorizontalScrollBarVisibility == System.Windows.Visibility.Visible)
                {
                    //offset here is the amount of pixels that the scrollbar can move to the right for some reason tooltip doesn't work correctly when offset is present...
                    //fix it by subtracting the current offset
                    double aCurrentOffset = scrollViewer.HorizontalOffset;
                    aCalculatedOffset += aCurrentOffset;
                }
                return aCalculatedOffset;
            }
            if(scrollViewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible)
            {
                aCalculatedOffset += System.Windows.SystemParameters.ScrollWidth;
            }
            else if (scrollViewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Collapsed)
            {
                //wpf bug - when a scrollbar gets collapsed due to filtering some leftover remain :s
                if (AutoCompletionListBorder.ActualWidth > scrollViewer.ViewportWidth)
                {                    
                    aCalculatedOffset += (System.Windows.SystemParameters.ScrollWidth - 11.0);
                }
            }
            
            if(scrollViewer.ComputedHorizontalScrollBarVisibility == System.Windows.Visibility.Visible)
            {
                //offset here is the amount of pixels that the scrollbar can move to the right
                double aCurrentOffset = scrollViewer.HorizontalOffset;
                double aExtendedWidth = scrollViewer.ExtentWidth;
                double aViewPortWidth = scrollViewer.ViewportWidth;
                double aMaxOffset     = aExtendedWidth - aViewPortWidth;
                aCalculatedOffset     -= (aMaxOffset - aCurrentOffset);
            }

            return aCalculatedOffset;
        }

        /**
         * Find the scrollbar out of a wpf control, e.g. DataGrid if it exists.
         *
         * \param   dep The dep.
         *
         * \return  The scrollbar.
         */
        private static ScrollViewer GetScrollViewer(DependencyObject dep)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dep); i++)
            {
                var child =  VisualTreeHelper.GetChild(dep, i);
                if (child != null && child is ScrollViewer)
                    return child as ScrollViewer;
                else
                {
                    ScrollViewer sub = GetScrollViewer(child);
                    if (sub != null)
                        return sub;
                }
            }
            return null;
        }

        private AutoCompletionViewModel GetModel()
        {
            return ((AutoCompletionViewModel)DataContext);
        }

        /**
         * @return  true if mouse inside window, false if not.
         */
        private bool IsMouseInsideFrameworkElement(System.Windows.FrameworkElement element)
        {
            double dWidth  = -1;
            double dHeight = -1;
            if (element != null)
            {
                dWidth  = element.ActualWidth;
                dHeight = element.ActualHeight;
            }
            System.Windows.Point aPoint = Mouse.GetPosition(element);
            
            double xStart = 0.0;
            double xEnd = xStart + dWidth;
            double yStart = 0.0;
            double yEnd = yStart + dHeight;
            
            if (aPoint.X < xStart || aPoint.X >= xEnd || aPoint.Y < yStart || aPoint.Y >= yEnd)
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
            AutoCompletionDatagrid.ScrollIntoView(view.CurrentItem);
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
        
        private void ShowDelayedToolTip(ToolTip tp)
        {
            tp.HorizontalOffset = CalculateTooltipOffset();
            tp.IsOpen           = true;
        }

        #endregion

        #region EventHandlers

        private void OnToolTipDelayedHandlerExpired(ToolTip tp)
        {
            Dispatcher.Invoke(new Action<ToolTip>(ShowDelayedToolTip), tp);
        }

        private void OnKeyMonitorKeyDown(System.Windows.Forms.Keys key, int repeatCount, ref bool handled)
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

        private void OnAutoCompletionDatagridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetModel().SelectPosition(((DataGrid)sender).SelectedIndex);
            if (GetModel().SelectedCompletion != null)
            {
                AutoCompletionDatagrid.ScrollIntoView(GetModel().SelectedCompletion);
            }
            //keep caret blinking after a selection has been made by clicking
            Npp.GrabFocus();
        }

        private void OnAutoCompletionBorderBackgroundUpdated(object sender, DataTransferEventArgs e)
        {
            var border  = sender as Border;
            var context = border.DataContext as AutoCompletionViewModel.Completion;
            ToolTip tp  = border.ToolTip as ToolTip;
            if (context.IsSelected)
            {
                tp.PlacementTarget = border;
                tp.Placement       = System.Windows.Controls.Primitives.PlacementMode.Right;
                HidePreviouslyOpenedTooltip(tp);
                _delayedToolTipHandler.TriggerHandler(tp);
            }
        }

        private void OnAutoCompletionBorderMouseEnter(object sender, MouseEventArgs e)
        {
            _delayedToolTipHandler.Cancel();
            Border border = (Border)sender;
            HidePreviouslyOpenedTooltip(border.ToolTip as ToolTip);            
        }

        private void OnAutoCompletionBorderToolTipOpening(object sender, ToolTipEventArgs e)
        {
            var border = sender as Border;
            var Tp = border.ToolTip as ToolTip;
            Tp.HorizontalOffset = CalculateTooltipOffset();
        }

        private void ToolTipOpenedHandler(object sender, RoutedEventArgs e)
        {
            ToolTip toolTip  = (ToolTip)sender;
            HidePreviouslyOpenedTooltip(toolTip);
        }

        /**
         * \brief   Resizes open auto completion list when the container's size change.
         */
        private void OnAutoCompletionContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsOnTop && IsVisible)
            {
                var aHeightDiff = e.PreviousSize.Height - e.NewSize.Height;
                Top += aHeightDiff;
            }
            else if(!IsOnTop && IsVisible)
            {
                if (!((e.NewSize.Height + Top ) <= Npp.GetClientRectFromControl(Npp.NppHandle).Bottom))
                {
                    //bottom exceeded - put list on top of word
                    Top = Npp.GetCaretScreenLocationForFormAboveWord().Y;
                    //problem here - we need to take into account the initial length of the list, otherwise our initial point is wrong if the list is not full
                    Top -= (int)(e.NewSize.Height);
                    IsOnTop = true;
                }
            }
            //position list in such a way that it doesn't get split into two monitors
            var rectFromPoint = Npp.GetClientRectFromPoint(new System.Drawing.Point((int)Left, (int)Top));
            //if the width of the auto completion window overlaps the right edge of the screen, then move the window at the left until no overlap is present
            if (rectFromPoint.Right < Left + e.NewSize.Width)
            {
                double dif = (Left + e.NewSize.Width) - rectFromPoint.Right;
                Left -= (int)dif;
            }
        }

        private void OnAutoCompletionFormVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {            
            if (!IsVisible)
            {
                _keyMonitor.Uninstall();
                AutoCompletionDatagrid.SelectedIndex = -1;
                GetModel().OnAutoCompletionWindowCollapsing();
                UninstallMouseMonitorHooks();
                IsOnTop = false;
                HidePreviouslyOpenedTooltip(null);
                _delayedToolTipHandler.Cancel();
            }
            else
            {
                _keyMonitor.Install();
                InstallMouseMonitorHooks();
                _delayedFilterEventHandler.Cancel();
            }
        }

        private void OnAutoCompletionDatagridMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (TriggerPoint != null && !String.IsNullOrEmpty(Completion.InsertionText))
            {
                //use current selected item to replace token
                Npp.ReplaceWordFromToken(TriggerPoint, Completion.InsertionText);
            }
            Hide();
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
    }
}
