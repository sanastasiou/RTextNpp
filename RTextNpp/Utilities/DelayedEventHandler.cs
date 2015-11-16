using System;
using System.Windows.Threading;
namespace RTextNppPlugin.Utilities
{
    internal class DelayedEventHandler<R>
    {
        #region [Data Members]
        private DispatcherTimer _timer    = null;
        private IActionWrapper<R> _action = null;
        private R _result                 = default(R);
        #endregion
        #region [Interface]
        internal DelayedEventHandler(IActionWrapper<R> action, double milliseconds, DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
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
        internal void TriggerHandler(IActionWrapper<R> action)
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

        internal R Result
        {
            get
            {
                return _result;
            }
        }
        #endregion
        #region [Event Handlers]
        void OnIntervalTick(object sender, EventArgs e)
        {
            Cancel();
            if (_action != null)
            {
                _result = _action.DoAction();
            }
        }
        #endregion
    }

    internal class VoidDelayedEventHandler
    {
        #region [Data Members]
        private DispatcherTimer _timer = null;
        private Action _action = null;
        #endregion
        #region [Interface]
        internal VoidDelayedEventHandler(Action action, double milliseconds, DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
        {
            _action = action;
            _timer = new DispatcherTimer(priority);
            _timer.Interval = TimeSpan.FromMilliseconds(milliseconds);
            _timer.Tick += OnIntervalTick;
            Cancel();
        }
        internal void TriggerHandler()
        {
            _timer.Start();
            _timer.IsEnabled = true;
        }
        internal void TriggerHandler(Action action)
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
                _action();
            }
        }
        #endregion
    }
}