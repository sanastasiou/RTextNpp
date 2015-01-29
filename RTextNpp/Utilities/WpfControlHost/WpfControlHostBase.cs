﻿using System;
using System.Timers;

namespace RTextNppPlugin.Utilities.WpfControlHost
{
    /**
     * A npp control host.
     *
     * The solid purpose of this class is to periodically refresh the hosted control, because Notepad++ doesn't do this.
     * This results in falsely drawn controls when the user resizes the Notepad++ window.
     * \tparam  T   Generic type parameter which has to be a wpf control host.
     *                   
     */
    class WpfControlHostBase<T> : IDisposable where T : System.Windows.Forms.Form, new()
    {

        #region Interface

        /**
         * Gets the underlying element host.
         *
         * \return  The element host.
         */
        public System.Windows.Forms.Form ElementHost { get { return _elementHost; } }
       
        /**
         * Constructor.
         *
         * \param   settingKey  The key for the persistence setting.
         */
        public WpfControlHostBase()
        {
            _elementHost = new T();
            _elementHost.VisibleChanged += OnVisibilityChanged;
            _elementHost.Move += OnElementHostMove;
            _elementHost.PaddingChanged += OnElementHostMove;
            _elementHost.Resize += OnElementHostMove;
            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
            _refreshTimer.Enabled = true;
            _refreshTimer.AutoReset = true;
        }

        public T WpfHost
        {
            get
            {
                return (T)_elementHost;
            }
        }

        public void SetNppHandle(IntPtr handle)
        {
            NPP_HANDLE = handle;
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
         * Gets a value indicating whether the element host form is visible.
         *
         * \return  true if visible, false if not.
         */
        public bool Visible
        {
            get
            {
                if (this._elementHost.InvokeRequired)
                {

                    return (bool)this._elementHost.Invoke(new Func<bool>(IsVisible));
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
        public bool Focus()
        {
            if (_elementHost.InvokeRequired)
            {
                return (bool)_elementHost.Invoke(new Func<bool>(_elementHost.Focus));
            }
            return _elementHost.Focus();
        }

        public int CmdId
        {
            set
            {
                if (this._elementHost.InvokeRequired)
                {
                    this._elementHost.Invoke(new Action<int>(SetCmdId), value);
                }
                else
                {
                    _cmdId = value;
                }
            }
            private get
            {
                return _cmdId;
            }
        }

        public IntPtr Handle
        {
            get
            {
                if (this._elementHost.InvokeRequired)
                {
                    return (IntPtr)this._elementHost.Invoke(new Func<IntPtr>(GetHandle));
                }
                return _elementHost.Handle;
            }
        }

        public bool Created
        {
            get
            {
                if (this._elementHost.InvokeRequired)
                {
                    return (bool)this._elementHost.Invoke(new Func<bool>(IsCreated));
                }
                else
                {
                    return IsCreated();
                }
            }
        }
        #endregion

        #region [Event Handlers]

        private void OnRefreshTimerElapsed(object sender, ElapsedEventArgs e)
        {
            //update check box - special case where update box has false value after plugin initialization...
            Win32.SendMessage(NPP_HANDLE, NppMsg.NPPM_SETMENUITEMCHECK, CmdId, _elementHost.Visible ? 1 : 0);
            if (_refreshNeeded)
            {
                if (_elementHost.InvokeRequired)
                {
                    _elementHost.BeginInvoke((Action)(() => { _elementHost.Refresh(); }));
                }
                else
                {
                    _elementHost.Refresh();
                }
                _refreshNeeded = false;
            }
        }

        void OnElementHostMove(object sender, EventArgs e)
        {
            _refreshNeeded = true;
        }

        /**
         * Raises the visibility changed event.
         *
         * \param   sender  Source of the event.
         * \param   e       Event information to send to registered event handlers.
         */
        protected virtual void OnVisibilityChanged(object sender, EventArgs e)
        {
            Win32.SendMessage(NPP_HANDLE, NppMsg.NPPM_SETMENUITEMCHECK, CmdId, _elementHost.Visible ? 1 : 0);
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

        #region Helpers

        /**
         * Finaliser.
         */
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
                _refreshTimer.Enabled = false;
                _refreshTimer.AutoReset = false;
                _elementHost.VisibleChanged -= OnVisibilityChanged;
                _elementHost.Move -= OnElementHostMove;
                _elementHost.PaddingChanged -= OnElementHostMove;
                _elementHost.Resize -= OnElementHostMove;
                _refreshTimer.Elapsed -= OnRefreshTimerElapsed;
            }
            disposed = true;
        }

        private void SetHandle(IntPtr handle)
        {
            NPP_HANDLE = handle;
        }

        private IntPtr GetHandle()
        {
            return _elementHost.Handle;
        }

        bool IsCreated()
        {
            if (!_isCreated)
            {
                _isCreated = true;
                return false;
            }
            return true;
        }

        bool IsVisible()
        {
            return _elementHost.Visible;
        }

        void SetCmdId(int id)
        {
            _cmdId = id;
        }

        #endregion

        #region [Data Members]

        private IntPtr NPP_HANDLE = IntPtr.Zero;                                  //!< Notepad++ main window handle.
        private System.Windows.Forms.Form _elementHost;                           //!< The element host to be redrawed.
        private Timer _refreshTimer = new Timer(Constants.FORM_INTERVAL_REFRESH); //!< The timer, which if expired, shall refresh the element host window.
        private bool disposed = false;                                            //!< Has the disposed method already been called.
        private bool _isCreated = false;                                          //!< Indicates if windows was created.
        private int _cmdId = 0;                                                   //!< Indicates the cmd id, needed to set check box on menu items.
        private bool _refreshNeeded = false;                                      //!< Indicates that a control refresh is needed, e.g. after a move.
        #endregion
    }
}