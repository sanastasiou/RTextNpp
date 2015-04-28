using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Threading;

namespace RTextNppPlugin.Utilities
{
    class DelayedEventHandler
    {
        #region [Data Members]

        private DispatcherTimer _timer = null;
        private Action _handler        = null;     

        #endregion

        #region [Interface]



        public DelayedEventHandler(Action handler, double milliseconds, DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
        {
            _handler         = handler;
            _timer           = new DispatcherTimer(priority);
            _timer.Interval  = TimeSpan.FromMilliseconds(milliseconds);
            _timer.Tick      += OnIntervalTick;
            Cancel();
        }

        public void TriggerHandler()
        {
            _timer.Start();
            _timer.IsEnabled = true;
        }

        public void Cancel()
        {
            _timer.Stop();
            _timer.IsEnabled = false;
        }

        #endregion

        #region [Event Handlers]

        void OnIntervalTick(object sender, EventArgs e)
        {
            Cancel();
            _handler.Invoke();
        }              
        #endregion
    }

    class DelayedEventHandler<T>
    {
        #region [Data Members]

        private DispatcherTimer _timer = null;
        private Action<T> _handler = null;
        private T _arg = default(T);

        #endregion

        #region [Interface]



        public DelayedEventHandler(Action<T> handler, double milliseconds, DispatcherPriority priority = DispatcherPriority.ApplicationIdle)
        {
            _handler = handler;
            _timer = new DispatcherTimer(priority);
            _timer.Interval = TimeSpan.FromMilliseconds(milliseconds);
            _timer.Tick += OnIntervalTick;
            Cancel();
        }

        public void TriggerHandler(T arg)
        {
            _arg = arg;
            _timer.Start();
            _timer.IsEnabled = true;
        }

        public void Cancel()
        {
            _timer.Stop();
            _timer.IsEnabled = false;
        }

        #endregion

        #region [Event Handlers]

        void OnIntervalTick(object sender, EventArgs e)
        {
            Cancel();
            _handler.Invoke(_arg);
        }
        #endregion
    }
}
