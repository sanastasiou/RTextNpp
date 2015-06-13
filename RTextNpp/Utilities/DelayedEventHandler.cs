using System;
using System.Windows.Threading;

namespace RTextNppPlugin.Utilities
{
    internal class DelayedEventHandler
    {
        #region [Data Members]

        private DispatcherTimer _timer = null;
        private IActionWrapper _action = null;

        #endregion

        #region [Interface]



        internal DelayedEventHandler(IActionWrapper action, double milliseconds, DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
        {
            _action          = action;
            _timer           = new DispatcherTimer(priority);
            _timer.Interval  = TimeSpan.FromMilliseconds(milliseconds);
            _timer.Tick      += OnIntervalTick;
            Cancel();
        }

        internal void TriggerHandler()
        {
            _timer.Start();
            _timer.IsEnabled = true;
        }

        internal void TriggerHandler(IActionWrapper action)
        {
            _action = action;
            _timer.Start();
            _timer.IsEnabled = true;
        }

        internal void Cancel()
        {
            _timer.Stop();
            _timer.IsEnabled = false;
        }

        internal bool IsRunning
        {
            get
            {
                return _timer.IsEnabled;
            }
        }

        #endregion

        #region [Event Handlers]

        void OnIntervalTick(object sender, EventArgs e)
        {
            Cancel();
            if (_action != null)
            {
                _action.DoAction();
            }
        }              
        #endregion
    }    
}
