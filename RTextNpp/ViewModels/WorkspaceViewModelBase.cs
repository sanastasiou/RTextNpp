using System;
using RTextNppPlugin.RText;
using System.Collections.Generic;
using RTextNppPlugin.Utilities.Settings;
namespace RTextNppPlugin.ViewModels
{
    class WorkspaceViewModelBase : IConsoleViewModelBase
    {
        #region [Interface]
        
        public WorkspaceViewModelBase(string workspace)
        {
            _workspace = workspace;
        }
        
        public void AddWorkspace(string workspace, ISettings settings = null, Connector connector = null)
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
        
        public int ErrorCount
        {
            get { return 0; }
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
        
        public string ActiveCommand
        {
            get { return String.Empty; }
        }

        public IEnumerable<ErrorListViewModel> WorkspaceErrors 
        { 
            get
            {
                return _errorList;
            }
        }
        #endregion
       
        #region [Data Members]
        private string _workspace = null;  //!< Associated namespace name.
        protected IList<ErrorListViewModel> _errorList = new List<ErrorListViewModel>();
        #endregion
    }
}