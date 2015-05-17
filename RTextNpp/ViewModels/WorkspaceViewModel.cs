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
            _connector.OnProgressUpdated += OnConnectorProgressUpdated;
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
        private void OnConnectorProgressUpdated(object source, Connector.ProgressResponseEventArgs e)
        {
            if (e.Workspace == _mainModel.Workspace)
            {
                _mainModel.ProgressPercentage = e.Response.percentage;
            }
        }

        private void OnConnectorStateChanged(object source, Connector.StateChangedEventArgs e)
        {
            switch (e.State)
            {
                case Automate.StateEngine.ProcessState.Loading:
                case Automate.StateEngine.ProcessState.Busy:
                    _isActive = true;
                    _isBusy = true;
                    _activeCommand = e.Command;
                    if (e.State == Automate.StateEngine.ProcessState.Loading)
                    {
                        _isLoading                    = true;
                        _mainModel.ProgressPercentage = 0.0;
                    }
                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive      = _isActive;
                        _mainModel.IsBusy        = _isBusy;
                        _mainModel.IsLoading     = _isLoading;
                        _mainModel.ActiveCommand = _activeCommand;
                    }
                    break;
                case Automate.StateEngine.ProcessState.Connected:
                case Automate.StateEngine.ProcessState.Idle:
                    _isActive = true;
                    _isLoading = false;
                    _isBusy = false;
                    _activeCommand = String.Empty;
                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive      = _isActive;
                        _mainModel.IsBusy        = _mainModel.IsLoading = false;
                        _mainModel.ActiveCommand = _activeCommand;
                    }
                    break;
                case Automate.StateEngine.ProcessState.Closed:
                default:
                    _isActive = _isLoading = _isBusy = false;
                    _activeCommand = Constants.Commands.STOP;
                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive      = _mainModel.IsLoading = _mainModel.IsBusy = false;
                        _mainModel.ActiveCommand = _activeCommand;
                    }
                    break;
            }
          
        }

        public void Dispose()
        {
            _connector.OnStateChanged    -= OnConnectorStateChanged;
            _connector.OnProgressUpdated -= OnConnectorProgressUpdated;
        }
        #endregion

        #region [Data Members]

        private bool _isBusy                = false;  //!< Indicates if backend is currently busy.
        private bool _isActive              = false;  //!< Indicates whether backend process is currently active.
        private double _percentage          = 0.0;    //!< The current command percentage.
        private bool _isLoading             = false;  //!< Model loading status.
        private Connector _connector        = null;   //!< Associated connector instance.
        private ConsoleViewModel _mainModel = null;   //!< Main model reference.
        private string _activeCommand = String.Empty; //!< Holds the current active command.

        #endregion
    }
}
