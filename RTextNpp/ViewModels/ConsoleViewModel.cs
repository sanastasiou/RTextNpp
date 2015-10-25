using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.RText;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.WpfControls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Media;
using RTextNppPlugin.Utilities.Settings;
namespace RTextNppPlugin.ViewModels
{
    /**
     * A ViewModel for the console.
     * The model is responsible for holding information about all loaded rtext workspaces.
     * The model provide means to update the console, error list and rtext find windows.
     */
    internal class ConsoleViewModel : BindableObject, IConsoleViewModelBase, IDisposable
    {
        #region [Data Members]
        private ObservableCollection<IConsoleViewModelBase> _workspaceCollection = new ObservableCollection<IConsoleViewModelBase>();
        private int _index                                                       = -1;
        private bool _isBusy                                                     = false;
        private bool _isLoading                                                  = false;
        private bool _isActive                                                   = false;
        private bool _isAutomateWorkspace                                        = false;
        private double _progressPercentage                                       = 0.0;
        private string _workspace                                                = null;
        private string _currentCommand                                           = String.Empty;
        private int _errorCount                                                  = 0;
        private readonly ConnectorManager _cmanager                              = null;
        private BulkObservableCollection<ErrorListViewModel> _errorList          = new BulkObservableCollection<ErrorListViewModel>();
        private INpp _nppHelper                                                  = null;
        private Color _expanderHeaderBackground                                  = Colors.White;
        private Color _expanderHeaderTextForeground                              = Colors.Black;
        IStyleConfigurationObserver _styleObserver                               = null;
        #endregion
        #region Interface
        /**
         * Constructor.
         *
         * \param   workspace   The workspace.
         */
        public ConsoleViewModel(ConnectorManager cmanager, INpp npphelper, IStyleConfigurationObserver styleObserver)
        {
            if(cmanager == null)
            {
                throw new ArgumentNullException("cmanager");
            }
            if(npphelper == null)
            {
                throw new ArgumentNullException("nppHelper");
            }
            if(styleObserver == null)
            {
                throw new ArgumentNullException("styleObserver");
            }
            _cmanager = cmanager;
            #if DEBUG
            AddWorkspace(Constants.DEBUG_CHANNEL);
            #endif
            AddWorkspace(Constants.GENERAL_CHANNEL);
            //subscribe to connector manager for workspace events
            _cmanager.OnConnectorAdded += ConnectorManagerOnConnectorAdded;
            Index = 0;
            _nppHelper     = npphelper;
            _styleObserver = styleObserver;
            _styleObserver.OnSettingsChanged += OnStyleObserverSettingsChanged;
        }
        void OnStyleObserverSettingsChanged(object sender, EventArgs e)
        {
            IWordsStyle aErrorOverviewStyle = _styleObserver.GetStyle(Constants.Wordstyles.ERROR_OVERVIEW);
            ExpanderHeaderBackground        = aErrorOverviewStyle.Background;
            ExpanderHeaderTextForeground    = aErrorOverviewStyle.Foreground;
        }
        public Dispatcher Dispatcher { get; set; }
        void ConnectorManagerOnConnectorAdded(object source, ConnectorManager.ConnectorAddedEventArgs e)
        {
            //change to newly added workspace
            AddWorkspace(e.Workspace, e.Connector);
        }
        public string GetCurrentLogChannel()
        {
            return _workspaceCollection[_index].Workspace;
        }
        public void AddWorkspace(string workspace, Connector connector = null)
        {
            var workspaceModel = _workspaceCollection.FirstOrDefault(x => x.Workspace.Equals(workspace, StringComparison.InvariantCultureIgnoreCase));
            if (workspaceModel == null)
            {
                if (connector == null)
                {
                    _workspaceCollection.Add(new WorkspaceViewModelBase(workspace));
                }
                else
                {
                    _workspaceCollection.Add(new WorkspaceViewModel(workspace, ref connector, this, _nppHelper));
                }
                Index = _workspaceCollection.IndexOf(_workspaceCollection.Last());
            }
            else
            {
                Index = _workspaceCollection.IndexOf(workspaceModel);
            }
        }
        public Color ExpanderHeaderBackground
        {
            get
            {
                return _expanderHeaderBackground;
            }
            set
            {
                if(value != _expanderHeaderBackground)
                {
                    _expanderHeaderBackground = value;
                    base.RaisePropertyChanged("ExpanderHeaderBackground");
                }
            }
        }
        public Color ExpanderHeaderTextForeground
        {
            get
            {
                return _expanderHeaderTextForeground;
            }
            set
            {
                if (value != _expanderHeaderTextForeground)
                {
                    _expanderHeaderTextForeground = value;
                    base.RaisePropertyChanged("ExpanderHeaderTextForeground");
                }
            }
        }

