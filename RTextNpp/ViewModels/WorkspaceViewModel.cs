
namespace RTextNppPlugin.ViewModels
{
    class WorkspaceViewModel
    {
        public WorkspaceViewModel(string workspace)
        {
            _workspace = workspace;
        }

        public string Workspace
        {
            get
            {
                return _workspace;
            }
        }

        private readonly string _workspace = null;        
    }
}
