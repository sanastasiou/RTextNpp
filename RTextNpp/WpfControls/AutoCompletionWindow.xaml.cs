using RTextNppPlugin.Parsing;
using System;
using System.Windows;
using System.Windows.Input;
using RTextNppPlugin.ViewModels;

namespace RTextNppPlugin.WpfControls
{
    public partial class AutoCompletionWindow : Window, IDisposable
    {
        #region [DataMembers]

        CSScriptIntellisense.MouseMonitor _mouseMonitor = new CSScriptIntellisense.MouseMonitor();

        #endregion

        #region [Interface]
        public AutoCompletionWindow()
        {
            InitializeComponent();
            _mouseMonitor.Install();
            _mouseMonitor.MouseClicked += OnMouseMonitorMouseClicked;
        }

        public void AugmentAutoCompletion(ContextExtractor extractor, System.Drawing.Point caretPoint, Tokenizer.TokenTag? token, ref bool request)
        {
            GetModel().AugmentAutoCompletion(extractor, caretPoint, token, ref request);
        }

        /**
         * @param   level   The zoom level. Updates auto completion form zoom level via databinding.
         */
        public void SetZoomLevel(double level)
        {
            GetModel().ZoomLevel = level;
        }        

        public void OnKeyPressed(System.Windows.Forms.Keys key)
        {
            //reparse line and find new trigger token
            GetModel().OnKeyPressed(key);
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