        /**
         * Gets or sets the zero-based index of the workspace list.
         *
         * \return  The index.
         */
        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                if (value != _index)
                {
                    _index              = value;
                    IsActive            = _workspaceCollection[_index].IsActive;
                    IsBusy              = _workspaceCollection[_index].IsBusy;
                    IsLoading           = _workspaceCollection[_index].IsLoading;
                    IsAutomateWorkspace = _workspaceCollection[_index].IsAutomateWorkspace;
                    ProgressPercentage  = _workspaceCollection[_index].ProgressPercentage;
                    Workspace           = _workspaceCollection[_index].Workspace;
                    ActiveCommand       = _workspaceCollection[_index].ActiveCommand;
                    ErrorCount          = _workspaceCollection[_index].ErrorCount;
                    base.RaisePropertyChanged("Index");
                }
            }
        }
        public string Workspace
        {
            get
            {
                return _workspace;
            }
            set
            {
                if(value != _workspace)
                {
                    _workspace = value;
                    base.RaisePropertyChanged("Workspace");
                }
            }
        }
        /**
         * \brief   Gets a value indicating whether the backend is loading is model loading.
         *
         * \return  true if this object is model loading, false if not.
         */
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                if(value != _isBusy)
                {
                    _isBusy = value;
                    base.RaisePropertyChanged("IsBusy");
                }
            }
        }
        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
            set
            {
                if(value !=  _isLoading)
                {
                    _isLoading = value;
                    base.RaisePropertyChanged("IsLoading");
                }
            }
        }
        public int ErrorCount
        {
            get
            {
                return _errorCount;
            }
            set
            {
                if(value != _errorCount)
                {
                    _errorCount = value;
                    base.RaisePropertyChanged("ErrorCount");
                }
            }
        }
        public BulkObservableCollection<ErrorListViewModel> Errors
        {
            get
            {
                return _errorList;
            }
        }
        /**
         * Gets the progress percentage.
         *
         * \return  The progress percentage of current backend command.
         */
        public double ProgressPercentage
        {
            get
            {
                return _progressPercentage;
            }
            set
            {
                if (value != _progressPercentage)
                {
                    _progressPercentage = value;
                    base.RaisePropertyChanged("ProgressPercentage");
                }
            }
        }
        public bool IsAutomateWorkspace
        {
            get
            {
                return _isAutomateWorkspace;
            }
            set
            {
                if(value != _isAutomateWorkspace)
                {
                    _isAutomateWorkspace = value;
                    base.RaisePropertyChanged("IsAutomateWorkspace");
                }
            }
        }
        public bool IsActive
        {
            get
            {
                return _isActive;
            }
            set
            {
                if(value != _isActive)
                {
                    _isActive = value;
                    base.RaisePropertyChanged("IsActive");
                }
            }
        }
        public string ActiveCommand
        {
            get
            {
                return _currentCommand;
            }
            set
            {
                if(value != _currentCommand)
                {
                    _currentCommand = value;
                    base.RaisePropertyChanged("ActiveCommand");
                }
            }
        }
        /**
         * Gets a collection of workspaces.
         *
         * \return  A Collection of workspaces.
         */
        public ObservableCollection<IConsoleViewModelBase> WorkspaceCollection
        {
            get
            {
                return _workspaceCollection;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
        #region [Helpers]
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose any disposable fields here
                GC.SuppressFinalize(this);
            }
            _cmanager.OnConnectorAdded -= ConnectorManagerOnConnectorAdded;
            _styleObserver.OnSettingsChanged -= OnStyleObserverSettingsChanged;
        }
        #endregion
    }
}