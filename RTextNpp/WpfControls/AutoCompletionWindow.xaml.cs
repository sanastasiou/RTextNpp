using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CSScriptIntellisense;
using RTextNppPlugin.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.ViewModels;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;

namespace RTextNppPlugin.WpfControls
{
    public partial class AutoCompletionWindow : Window, IDisposable
    {
        #region [DataMembers]

        MouseMonitor _mouseMonitor                                  = new MouseMonitor();
        DelayedKeyEventHandler _delayedFilterEventHandler           = null;
        KeyInterceptor _keyMonitor                                  = new KeyInterceptor();

        #endregion

        #region [Interface]
        public AutoCompletionWindow()
        {
            InitializeComponent();
            _mouseMonitor.MouseClicked += OnMouseMonitorMouseClicked;
            _mouseMonitor.MouseWheelMoved += OnMouseMonitorMouseWheelMoved;
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Down);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Up);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageUp);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageDown);
            _keyMonitor.KeyDown += OnKeyMonitorKeyDown;
            _delayedFilterEventHandler = new DelayedKeyEventHandler(this.PostProcessKeyPressed, 100);
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
            FrameworkElement pnlClient = this.Content as FrameworkElement;
            if (pnlClient != null)
            {
                dWidth = pnlClient.ActualWidth;
                dHeight = pnlClient.ActualHeight;
            }
            Point aPoint = Mouse.GetPosition(this);
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
        #endregion

        #region EventHandlers
        bool OnMouseMonitorMouseWheelMoved(int movement)
        {
            //do now allow event to propagate, therefore not allowing text to scroll by it's own
            ScrollList(movement > 0 ? System.Windows.Forms.Keys.Up : System.Windows.Forms.Keys.Down, 3);
            return true;

        }

        void OnKeyIntercepterKeyDown(System.Windows.Forms.Keys key, int repeatCount, ref bool handled)
        {
            handled = false;
        }


        void OnMouseMonitorMouseClicked()
        {
            if (!IsMouseInsideWindow())
            {
                this.Hide();
            }
        }

        private void OnAutoCompletionFormVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsVisible)
            {
                _mouseMonitor.Uninstall();
                _keyMonitor.Uninstall();
                this.AutoCompletionDatagrid.SelectedIndex = -1;
                GetModel().OnAutoCompletionWindowCollapsing();
                GetModel().ClearSelectedCompletion();
            }
            else
            {
                _keyMonitor.Install();
                _mouseMonitor.Install();
                _delayedFilterEventHandler.Cancel();                
            }
        }

        #endregion

        #region [Helpers]

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
            _mouseMonitor.Uninstall();
        }
        #endregion

        private void OnAutoCompletionDatagridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetModel().SelectPosition(((DataGrid)sender).SelectedIndex);
            this.AutoCompletionDatagrid.ScrollIntoView(GetModel().SelectedCompletion);
        }
    }
}
