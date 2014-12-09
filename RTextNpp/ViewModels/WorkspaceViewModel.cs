using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTextNppPlugin.WpfControls;

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
