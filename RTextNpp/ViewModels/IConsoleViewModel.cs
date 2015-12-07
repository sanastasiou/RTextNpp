using RTextNppPlugin.RText;
using RTextNppPlugin.Utilities.Settings;
using System.Collections.Generic;
namespace RTextNppPlugin.ViewModels
{
    /**
     * Interface for console view model.
     */
    interface IConsoleViewModelBase
    {
        void AddWorkspace(string workspace, ISettings settings = null, Connector connector = null);
        string Workspace { get; }
        bool IsBusy { get; }
        bool IsActive { get; }
        bool IsLoading { get; }
        bool IsAutomateWorkspace { get; }
        double ProgressPercentage { get; }
        string ActiveCommand { get; }
        int ErrorCount { get; }
        IEnumerable<ErrorListViewModel> WorkspaceErrors { get; }
    }
}