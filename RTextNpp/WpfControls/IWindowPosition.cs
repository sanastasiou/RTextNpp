using System;
namespace RTextNppPlugin.WpfControls
{
    interface IWindowPosition
    {
        bool IsEdgeOfScreenReached(double offset);
    }
}