﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using RTextNppPlugin.WpfControls;

namespace RTextNppPlugin.ViewModels
{
    /**
     * A ViewModel for the console.
     * The model is responsible for holding information about all loaded automate workspaces.
     * The model provide means to update the console, error list and rtext find windows.
     */
    class ConsoleViewModel : BindableObject, IConsoleViewModel
    {
        /**
         * Constructor.
         *
         * \param   workspace   The workspace.
         */
        public ConsoleViewModel()
        {
                        
        }

        public void addWorkspace(string workspace)
        {
            var workspaceModel = _workspaceCollection.FirstOrDefault(x => x.Workspace.Equals(workspace, StringComparison.InvariantCultureIgnoreCase));
            if (workspaceModel == null)
            {
                _workspaceCollection.Add(new WorkspaceViewModel(workspace));
            }
            WorkspaceExists = true;
        }

        public void removeWorkspace(string workspace)
        {
            var workspaceModel = _workspaceCollection.FirstOrDefault(x => x.Workspace.Equals(workspace, StringComparison.InvariantCultureIgnoreCase));
            if (workspaceModel != null)
            {
                _workspaceCollection.RemoveAt(_workspaceCollection.IndexOf(workspaceModel));
            }
            if(_workspaceCollection.Count == 0)
            {
                _workspaceExists = false;
            }
        }

        public bool WorkspaceExists
        {
            get
            {
                return _workspaceCollection.Count > 0;
            }
            private set
            {
                if(value == _workspaceExists)
                {
                    return;
                }
                else
                {
                    _workspaceExists = value;
                    base.RaisePropertyChanged("WorkspaceExists");
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
         * Gets a collection of workspaces.
         *
         * \return  A Collection of workspaces.
         */
        public ObservableCollection<WorkspaceViewModel> WorkspaceCollection
        {
            get
            {
                return _workspaceCollection;
            }
        }
       
        private ObservableCollection<WorkspaceViewModel> _workspaceCollection = new ObservableCollection<WorkspaceViewModel>();
        private bool _workspaceExists = false;
        private int _index = 0;
        //private readonly string _workspace = null; //!< Holds associated workspace for this viewmodel
    }
}