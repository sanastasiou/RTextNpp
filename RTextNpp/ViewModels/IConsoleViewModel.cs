using RTextNppPlugin.Automate;

namespace RTextNppPlugin.ViewModels
{
    /**
     * Interface for console view model.
     */
    interface IConsoleViewModelBase
    {
        void AddWorkspace(string workspace, Connector connector = null);
        string Workspace { get; }
        bool IsBusy { get; }
        bool IsActive { get; }
        bool IsAutomateWorkspace { get; }
        double ProgressPercentage { get; }
    }
}
