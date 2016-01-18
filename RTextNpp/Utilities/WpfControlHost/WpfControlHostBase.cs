using RTextNppPlugin.Scintilla;
using System;
using System.Diagnostics;
using System.Timers;
namespace RTextNppPlugin.Utilities.WpfControlHost
{
    /**
     * A npp control host.
     *
     * The solid purpose of this class is to periodically refresh the hosted control, because Notepad++ doesn't do
     * This results in falsely drawn controls when the user resizes the Notepad++ window.
     * \tparam  T   Generic type parameter which has to be a wpf control host.
     *
     */
    internal class WpfControlHostBase<T> : IDisposable where T : System.Windows.Forms.Form
    {
        #region [Interface]
        /**
         * Gets the underlying element host.
         *
         * \return  The element host.
         */
        internal T ElementHost { get { return _elementHost; } }
        /**
         * Constructor.
         *
         * \param   settingKey  The key for the persistence setting.
         */
        internal WpfControlHostBase(T elementHost, INpp nppHelper)
        {
            Trace.WriteLine("WpfControlHostBase()");
            _elementHost                = elementHost;
            _elementHost.VisibleChanged += OnVisibilityChanged;
            _elementHost.Move           += OnElementHostMove;
            _elementHost.PaddingChanged += OnElementHostMove;
            _elementHost.Resize         += OnElementHostMove;
            _refreshTimer.Elapsed       += OnRefreshTimerElapsed;
            _refreshTimer.Enabled       = true;
            _refreshTimer.AutoReset     = true;
            _nppHelper                  = nppHelper;
        }
        /**
         * internal implementation of Dispose pattern callable by consumers.
         */
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        /**
         * Gets a value indicating whether the element host form is visible.
         *
         * \return  true if visible, false if not.
         */
        internal bool Visible
        {
            get
            {
                if (_elementHost.InvokeRequired)
                {
                    return (bool)_elementHost.Invoke(new Func<bool>(IsVisible));
                }
                else
                {
                    return _elementHost.Visible;
                }
            }
        }
        /**
         * Focus on the element host.
         *
         * \return  true if it succeeds, false if it fails.
         */
        internal bool Focus()
        {
            if (_elementHost.InvokeRequired)
            {
                return (bool)_elementHost.Invoke(new Func<bool>(_elementHost.Focus));
            }
            return _elementHost.Focus();
        }
        internal int CmdId
        {
            set
            {
                if (_elementHost.InvokeRequired)
                {
                    _elementHost.Invoke(new Action<int>(SetCmdId), value);
                }
                else
                {
                    _cmdId = value;
                }
            }
            get
            {
                return _cmdId;
            }
        }
        internal IntPtr Handle
        {
            get
            {
                if (_elementHost.InvokeRequired)
                {
                    return (IntPtr)_elementHost.Invoke(new Func<IntPtr>(GetHandle));
                }
                return _elementHost.Handle;
            }
        }
        #endregion
        
        #region [Event Handlers]
        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _refreshTimer.Elapsed -= OnRefreshTimerElapsed;
            //update check box - special case where update box has false value after plugin initialization...
            if (_refreshNeeded)
            {
                _elementHost.BeginInvoke((Action)(() => { _elementHost.Refresh(); }));
                _refreshNeeded = false;
            }
            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
        }
        private void OnElementHostMove(object sender, EventArgs e)
        {
            _refreshNeeded = true;
        }
        /**
         * Raises the visibility changed event.
         *
         * \param   sender  Source of the event.
         * \param   e       Event information to send to registered event handlers.
         */
        internal virtual void OnVisibilityChanged(object sender, EventArgs e)
        {
            _nppHelper.ChangeMenuItemCheck(_cmdId, _elementHost.Visible);
            if (_elementHost.Visible)
            {
                _refreshTimer.Start();
            }
            else
            {
                _refreshTimer.Stop();
            }
        }
        #endregion
        
        #region [Helpers]
        /**
         * Finaliser.
         */
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        ~WpfControlHostBase()
        {
            Dispose(false);
        }
        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }
            if (disposing)
            {
                _refreshTimer.Enabled       = false;
                _refreshTimer.AutoReset     = false;
                _elementHost.VisibleChanged -= OnVisibilityChanged;
                _elementHost.Move           -= OnElementHostMove;
                _elementHost.PaddingChanged -= OnElementHostMove;
                _elementHost.Resize         -= OnElementHostMove;
                _refreshTimer.Elapsed       -= OnRefreshTimerElapsed;
            }
            disposed = true;
        }
        private IntPtr GetHandle()
        {
            return _elementHost.Handle;
        }
        private bool IsVisible()
        {
            return _elementHost.Visible;
        }
        private void SetCmdId(int id)
        {
            _cmdId = id;
        }
        #endregion
        
        #region [Data Members]
        private readonly INpp _nppHelper = null;                                       //!< Npp helper instance, used to communicate with Npp.
        private T _elementHost;                                                        //!< The element host to be redrawed.
        private Timer _refreshTimer      = new Timer(Constants.FORM_INTERVAL_REFRESH); //!< The timer, which if expired, shall refresh the element host window.
        private bool disposed            = false;                                      //!< Has the disposed method already been called.
        private int _cmdId               = 0;                                          //!< Indicates the cmd id, needed to set check box on menu items.
        private bool _refreshNeeded      = false;                                      //!< Indicates that a control refresh is needed, e.g. after a move.
        #endregion
    }
}