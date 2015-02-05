using RTextNppPlugin.Automate;

namespace RTextNppPlugin.ViewModels
{
    class WorkspaceViewModel
    {
        public WorkspaceViewModel(string workspace, ref Connector connector)
        {
            _workspace = workspace;
            _connector = connector;
        }

        public string Workspace
        {
            get
            {
                return _workspace;
            }
        }

        /**
         * \brief   Gets a value indicating whether this workspace is currently loading.
         *
         * \return  true if this workspace is loading, false if not.
         */
        public bool IsLoading
        {
            get
            {
                return _isLoading;
            }
        }

        public double ProgressPercentage { get; private set; }

        private readonly string _workspace = null;  //!< Associated namespace name.
        private bool _isLoading            = false; //!< Model loading status.
        private Connector _connector       = null;  //!< Associated connector instance.
    }
}
