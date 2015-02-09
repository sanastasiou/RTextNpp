using System;

using RTextNppPlugin.Automate;

namespace RTextNppPlugin.ViewModels
{
    class WorkspaceViewModel : WorkspaceViewModelBase, IConsoleViewModelBase, IDisposable
    {
        #region [Interface]
        public WorkspaceViewModel(string workspace, ref Connector connector, ConsoleViewModel mainViewModel) : base(workspace)
        {
            _connector = connector;
            _mainModel = mainViewModel;
            _connector.OnStateChanged += OnConnectorStateChanged;
        }

        /**
         * \brief   Gets a value indicating whether this workspace is currently loading.
         *
         * \return  true if this workspace is loading, false if not.
         */
        new public bool IsBusy
        {
            get
            {
                return (_isLoading || _isBusy);
            }
        }

        new public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
        }

        new public double ProgressPercentage 
        { 
            get
            {
                return _percentage;
            }
        }

        public void AddConnector(Connector connector)
        {
            _connector = connector;
        }

        new public bool IsActive
        {
            get { return _isActive; }
        }

        new public bool IsAutomateWorkspace
        {
            get { return true; }
        }


        #endregion

        #region [Event Handlers]
        void OnConnectorStateChanged(object source, Connector.StateChangedEventArgs e)
        {
            switch (e.State)
            {
                case RTextNppPlugin.Automate.StateEngine.ProcessState.Connecting:
                case RTextNppPlugin.Automate.StateEngine.ProcessState.Busy:
                    _isActive = true;
                    _isBusy   = true;
                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive = true;
                        _mainModel.IsBusy   = true;
                        if (_connector.IsLoading)
                        {
                            _mainModel.IsLoading = true;
                        }
                    }
                    break;
                case RTextNppPlugin.Automate.StateEngine.ProcessState.Connected:
                    _isActive  = true;
                    _isLoading = false;
                    _isBusy    = false;
                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive = true;
                        _mainModel.IsBusy = _mainModel.IsLoading = false;
                    }
                    break;
                case RTextNppPlugin.Automate.StateEngine.ProcessState.Closed:
                default:
                    _isActive = _isLoading = _isBusy = false;
                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive = _mainModel.IsLoading = _mainModel.IsBusy = false;
                    }
                    break;
            }
        }

        public void Dispose()
        {
            _connector.OnStateChanged -= OnConnectorStateChanged;
        }
        #endregion

        #region [Data Members]

        private bool _isBusy                = false; //!< Indicates if backend is currently busy.
        private bool _isActive              = false; //!< Indicates whether backend process is currently active.
        private double _percentage          = 0.0;   //!< The current command percentage.
        private bool _isLoading             = false; //!< Model loading status.
        private Connector _connector        = null;  //!< Associated connector instance.
        private ConsoleViewModel _mainModel = null;  //!< Main model reference.

        #endregion
    }
}
