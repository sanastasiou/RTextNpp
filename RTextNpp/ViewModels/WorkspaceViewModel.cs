using System;
using System.Linq;
namespace RTextNppPlugin.ViewModels
{
    using RTextNppPlugin.DllExport;
    using RTextNppPlugin.RText;
    using RTextNppPlugin.RText.Protocol;
    using RTextNppPlugin.RText.StateEngine;
    using RTextNppPlugin.Scintilla.Annotations;
    using RTextNppPlugin.Utilities;
    using RTextNppPlugin.Utilities.Settings;
    using System.Collections.Generic;
    using System.Windows.Threading;
    class WorkspaceViewModel : WorkspaceViewModelBase, IConsoleViewModelBase, IDisposable
    {
        #region [Interface]
        public WorkspaceViewModel(string workspace, ref Connector connector, ConsoleViewModel mainViewModel, INpp nppHelper, Dispatcher dispatcher, ISettings settings)
            : base(workspace)
        {
            _connector                   = connector;
            _mainModel                   = mainViewModel;
            _connector.OnStateChanged    += OnConnectorStateChanged;
            _connector.OnProgressUpdated += OnConnectorProgressUpdated;
            _nppHelper                   = nppHelper;
            _dispatcher                  = dispatcher;
            _annotationsManager          = new AnnotationManager(settings, nppHelper, Plugin.nppData);
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
                return (IsLoading || _connector.CurrentState.State == ConnectorStates.Busy);
            }
        }
        new public bool IsLoading
        {
            get
            {
                return (_connector.CurrentState.State == ConnectorStates.Loading);
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
            get
            {
                return _connector.CurrentState.State != ConnectorStates.Disconnected;
            }
        }
        new public bool IsAutomateWorkspace
        {
            get 
            { 
                return true;
            }
        }

        new public string ActiveCommand
        {
            get 
            {
                return _connector.ActiveCommand;
            }
        }
        #endregion
        
        #region [Event Handlers]
        
        private void OnConnectorProgressUpdated(object source, Connector.ProgressResponseEventArgs e)
        {
            _percentage    = e.Response.percentage;
            _isLoading     = true;
            _isBusy        = true;
            _isActive      = true;
            _activeCommand = e.Command;
            if (e.Workspace == _mainModel.Workspace)
            {
                _mainModel.ProgressPercentage = ProgressPercentage;
                _mainModel.IsLoading          = IsLoading;
                _mainModel.IsActive           = _isActive;
                _mainModel.IsBusy             = _isBusy;
                _mainModel.ActiveCommand      = _activeCommand;
            }
        }
        
        private void OnConnectorStateChanged(object source, Connector.StateChangedEventArgs e)
        {
            lock (_lock)
            {
                switch (e.StateEntered)
                {
                    case ConnectorStates.Loading:
                    case ConnectorStates.Busy:
                        _isActive = true;
                        _isBusy = true;
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
                        _isActive      = true;
                        _isLoading     = false;
                        _isBusy        = false;
                        _activeCommand = string.Empty;
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


                    if (e.StateEntered == ConnectorStates.Idle && e.StateLeft == ConnectorStates.Loading)
                    {
                        AddErrors();
                        if (e.Workspace == _mainModel.Workspace)
                        {
                            _dispatcher.Invoke(new Action(() =>
                            {
                                _mainModel.ErrorCount = _connector.ErrorList.total_problems;
                                _mainModel.AddErrors(_errorList);
                            }));
                            AddAnnotations();
                        }
                    }
                    else if (e.StateEntered == ConnectorStates.Loading && e.StateLeft == ConnectorStates.Idle)
                    {
                        _errorList.Clear();
                        if (e.Workspace == _mainModel.Workspace)
                        {
                            _dispatcher.Invoke(new Action(() =>
                            {
                                _mainModel.ErrorCount = 0;
                                _mainModel.Errors.Clear();
                            }));
                        }
                    }
                
                _previousConnectorState = e.StateEntered;
            }
        }
        
        public void Dispose()
        {
            _connector.OnStateChanged    -= OnConnectorStateChanged;
            _connector.OnProgressUpdated -= OnConnectorProgressUpdated;
        }
        #endregion
       
        #region [Helpers]
               
        void AddErrors()
        {
            _errorList.Clear();
            if (_connector.ErrorList.total_problems > 0)
            {
                foreach (var errors in _connector.ErrorList.problems.OrderBy(x => x.file))
                {
                    _errorList.Add(new ErrorListViewModel(errors.file, errors.problems.OrderBy(x => x.line).Select(x => new ErrorItemViewModel(x, errors.file)), false, _nppHelper));

                }
            }
        }

        private void AddAnnotations()
        {
            //find out which files are open in both views and add annotations if applicable

            string aCurrentFilePath = _nppHelper.GetCurrentFilePath().Replace('\\', '/');
            var aCurrentFileErrors  = _errorList.FirstOrDefault(x => x.FilePath.Equals(aCurrentFilePath, StringComparison.InvariantCultureIgnoreCase));
            _annotationsManager.AddErrors(aCurrentFileErrors, _nppHelper.CurrentScintilla);

            var mainFiles      = _nppHelper.GetOpenFiles(NppMsg.PRIMARY_VIEW);
            var secondaryFiles = _nppHelper.GetOpenFiles(NppMsg.SECOND_VIEW);

            //check current doc index
            var mainDoxIndex = _nppHelper.CurrentDocIndex(NppMsg.MAIN_VIEW);
            var secondaryDocIndex = _nppHelper.CurrentDocIndex(NppMsg.SUB_VIEW);

            //now refresh annotations to file, if file belongs to workspace that was just updated
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
        private static readonly object _lock            = new object();         //!< Mutex.
        private readonly Dispatcher _dispatcher         = null;                 //!< UI Dispatcher.
        private IError _annotationsManager              = null;
        #endregion
    }
}