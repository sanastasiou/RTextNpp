using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using CSScriptIntellisense;
using RTextNppPlugin.Parsing;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.ViewModels;		

namespace RTextNppPlugin.WpfControls
{
    public partial class AutoCompletionWindow : Window, IDisposable
    {
        #region [DataMembers]

        CSScriptIntellisense.MouseMonitor _mouseMonitor             = new CSScriptIntellisense.MouseMonitor();
        DelayedKeyEventHandler _delayedFilterEventHandler           = null;

        #endregion

        #region [Interface]
        public AutoCompletionWindow()
        {
            InitializeComponent();
            _mouseMonitor.MouseClicked += OnMouseMonitorMouseClicked;
            _mouseMonitor.MouseWheelMoved += OnMouseMonitorMouseWheelMoved;
            _delayedFilterEventHandler = new DelayedKeyEventHandler(this.PostProcessKeyPressed, 200);

        }

        public void AugmentAutoCompletion(ContextExtractor extractor, System.Drawing.Point caretPoint, Tokenizer.TokenTag? token, ref bool request)
        {
            GetModel().AugmentAutoCompletion(extractor, caretPoint, token, ref request);
            CharProcessAction = GetModel().CharProcessAction;
        }

        public void PostProcessKeyPressed()
        {
            //handle this on UI thread since it will alter UI
            Dispatcher.BeginInvoke(new Action(GetModel().Filter));
        }

        internal AutoCompletionViewModel.CharProcessResult CharProcessAction { get; private set; }

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

        public void OnKeyPressed(char c = '\0')
        {
            CharProcessAction = AutoCompletionViewModel.CharProcessResult.NoAction;
            if (IsVisible)
            {                
                _delayedFilterEventHandler.Cancel();
                //reparse line and find new trigger token
                GetModel().OnKeyPressed(c);

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
            //do now allow event to propage, therefore not allowing text to scroll...
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
                this.AutoCompletionDatagrid.SelectedIndex = -1;
            }
            else
            {
                _mouseMonitor.Install();
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
            _mouseMonitor.Uninstall();
        }
        #endregion

    }
}
