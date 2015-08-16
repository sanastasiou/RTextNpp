using System;
using System.Linq;

namespace RTextNppPlugin.ViewModels
{
    using RTextNppPlugin.RText;
    using RTextNppPlugin.RText.Protocol;
    using RTextNppPlugin.RText.StateEngine;
    using RTextNppPlugin.Utilities;
    using System.Collections.Generic;

    class WorkspaceViewModel : WorkspaceViewModelBase, IConsoleViewModelBase, IDisposable
    {
        #region [Interface]
        public WorkspaceViewModel(string workspace, ref Connector connector, ConsoleViewModel mainViewModel, INpp nppHelper)
            : base(workspace)
        {
            _connector                   = connector;
            _mainModel                   = mainViewModel;
            _connector.OnStateChanged    += OnConnectorStateChanged;
            _connector.OnProgressUpdated += OnConnectorProgressUpdated;
            _nppHelper                   = nppHelper;
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

        new public int ErrorCount
        {
            get
            {
                return (_connector != null) ? _connector.ErrorList != null ? _connector.ErrorList.total_problems : 0 : 0;
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
            _percentage = e.Response.percentage;
            _isLoading  = true;
            if (e.Workspace == _mainModel.Workspace)
            {
                _mainModel.ProgressPercentage = ProgressPercentage;
                _mainModel.IsLoading          = IsLoading;
            }
        }

        private void OnConnectorStateChanged(object source, Connector.StateChangedEventArgs e)
        {
            switch (e.StateEntered)
            {
                case ConnectorStates.Loading:
                case ConnectorStates.Busy:
                    _isActive      = true;
                    _isBusy        = true;
                    _activeCommand = e.Command;

                    if (e.StateEntered == ConnectorStates.Loading)
                    {
                        _isLoading = true;
                        _percentage = 0.0;
                    }

                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive           = _isActive;
                        _mainModel.IsBusy             = _isBusy;
                        _mainModel.IsLoading          = _isLoading;
                        _mainModel.ActiveCommand      = _activeCommand;
                        _mainModel.ProgressPercentage = _percentage;
                    }                    
                    break;
                case ConnectorStates.Idle:
                    _isActive  = true;
                    _isLoading = false;
                    _isBusy    = false;
                    _activeCommand = String.Empty;
                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive      = _isActive;
                        _mainModel.IsBusy        = _mainModel.IsLoading = false;
                        _mainModel.ActiveCommand = _activeCommand;
                    }
                    break;                
                default:
                    _isActive = _isBusy = false;
                    _activeCommand = Constants.Commands.STOP;
                    if (e.Workspace == _mainModel.Workspace)
                    {
                        _mainModel.IsActive      = _mainModel.IsLoading = _mainModel.IsBusy = false;
                        _mainModel.ActiveCommand = _activeCommand;
                    }
                    break;
            }
            if (e.StateEntered == ConnectorStates.Idle && _previousConnectorState == ConnectorStates.Loading)
            {
                if (e.Workspace == _mainModel.Workspace)
                {
                    _mainModel.ErrorCount = _connector.ErrorList.total_problems;
                    AddErrorsToMainModel();
                }
            }
            else if (e.StateEntered == ConnectorStates.Loading && _previousConnectorState == ConnectorStates.Idle)
            {
                ClearErrors();
            }
            _previousConnectorState = e.StateEntered;
          
        }

        public void Dispose()
        {
            _connector.OnStateChanged    -= OnConnectorStateChanged;
            _connector.OnProgressUpdated -= OnConnectorProgressUpdated;
        }
        #endregion

        #region [Helpers]
        void ClearErrors()
        {
            if(_connector.Workspace == _mainModel.Workspace)
            {
                _mainModel.Errors.Clear();
            }
        }

        void AddErrorsToMainModel()
        {
            _mainModel.Errors.Clear();
            if (_connector.ErrorList.total_problems > 0)
            {
                List<ErrorListViewModel> errorLists = new List<ErrorListViewModel>(_connector.ErrorList.problems.Count);
                foreach (var errors in _connector.ErrorList.problems.OrderBy( x => x.file ))
                {
                    errorLists.Add(new ErrorListViewModel(errors.file, errors.problems.OrderBy( x => x.line).Select(x => new ErrorItemViewModel(x, errors.file)), false));
                }
                _mainModel.Errors.AddRange(errorLists);
            }
        }
        #endregion

        #region [Data Members]

        private bool _isBusy                            = false;                //!< Indicates if backend is currently busy.
        private bool _isActive                          = false;                //!< Indicates whether backend process is currently active.
        private double _percentage                      = 0.0;                  //!< The current command percentage.
        private bool _isLoading                         = false;                //!< Model loading status.
        private Connector _connector                    = null;                 //!< Associated connector instance.
        private ConsoleViewModel _mainModel             = null;                 //!< Main model reference.
        private string _activeCommand                   = String.Empty;         //!< Holds the current active command.
        private ConnectorStates _previousConnectorState = ConnectorStates.Idle; //!< Stores previous connector state.
        private INpp _nppHelper                         = null;                 //!< Npp helper instance.

        #endregion
    }
}
