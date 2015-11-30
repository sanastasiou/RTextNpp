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
        private ObservableCollection<ErrorListViewModel> _underlyingErrorList    = null;
        private INpp _nppHelper                                                  = null;
        private Color _expanderHeaderBackground                                  = Colors.White;
        private Color _expanderHeaderTextForeground                              = Colors.Black;
        IStyleConfigurationObserver _styleObserver                               = null;
        Dictionary<string, bool> _annotationList                                 = new Dictionary<string, bool>(100);
        private readonly Dispatcher _dispatcher                                  = null;
        #endregion

        #region [Event Handlers]

        void OnBufferActivated(object source, string file)
        {
            //add annotations if not already added
            if(_annotationList.ContainsKey(file))
            {
                if(!_annotationList[file])
                {
                    var errorList = _underlyingErrorList.FirstOrDefault(x => x.FilePath.Replace('\\', '/').Equals(file, StringComparison.InvariantCultureIgnoreCase));
                    if (errorList != null)
                    {
                        _annotationList[file] = true;
                        AddAnnotations(errorList);
                    }
                }
            }
            else
            {
                var errorList = _underlyingErrorList.FirstOrDefault(x => x.FilePath.Replace('\\', '/').Equals(file, StringComparison.InvariantCultureIgnoreCase));
                if (errorList != null)
                {
                    _annotationList[file] = true;
                    AddAnnotations(errorList);
                }
            }
        }

        void OnPreviewFileClosed(object source, string file)
        {
            //clear all annotations here
            _annotationList[file] = false;
            _nppHelper.ClearAllAnnotations();
        }

        void OnStyleObserverSettingsChanged(object sender, EventArgs e)
        {
            IWordsStyle aErrorOverviewStyle = _styleObserver.GetStyle(Constants.Wordstyles.ERROR_OVERVIEW);
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
        public ConsoleViewModel(ConnectorManager cmanager, INpp npphelper, IStyleConfigurationObserver styleObserver, Dispatcher dispatcher)
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
            _cmanager.OnConnectorAdded       += ConnectorManagerOnConnectorAdded;
            Index                            = 0;
            _nppHelper                       = npphelper;
            _styleObserver                   = styleObserver;
            _styleObserver.OnSettingsChanged += OnStyleObserverSettingsChanged;
            Plugin.PreviewFileClosed         += OnPreviewFileClosed;
            Plugin.BufferActivated           += OnBufferActivated;
            _underlyingErrorList = new ObservableCollection<ErrorListViewModel>();
            _dispatcher = dispatcher;
        }
            
        internal Dispatcher Dispatcher { get; set; }
        
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
                    _workspaceCollection.Add(new WorkspaceViewModel(workspace, ref connector, this, _nppHelper, _dispatcher));
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
        
        public ObservableCollection<ErrorListViewModel> Errors
        {
            get
            {
                return _underlyingErrorList;
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
            Plugin.PreviewFileClosed         -= OnPreviewFileClosed;
            _styleObserver.OnSettingsChanged -= OnStyleObserverSettingsChanged;
            Plugin.BufferActivated           -= OnBufferActivated;
        }

        internal void ClearAnnotations()
        {
            _annotationList[_nppHelper.GetCurrentFilePath()] = false;
            _nppHelper.ClearAllAnnotations();
        }

        internal void AddAnnotations(ErrorListViewModel model)
        {
            if (model != null)
            {
                //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                var aErrorGroupByLines = model.ErrorList.GroupBy(x => x.Line);
                foreach (var errorGroup in aErrorGroupByLines)
                {
                    StringBuilder aErrorDescription = new StringBuilder(errorGroup.Count() * 50);
                    int aErrorCounter = 0;
                    foreach (var error in errorGroup)
                    {
                        aErrorDescription.AppendFormat("{0} at line : {1} - {2}", error.Severity, error.Line, error.Message);
                        if (++aErrorCounter < errorGroup.Count())
                        {
                            aErrorDescription.Append("\n");
                        }
                    }
                    //npp offset for line
                    _nppHelper.SetAnnotationStyle((errorGroup.First().Line -1), 1);
                    _nppHelper.AddAnnotation((errorGroup.First().Line - 1), aErrorDescription);
                }
            }
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