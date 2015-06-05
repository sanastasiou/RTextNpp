using System;
using WindowsSubclassWrapper;

namespace RTextNppPlugin.Utilities
{
    class ScintillaMessageInterceptor : WindowSubclassCliWrapper
    {
        #region [Events]
        public class ScintillaFocusChangedEventArgs
        {
            public bool Focused;
            public UIntPtr WindowHandle;
            public bool Handled;
        }

        public delegate void ScintillaFocusChangedEvent(object source, ScintillaFocusChangedEventArgs e);

        public event ScintillaFocusChangedEvent ScintillaFocusChanged;

        public class MouseWheelMovedEventArgs
        {
            public uint Msg;
            public UIntPtr WParam;
            public IntPtr LParam;
            public bool Handled;
        }

        public delegate void MouseWheelMovedEvent(object source, MouseWheelMovedEventArgs e);

        public event MouseWheelMovedEvent MouseWheelMoved;
        #endregion

        public ScintillaMessageInterceptor(IntPtr nppHandle)
            : base(nppHandle)
        {
        }


        public override bool OnMessageReceived(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            VisualUtilities.WindowsMessage aMsg = (VisualUtilities.WindowsMessage)msg;
            switch (aMsg)
            {
                case VisualUtilities.WindowsMessage.WM_MOUSEWHEEL:
                    {
                        var e = new MouseWheelMovedEventArgs { Handled = false, Msg = msg, WParam = wParam, LParam = lParam };
                        if (MouseWheelMoved != null)
                        {
                            MouseWheelMoved(this, e);
                        }
                        return e.Handled;
                    }
                case VisualUtilities.WindowsMessage.WM_KILLFOCUS:
                    {
                        var e = new ScintillaFocusChangedEventArgs { Focused = false, WindowHandle = wParam, Handled = false };
                        if (ScintillaFocusChanged != null)
                        {
                            ScintillaFocusChanged(this, e);
                        }
                        return e.Handled;
                    }
                case VisualUtilities.WindowsMessage.WM_SETFOCUS:
                    {
                        var e = new ScintillaFocusChangedEventArgs { Focused = true, WindowHandle = wParam, Handled = false };
                        if (ScintillaFocusChanged != null)
                        {
                            ScintillaFocusChanged(this, e);
                        }
                        return e.Handled;
                    }
            }
            return false;
        }
    }

    class NotepadMessageInterceptor : WindowSubclassCliWrapper
    {
        #region [Events]
        public class MenuLoopStateChangedEventArgs
        {
            public bool IsMenuLoopActive;
            public bool Handled;
        }

        public delegate void MenuLoopStateChangedEvent(object source, MenuLoopStateChangedEventArgs e);

        public event MenuLoopStateChangedEvent MenuLoopStateChanged;

        #endregion

        public NotepadMessageInterceptor(IntPtr nppHandle)
            : base(nppHandle)
        {
        }


        public override bool OnMessageReceived(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            VisualUtilities.WindowsMessage aMsg = (VisualUtilities.WindowsMessage)msg;
            switch (aMsg)
            {
                case VisualUtilities.WindowsMessage.WM_ENTERMENULOOP:
                    {
                        var e = new MenuLoopStateChangedEventArgs { Handled = false, IsMenuLoopActive = true };
                        if (MenuLoopStateChanged != null)
                        {
                            MenuLoopStateChanged(this, e);
                        }
                        return e.Handled;
                    }
                case VisualUtilities.WindowsMessage.WM_EXITMENULOOP:
                    {
                        var e = new MenuLoopStateChangedEventArgs { Handled = false, IsMenuLoopActive = false };
                        if (MenuLoopStateChanged != null)
                        {
                            MenuLoopStateChanged(this, e);
                        }
                        return e.Handled;
                    }
            }
            return false;
        }
    }
}
