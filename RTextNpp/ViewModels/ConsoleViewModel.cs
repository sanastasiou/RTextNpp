﻿using RTextNppPlugin.WpfControls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using RTextNppPlugin.Automate;

namespace RTextNppPlugin.ViewModels
{
    /**
     * A ViewModel for the console.
     * The model is responsible for holding information about all loaded automate workspaces.
     * The model provide means to update the console, error list and rtext find windows.
     */
    class ConsoleViewModel : BindableObject, IConsoleViewModelBase, IDisposable
    {

        #region Interface
        /**
         * Constructor.
         *
         * \param   workspace   The workspace.
         */
        public ConsoleViewModel()
        {
            #if DEBUG
            AddWorkspace(Constants.DEBUG_CHANNEL);
            #endif
            AddWorkspace(Constants.GENERAL_CHANNEL);            
            //subscribe to connector manager for workspace events
            ConnectorManager.Instance.OnConnectorAdded += ConnectorManagerOnConnectorAdded;
        }

        void ConnectorManagerOnConnectorAdded(object source, ConnectorManager.ConnectorAddedEventArgs e)
        {

            //change to newly added workspace            
            AddWorkspace(e.Workspace, e.Connector);
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
                    _workspaceCollection.Add(new WorkspaceViewModel(workspace, ref connector));
                }
                Index = _workspaceCollection.IndexOf(_workspaceCollection.Last());
            }
            else
            {
                Index = _workspaceCollection.IndexOf(workspaceModel);
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
                if (value == _index)
                {
                    return;
                }
                else
                {
                    _index = value;
                    base.RaisePropertyChanged("Index");
                    base.RaisePropertyChanged("Workspace");
                    base.RaisePropertyChanged("IsBusy");
                    base.RaisePropertyChanged("IsAutomateWorkspace");
                }
            }
        }

        /**
         * Gets the workspace.
         *
         * \return  The workspace.
         */
        public string Workspace
        {
            get
            {
                return _workspaceCollection[_index].Workspace;
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
                return _workspaceCollection[_index].IsBusy;
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
                return _workspaceCollection[_index].ProgressPercentage;
            }
        }

        public bool IsAutomateWorkspace
        {
            get
            {
                return _workspaceCollection[_index].IsAutomateWorkspace;
            }
        }

        public bool IsActive
        {
            get
            {
                return _workspaceCollection[_index].IsActive;
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

        #region [Data Members]
        private ObservableCollection<IConsoleViewModelBase> _workspaceCollection = new ObservableCollection<IConsoleViewModelBase>();
        private int _index                                                    = 0;
        #endregion

        #region [Helpers]

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose any disposable fields here
                GC.SuppressFinalize(this);
            }
            ConnectorManager.Instance.OnConnectorAdded -= ConnectorManagerOnConnectorAdded;
        }

        #endregion
    }
}
