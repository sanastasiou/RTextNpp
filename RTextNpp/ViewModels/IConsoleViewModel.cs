﻿using RTextNppPlugin.Automate;

namespace RTextNppPlugin.ViewModels
{
    /**
     * Interface for console view model.
     * This interface provides means to modify the underlying view model of the console window of the plugin.
     */
    interface IConsoleViewModel
    {
        void addWorkspace(string workspace, Connector connector);
        void removeWorkspace(string workspace);
    }
}
