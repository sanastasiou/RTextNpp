using System;

using RTextNppPlugin.Automate;

namespace RTextNppPlugin.ViewModels
{
    class WorkspaceViewModel : WorkspaceViewModelBase, IConsoleViewModelBase
    {
        #region [Interface]
        public WorkspaceViewModel(string workspace, ref Connector connector) : base(workspace)
        {
            _connector = connector;
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
                return _isLoading;
            }
        }

        new public double ProgressPercentage 
        { 
            get
            {
                return _percentage;
            }
        }

        public void AddConnector(Connector connector)
        {
            _connector = connector;
        }

        new public bool IsActive
        {
            get { return _isActive; }
        }

        new public bool IsAutomateWorkspace
        {
            get { return true; }
        }

        #endregion

        #region [Data Members]

        private bool _isActive = false;
        private double _percentage   = 0.0;   //!< The current command percentage.
        private bool _isLoading      = false; //!< Model loading status.
        private Connector _connector = null;  //!< Associated connector instance.

        #endregion
    }
}
