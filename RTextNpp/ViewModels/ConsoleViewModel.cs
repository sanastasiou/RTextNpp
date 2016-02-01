using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.RText;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.WpfControls;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;
using System.Windows.Media;
using RTextNppPlugin.Utilities.Settings;
using System.Text;
using RTextNppPlugin.Scintilla;
using RTextNppPlugin.Scintilla.Annotations;


namespace RTextNppPlugin.ViewModels
{
    /**
     * A ViewModel for the console.
     * The model is responsible for holding information about all loaded RText workspaces.
     * The model provide means to update the console, error list and RText find windows.
     */
    internal class ConsoleViewModel : BindableObject, IConsoleViewModelBase, IDisposable
    {
        #region [Data Members]
        private ObservableCollection<IConsoleViewModelBase> _workspaceCollection  = new ObservableCollection<IConsoleViewModelBase>();
        private int _index                                                        = -1;
        private bool _isBusy                                                      = false;
        private bool _isLoading                                                   = false;
        private bool _isActive                                                    = false;
        private bool _isAutomateWorkspace                                         = false;
        private double _progressPercentage                                        = 0.0;
        private string _workspace                                                 = null;
        private string _currentCommand                                            = String.Empty;
        private int _errorCount                                                   = 0;
        private readonly ConnectorManager _cmanager                               = null;
        private BulkObservableCollection<ErrorListViewModel> _underlyingErrorList = null;
        private INpp _nppHelper                                                   = null;
        private Color _expanderHeaderBackground                                   = Colors.White;
        private Color _expanderHeaderTextForeground                               = Colors.Black;
        IStyleConfigurationObserver _styleObserver                                = null;
        private readonly Dispatcher _dispatcher                                   = null;
        private readonly ISettings _settings                                      = null;
        private readonly ILineVisibilityObserver _lineVisibilityObserver          = null;
        private readonly IMouseDwellObserver _mouseDwellObserver                  = null;
        private int _zoomSliderPosition                                           = 100;
        private bool _isSliderLoaded                                              = false;
        #endregion

        #region [Event Handlers]

        void OnStyleObserverSettingsChanged(object sender, EventArgs e)
        {
            IWordsStyle aErrorOverviewStyle = _styleObserver.GetStyle(Constants.StyleId.ERROR_OVERVIEW);
            ExpanderHeaderBackground = aErrorOverviewStyle.Background;
            ExpanderHeaderTextForeground = aErrorOverviewStyle.Foreground;
        }
        #endregion

        #region Interface
        /**
         * Constructor.
         *
         * \param   workspace   The workspace.
         */
        public ConsoleViewModel(ConnectorManager cmanager, 
                                INpp npphelper, 
                                IStyleConfigurationObserver styleObserver, 
                                Dispatcher dispatcher, 
                                ISettings settings, 
                                ILineVisibilityObserver lineVisibilityObserver, 
                                IMouseDwellObserver mouseDwellObserver)
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
            _cmanager               = cmanager;
            _lineVisibilityObserver = lineVisibilityObserver;
            #if DEBUG
            _workspaceCollection.Add(new WorkspaceViewModelBase(Constants.DEBUG_CHANNEL));
            #endif
            _workspaceCollection.Add(new WorkspaceViewModelBase(Constants.GENERAL_CHANNEL));
            //subscribe to connector manager for workspace events
            _cmanager.OnConnectorAdded       += ConnectorManagerOnConnectorAdded;
            Index                            = 0;
            _nppHelper                       = npphelper;
            _styleObserver                   = styleObserver;
            _styleObserver.OnSettingsChanged += OnStyleObserverSettingsChanged;
            _underlyingErrorList             = new BulkObservableCollection<ErrorListViewModel>();
            _dispatcher                      = dispatcher;
            _settings                        = settings;
            _mouseDwellObserver              = mouseDwellObserver;
            _settings.OnSettingChanged       += OnSettingChanged;
        }

        internal Dispatcher Dispatcher { get; set; }
        
        void ConnectorManagerOnConnectorAdded(object source, ConnectorManager.ConnectorAddedEventArgs e)
        {
            //change to newly added workspace
            AddWorkspace(e.Workspace, _settings, e.Connector);
        }

        public void AddWorkspace(string workspace, ISettings settings = null, Connector connector = null)
        {
            var workspaceModel = _workspaceCollection.FirstOrDefault(x => x.Workspace.Equals(workspace, StringComparison.InvariantCultureIgnoreCase));

            _dispatcher.Invoke(new Action(() =>
            {
                if (workspaceModel == null)
                {
                    if (connector == null)
                    {
                        _workspaceCollection.Add(new WorkspaceViewModelBase(workspace));
                    }
                    else
                    {
                        _workspaceCollection.Add(new WorkspaceViewModel(workspace, ref connector, this, _nppHelper, _dispatcher, settings, _lineVisibilityObserver, _mouseDwellObserver));
                    }
                    Index = _workspaceCollection.IndexOf(_workspaceCollection.Last());
                }
                else
                {
                    Index = _workspaceCollection.IndexOf(workspaceModel);
                }
            }));
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

                    if (Errors != null)
                    {
                        _dispatcher.Invoke(new Action(() =>
                        {
                            AddErrors(_workspaceCollection[_index].WorkspaceErrors);
                        }));
                    }
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
         * \brief   Gets a value indicating whether the back-end is loading is model loading.
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

        internal void AddErrors(IEnumerable<ErrorListViewModel> errors)
        {
            Errors.Clear();
            Errors.AddRange(errors);
        }

        public BulkObservableCollection<ErrorListViewModel> Errors
        {
            get
            {
                return _underlyingErrorList;
            }
        }

        public IEnumerable<ErrorListViewModel> WorkspaceErrors
        { 
            get
            {
                return Errors as IEnumerable<ErrorListViewModel>;
            }
        }
        
        /**
         * Gets the progress percentage.
         *
         * \return  The progress percentage of current back-end command.
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

        public int ZoomSliderPosition
        {
            get
            {
                return _zoomSliderPosition;
            }
            set
            {
                if(value != _zoomSliderPosition)
                {
                    _zoomSliderPosition = value;
                    base.RaisePropertyChanged("ZoomSliderPosition");
                    if (_isSliderLoaded)
                    {
                        _settings.Set(value, Settings.RTextNppSettings.ZoomSliderPosition);
                    }
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
            _styleObserver.OnSettingsChanged -= OnStyleObserverSettingsChanged;
            _settings.OnSettingChanged       -= OnSettingChanged;
        }

        #region [Event Handlers]
        private void OnSettingChanged(object source, Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == Settings.RTextNppSettings.ZoomSliderPosition)
            {
                ZoomSliderPosition = _settings.Get<int>(Settings.RTextNppSettings.ZoomSliderPosition);
            }
        }

        internal void OnSliderLoaded()
        {
            ZoomSliderPosition = _settings.Get<int>(Settings.RTextNppSettings.ZoomSliderPosition);
            _isSliderLoaded    = true;
        }
        #endregion


        #endregion
        
        #region [Helpers]

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose any disposable fields here
                GC.SuppressFinalize(this);
            }
            _cmanager.OnConnectorAdded       -= ConnectorManagerOnConnectorAdded;
            _styleObserver.OnSettingsChanged -= OnStyleObserverSettingsChanged;
        }
      
        #endregion
    }
}