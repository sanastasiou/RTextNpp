using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using CSScriptIntellisense;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.RText;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.ViewModels;


namespace RTextNppPlugin.WpfControls
{
    public partial class AutoCompletionWindow : System.Windows.Window, IDisposable, IWin32MessageReceptor, IWindowPosition
    {
        #region [DataMembers]

        DelayedEventHandler _delayedFilterEventHandler              = null;
        KeyInterceptor _keyMonitor                                  = new KeyInterceptor();
        GlobalClickInterceptor _autoCompletionMouseMonitor          = null;
        ToolTip _previouslyOpenedToolTip                            = null;
        DelayedEventHandler _delayedToolTipHandler                  = null;

        
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

        internal AutoCompletionWindow(ConnectorManager cmanager, IWin32 win32Helper)
        {
            InitializeComponent();
            _autoCompletionMouseMonitor = new GlobalClickInterceptor(win32Helper);
            DataContext = new ViewModels.AutoCompletionViewModel(cmanager);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Down);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Up);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageUp);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageDown);
            _keyMonitor.KeyDown += OnKeyMonitorKeyDown;
            _delayedFilterEventHandler = new DelayedEventHandler(new ActionWrapper(PostProcessKeyPressed), 150);
            _delayedToolTipHandler     = new DelayedEventHandler(new ActionWrapper<System.Windows.Controls.ToolTip>(OnToolTipDelayedHandlerExpired, null), 1000, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            IsOnTop = false;
        }

        void OnAutoCompletionMouseMonitorMouseWheelMoved(object sender, MouseEventExtArgs e)
        {
            ScrollList(e.Delta > 0 ? System.Windows.Forms.Keys.Up : System.Windows.Forms.Keys.Down, 3);
            e.Handled = true;
        }

        void OnAutoCompletionMouseMonitorMouseClick(object sender, MouseEventExtArgs e)
        {     
            //if an auto completion is taking to long, then it will not be visible, in this case hide is called to cancel the auto completion request
            if (!VisualUtilities.IsMouseInsideFrameworkElement(Content as System.Windows.FrameworkElement))
            {
                Hide();
            }           
            else
            {
                if (VisualUtilities.IsMouseInsideFrameworkElement(AutoCompletionDatagrid as System.Windows.FrameworkElement))
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
                    Npp.Instance.GrabFocus();
                }
            }            
        }
        
        internal new void Hide()
        {
            base.Hide();
            GetModel().OnAutoCompletionWindowCollapsing();
        }

        internal async Task AugmentAutoCompletion(ContextExtractor extractor, System.Drawing.Point caretPoint, AutoCompletionTokenizer tokenizer)
        {
            await GetModel().AugmentAutoCompletion(extractor, caretPoint, tokenizer);
            CharProcessAction = GetModel().CharProcessAction;
            TriggerPoint      = GetModel().TriggerPoint;
        }

        internal void PostProcessKeyPressed()
        {
            //handle this on UI thread since it will alter UI
            Dispatcher.Invoke((System.Windows.Forms.MethodInvoker)(() =>
            {
                if (!GetModel().Pending)
                {
                    GetModel().Filter();
                    if (GetModel().SelectedCompletion != null)
                    {
                        ICollectionView view = CollectionViewSource.GetDefaultView(GetModel().CompletionList);
                        if (view.CurrentItem != null)
                        {
                            AutoCompletionDatagrid.ScrollIntoView(view.CurrentItem);
                        }
                    }
                }
            }));
        }

        internal AutoCompletionViewModel.CharProcessResult CharProcessAction { get; private set; }

        internal Tokenizer.TokenTag ? TriggerPoint {get;private set;}

        internal void OnZoomLevelChanged(int newZoomLevel)
        {
            if (IsVisible)
            {
                //in case the form is visible - move it to the new place...
                var aCaretPoint = Npp.Instance.GetCaretScreenLocationForForm();
                if (GetModel().TriggerPoint.HasValue)
                {
                    aCaretPoint = Npp.Instance.GetCaretScreenLocationRelativeToPosition(GetModel().TriggerPoint.Value.BufferPosition);
                }
                Left = aCaretPoint.X;
                Top  = aCaretPoint.Y;
            }
            Dispatcher.BeginInvoke(new Action<int>(GetModel().OnZoomLevelChanged), newZoomLevel);
        }

        internal AutoCompletionViewModel.Completion Completion { get { return GetModel().SelectedCompletion; } }

        internal void OnKeyPressed(char c = '\0')
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
                    Left = Npp.Instance.GetCaretScreenLocationForForm(Npp.Instance.GetCaretPosition()).X;
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
            if ((Left + Width + Constants.MAX_AUTO_COMPLETION_TOOLTIP_WIDTH) > Npp.Instance.GetClientRectFromPoint(new System.Drawing.Point((int)Left, (int)Top)).Right)
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
        }

        private void UninstallMouseMonitorHooks()
        {
            _autoCompletionMouseMonitor.MouseClick -= OnAutoCompletionMouseMonitorMouseClick;
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
            Npp.Instance.GrabFocus();
        }

        private void OnAutoCompletionBorderBackgroundUpdated(object sender, DataTransferEventArgs e)
        {
            var border  = sender as Border;
            var context = border.DataContext as AutoCompletionViewModel.Completion;
            ToolTip tp = border.ToolTip as ToolTip;
            if (context.IsSelected)
            {
                tp.PlacementTarget = border;
                tp.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
                HidePreviouslyOpenedTooltip(tp);
                _delayedToolTipHandler.TriggerHandler(new ActionWrapper<System.Windows.Controls.ToolTip>(OnToolTipDelayedHandlerExpired, tp));
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
                if (!((e.NewSize.Height + Top ) <= Npp.Instance.GetClientRectFromControl(Npp.Instance.NppHandle).Bottom))
                {
                    //bottom exceeded - put list on top of word
                    Top = Npp.Instance.GetCaretScreenLocationForFormAboveWord().Y;
                    //problem here - we need to take into account the initial length of the list, otherwise our initial point is wrong if the list is not full
                    Top -= (int)(e.NewSize.Height);
                    IsOnTop = true;
                }
            }
            //position list in such a way that it doesn't get split into two monitors
            var rectFromPoint = Npp.Instance.GetClientRectFromPoint(new System.Drawing.Point((int)Left, (int)Top));
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
                Npp.Instance.ReplaceWordFromToken(TriggerPoint, Completion.InsertionText);
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
