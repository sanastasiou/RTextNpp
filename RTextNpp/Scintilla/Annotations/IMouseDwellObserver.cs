using System;
namespace RTextNppPlugin.Scintilla.Annotations
{
    interface IMouseDwellObserver
    {
        void Dispose();
        event MouseDwellObserver.DwellEndingCallback OnDwellEndingEvent;
        event MouseDwellObserver.DwellStartingCallback OnDwellStartingEvent;
    }
}
