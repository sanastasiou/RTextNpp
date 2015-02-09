using System;
using RTextNppPlugin.Automate;

namespace RTextNppPlugin.ViewModels
{
    class WorkspaceViewModelBase : IConsoleViewModelBase
    {
        #region [Interface]

        public WorkspaceViewModelBase(string workspace)
        {
            _workspace = workspace;
        }

        public void AddWorkspace(string workspace, Connector connector = null)
        {
            _workspace = workspace;
        }

        public string Workspace
        {
            get { return _workspace; }
        }

        public bool IsBusy
        {
            get { return false; }
        }

        public double ProgressPercentage
        {
            get { return 100.0; }
        }

        public bool IsActive
        {
            get { return false; }
        }

        public bool IsAutomateWorkspace
        {
            get { return false; }
        }

        public bool IsLoading
        {
            get { return false; }
        }

        #endregion

        #region [Data Members]

        private string _workspace = null;  //!< Associated namespace name.

        #endregion
    }
}
