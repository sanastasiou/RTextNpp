using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CSScriptIntellisense;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.RText;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.ViewModels;
using System.Linq;
namespace RTextNppPlugin.WpfControls
{
    public partial class AutoCompletionWindow : System.Windows.Window, IDisposable, IWin32MessageReceptor, IWindowPosition
    {
        #region [DataMembers]
        VoidDelayedEventHandler _delayedFilterEventHandler     = null;                 //!< Control options filtering with a delay.
        KeyInterceptor _keyMonitor                             = new KeyInterceptor(); //!< Monitors key presses when auto completion is active.
        GlobalClickInterceptor _autoCompletionMouseMonitor     = null;                 //!< Monitors click events to hide auto completion window.
        bool _isOnTop                                          = false;                //!< Indicates if window is on top of a token.
        INpp _nppHelper                                        = null;                 //!< Allows access to Notepad++ functions.
        DatagridScrollviewerTooltipOffsetCalculator _tpControl = null;                 //!< Control tooltip placement on right of the completion options.
        #endregion
        
        #region [Interface]
        internal AutoCompletionWindow(ConnectorManager cmanager, IWin32 win32Helper, INpp nppHelper)
        {
            InitializeComponent();
            _autoCompletionMouseMonitor = new GlobalClickInterceptor(win32Helper);
            DataContext                 = new ViewModels.AutoCompletionViewModel(cmanager);
            _keyMonitor.KeyDown         += OnKeyMonitorKeyDown;
            _delayedFilterEventHandler  = new VoidDelayedEventHandler(new Action(PostProcessKeyPressed), 150);
            _tpControl                  = new DatagridScrollviewerTooltipOffsetCalculator(Dispatcher,
                                                                                          this,
                                                                                          Constants.MAX_AUTO_COMPLETION_TOOLTIP_WIDTH,
                                                                                          AutoCompletionDatagrid);
            _nppHelper                  = nppHelper;
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Down);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Up);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageUp);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageDown);
        }
        
        public bool IsEdgeOfScreenReached(double offset)
        {
            return ((Left + Width + offset) > _nppHelper.GetClientRectFromPoint(new System.Drawing.Point((int)Left, (int)Top)).Right);
        }
               
        internal new void Hide()
        {
            base.Hide();
            GetModel().OnAutoCompletionWindowCollapsing();
        }
        
        internal async Task AugmentAutoCompletion(ContextExtractor extractor, System.Drawing.Point caretPoint, AutoCompletionTokenizer tokenizer, bool isAutoCompletionShortcutActive)
        {
            await GetModel().AugmentAutoCompletion(extractor, caretPoint, tokenizer, isAutoCompletionShortcutActive);
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
        
        internal void OnZoomLevelChanged(double newZoomLevel)
        {
            if (IsVisible)
            {
                //in case the form is visible - move it to the new place...
                var aCaretPoint = _nppHelper.GetCaretScreenLocationForForm();
                if (GetModel().TriggerPoint.HasValue)
                {
                    aCaretPoint = _nppHelper.GetCaretScreenLocationRelativeToPosition(GetModel().TriggerPoint.Value.BufferPosition);
                }
                Left = aCaretPoint.X;
                Top  = aCaretPoint.Y;
            }
            Dispatcher.BeginInvoke(new Action<double>(GetModel().OnZoomLevelChanged), newZoomLevel);
        }
        
        internal AutoCompletionViewModel.Completion Completion { get { return GetModel().SelectedCompletion; } }
        
        #endregion
        
        #region [Helpers]
        
        private AutoCompletionViewModel GetModel()
        {
            return ((AutoCompletionViewModel)DataContext);
        }
        
        private void InstallMouseMonitorHooks()
        {
            _autoCompletionMouseMonitor.MouseClick += OnAutoCompletionMouseMonitorMouseClick;
        }
        
        private void UninstallMouseMonitorHooks()
        {
            _autoCompletionMouseMonitor.MouseClick -= OnAutoCompletionMouseMonitorMouseClick;
        }
        #endregion
        
        #region EventHandlers

        void OnAutoCompletionMouseMonitorMouseWheelMoved(object sender, MouseEventExtArgs e)
        {
            var collectionView = CollectionViewSource.GetDefaultView(GetModel().CompletionList);
            var aNewPosition = VisualUtilities.ScrollList(e.Delta > 0 ? System.Windows.Forms.Keys.Up : System.Windows.Forms.Keys.Down, collectionView, AutoCompletionDatagrid, 3);
            GetModel().SelectPosition(aNewPosition);
            AutoCompletionDatagrid.ScrollIntoView(collectionView.CurrentItem);
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
                    _nppHelper.GrabFocus();
                }
            }
        }

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
                if (CharProcessAction == AutoCompletionViewModel.CharProcessResult.MoveToRight)
                {
                    Left              = _nppHelper.GetCaretScreenLocationRelativeToPosition(_nppHelper.GetCaretPosition()).X;
                    CharProcessAction = AutoCompletionViewModel.CharProcessResult.NoAction;
                }
                //only filter if auto completion form can still remain open
                if (CharProcessAction != AutoCompletionViewModel.CharProcessResult.ForceClose)
                {
                    //do heavy lifting in here -> debounce many subsequent calls
                    _delayedFilterEventHandler.TriggerHandler();
                }
            }
        }

        private void OnKeyMonitorKeyDown(System.Windows.Forms.Keys key, int repeatCount, ref bool handled)
        {
            var collectionView = CollectionViewSource.GetDefaultView(GetModel().CompletionList);
            int aNewPosition = 0;
            switch (key)
            {
                case System.Windows.Forms.Keys.Up:
                case System.Windows.Forms.Keys.Down:
                    handled = true;
                    aNewPosition = VisualUtilities.ScrollList(key, collectionView, AutoCompletionDatagrid);
                    break;
                case System.Windows.Forms.Keys.PageDown:
                case System.Windows.Forms.Keys.PageUp:
                    aNewPosition = VisualUtilities.ScrollList(key, collectionView, AutoCompletionDatagrid, 25);
                    handled = true;
                    break;
                default:
                    return;
            }
            GetModel().SelectPosition(aNewPosition);
            AutoCompletionDatagrid.ScrollIntoView(collectionView.CurrentItem);
        }
        
        private void OnAutoCompletionDatagridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetModel().SelectPosition(((DataGrid)sender).SelectedIndex);
            if (GetModel().SelectedCompletion != null)
            {
                AutoCompletionDatagrid.ScrollIntoView(GetModel().SelectedCompletion);
            }
            //keep caret blinking after a selection has been made by clicking
            _nppHelper.GrabFocus();
        }
        
        private void OnAutoCompletionBorderBackgroundUpdated(object sender, DataTransferEventArgs e)
        {
            var border  = sender as Border;
            var context = border.DataContext as AutoCompletionViewModel.Completion;
            ToolTip tp = border.ToolTip as ToolTip;
            if (context.IsSelected)
            {
                _tpControl.ShowTooltip(tp, border);
            }
        }
        
        private void OnAutoCompletionBorderMouseEnter(object sender, MouseEventArgs e)
        {
            Border border = (Border)sender;
            _tpControl.CancelTooltipRequest(border.ToolTip as ToolTip);
        }
        
        private void OnAutoCompletionBorderToolTipOpening(object sender, ToolTipEventArgs e)
        {
            var border          = sender as Border;
            var Tp              = border.ToolTip as ToolTip;
            Tp.HorizontalOffset = _tpControl.CalculateTooltipOffset();
        }
        
        /**
         * \brief   Resizes open auto completion list when the container's size change.
         */
        private void OnContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            VisualUtilities.RepositionWindow(e, this, ref _isOnTop, _nppHelper, _nppHelper.GetCaretScreenLocation().Y);
        }
        
        private void OnAutoCompletionFormVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                _keyMonitor.Uninstall();
                AutoCompletionDatagrid.SelectedIndex = -1;
                GetModel().OnAutoCompletionWindowCollapsing();
                UninstallMouseMonitorHooks();
                _isOnTop = false;
                _tpControl.CancelTooltipRequest(null);
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
                _nppHelper.ReplaceWordFromToken(TriggerPoint, Completion.InsertionText);
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
                    int x             = unchecked((short)(long)lParam);
                    int y             = unchecked((short)((long)lParam >> 16));
                    OnAutoCompletionMouseMonitorMouseWheelMoved(null, new MouseEventExtArgs(System.Windows.Forms.MouseButtons.None, 0, x, y, wheelMovement));
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}