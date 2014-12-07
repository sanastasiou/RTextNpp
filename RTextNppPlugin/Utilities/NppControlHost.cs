using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Configuration;
using System.Reflection;

namespace RTextNppPlugin.Utilities
{
    /**
     * A npp control host.
     *
     * The solid purpose of this class is to periodically refresh the hosted control, because Notepad++ doesn't do this.
     * This results in falsely drawn controls when the user resizes the Notepad++ window.
     * \tparam  T   Generic type parameter which has to be a wpf control host.
     *                   */
    class NppControlHost<T> : IDisposable where T : System.Windows.Forms.Form, new()
    {

        #region Interface

        /**
         * Constructor.
         *
         * \param   settingKey  The key for the persistence setting.
         */
        public NppControlHost(string settingKey, IntPtr nppHandle)
        {
            NPP_HANDLE = nppHandle;
            _elementHost = new T();
            _elementHost.VisibleChanged += OnVisibilityChanged;
            _refreshTimer.Elapsed += onRefreshTimerElapsed;
            _refreshTimer.Enabled = true;
            _refreshTimer.AutoReset = true;
            SETTING_KEY = settingKey;
        }

        /**
         * Public implementation of Dispose pattern callable by consumers.
         */
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /**
         * Finaliser.
         */
        ~NppControlHost()
        {
            Dispose(false);
        }

        /**
         * Gets the handle.
         *
         * \return  The handle from element host.
         */
        public IntPtr Handle
        {
            get
            {
                return _elementHost.Handle;
            }
        }

        /**
         * Gets a value indicating whether the element host form is visible.
         *
         * \return  true if visible, false if not.
         */
        public bool Visible
        {
            get
            {
                return _elementHost.Visible;
            }
        }

        /**
         * Focus on the element host.
         *
         * \return  true if it succeeds, false if it fails.
         */
        public bool Focus()
        {
            return _elementHost.Focus();
        }

        public int CmdId { set; private get; }
        #endregion

        #region Helpers

        // Protected implementation of Dispose pattern. 
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                _refreshTimer.Enabled = false;
                _refreshTimer.AutoReset = false;
                _refreshTimer.Elapsed -= onRefreshTimerElapsed;
            }
            disposed = true;
        }

        private void onRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            //when the timer elapses the host shall be redrawed
            _elementHost.Refresh();
        }

        /**
         * Raises the visibility changed event.
         *
         * \param   sender  Source of the event.
         * \param   e       Event information to send to registered event handlers.
         */
        private void OnVisibilityChanged(object sender, EventArgs e)
        {
            Win32.SendMessage(NPP_HANDLE, NppMsg.NPPM_SETMENUITEMCHECK, CmdId, _elementHost.Visible ? 1 : 0);
            Utilities.ConfigurationSetter.saveSetting(_elementHost.Visible, SETTING_KEY);
        }

        #endregion

        private readonly IntPtr NPP_HANDLE = IntPtr.Zero;                         //!< Notepad++ main window handle.
        private readonly string SETTING_KEY = null;                               //!< The persistence setting for this form.
        private System.Windows.Forms.Form _elementHost;                           //!< The element host to be redrawed.
        private Timer _refreshTimer = new Timer(Constants.FORM_INTERVAL_REFRESH * 10); //!< The timer, which if expired, shall refresh the element host window.
        private bool disposed = false;                                            //!< Has the disposed method already been called.
    }
}
