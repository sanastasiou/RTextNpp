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
                    NavigateList(key);
                    break;
                case System.Windows.Forms.Keys.PageDown:
                case System.Windows.Forms.Keys.PageUp:
                    ScrollList(key);
                    handled = true;
                    break;
                default:
                    return;
            }
        }

        public void AugmentAutoCompletion(ContextExtractor extractor, System.Drawing.Point caretPoint, Tokenizer.TokenTag? token, ref bool request)
        {
            GetModel().AugmentAutoCompletion(extractor, caretPoint, token, ref request);
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
        public bool IsMouseInsideWindow()
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
            Trace.WriteLine(String.Format("Mouse wheel moved... {0}", movement));
            //do now allow event to propagate, therefore not allowing text to scroll...
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
         * \brief   Navigate list when pressing up/down arrows.
         *
         * \param   key The key.
         */
        private void NavigateList(System.Windows.Forms.Keys key)
        {
            var aTargets = GetModel().CompletionList;
            if (aTargets.Count > 0)
            {
                var aIndex = this.AutoCompletionDatagrid.SelectedIndex;                
                switch (key)
                {
                    case System.Windows.Forms.Keys.Up:
                        if (aIndex > 0)
                        {
                            GetModel().SelectedIndex  = this.AutoCompletionDatagrid.SelectedIndex - 1;
                        }
                        break;
                    case System.Windows.Forms.Keys.Down:
                        if (aIndex < (aTargets.Count - 1))
                        {
                            GetModel().SelectedIndex = this.AutoCompletionDatagrid.SelectedIndex + 1;
                        }
                        break;
                    default:
                        return;
                }
                this.AutoCompletionDatagrid.ScrollIntoView(this.AutoCompletionDatagrid.SelectedItem);
            }
        }

        private void ScrollList(System.Windows.Forms.Keys key)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(GetModel().CompletionList);
            int offset = 25;
            switch (key)
            {
                case System.Windows.Forms.Keys.PageDown:
                    if (view.CurrentPosition + offset < AutoCompletionDatagrid.Items.Count)
                    {
                        view.MoveCurrentToPosition(view.CurrentPosition + offset);
                    }
                    else
                    {
                        view.MoveCurrentToLast();
                    }
                    break;
                case System.Windows.Forms.Keys.PageUp:
                    if (view.CurrentPosition - offset >= 0)
                    {
                        view.MoveCurrentToPosition(view.CurrentPosition - offset);
                    }
                    else
                    {
                        view.MoveCurrentToFirst();
                    }
                    break;
            }
            this.AutoCompletionDatagrid.ScrollIntoView(view.CurrentItem);
        }

        private void SelectRowByIndex(DataGrid dataGrid, int rowIndex)
        {
            if (!dataGrid.SelectionUnit.Equals(DataGridSelectionUnit.FullRow))
                throw new ArgumentException("The SelectionUnit of the DataGrid must be set to FullRow.");

            if (rowIndex < 0 || rowIndex > (dataGrid.Items.Count - 1))
                throw new ArgumentException(string.Format("{0} is an invalid row index.", rowIndex));

            dataGrid.SelectedItems.Clear();
            /* set the SelectedItem property */
            object item = dataGrid.Items[rowIndex]; // = Product X
            dataGrid.SelectedItem = item;

            DataGridRow row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            if (row == null)
            {
                /* bring the data item (Product object) into view
                 * in case it has been virtualized away */
                dataGrid.ScrollIntoView(item);
                row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
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
            _mouseMonitor.Uninstall();
        }
        #endregion

    }
}
