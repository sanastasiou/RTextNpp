using System.Windows.Forms;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RTextNppPlugin;
using RTextNppPlugin.DllExport;
using CSScriptIntellisense;
namespace RTextNppPlugin.Utilities
{
    abstract class GlobalMouseHook
    {
        #region Mouse events
        protected IntPtr _MouseHookHandle;
        protected Win32.HookProc _MouseDelegate;
        protected IWin32 _win32Helper;
        #endregion
        #region Helpers
        internal GlobalMouseHook(IWin32 win32Helper)
        {
            _win32Helper = win32Helper;
        }
        abstract internal int MouseHookProc(int nCode, UIntPtr wParam, IntPtr lParam);
        protected void EnsureSubscribedToGlobalMouseEvents()
        {
            // install Mouse hook only if it is not installed and must be installed
            if (_MouseHookHandle == IntPtr.Zero)
            {
                //See comment of this field. To avoid GC to clean it up.
                _MouseDelegate = MouseHookProc;
                //install hook
                _MouseHookHandle = _win32Helper.ISetWindowsHookEx(VisualUtilities.HookType.WH_MOUSE_LL, _MouseDelegate, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
                //If SetWindowsHookEx fails.
                if (_MouseHookHandle == IntPtr.Zero)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set.
                    int errorCode = Marshal.GetLastWin32Error();
                    Trace.WriteLine(String.Format("Error in global mouse hook : {0}", errorCode));
                }
            }
        }
        abstract protected void TryUnsubscribeFromGlobalMouseEvents();
        protected void ForceUnsunscribeFromGlobalMouseEvents()
        {
            if (_MouseHookHandle != IntPtr.Zero)
            {
                //uninstall hook
                int result = _win32Helper.IUnhookWindowsHookEx(_MouseHookHandle);
                //reset invalid handle
                _MouseHookHandle = IntPtr.Zero;
                //Free up for GC
                _MouseDelegate = null;
                //if failed and exception must be thrown
                if (result == 0)
                {
                    //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set.
                    int errorCode = Marshal.GetLastWin32Error();
                    //Initializes and throws a new instance of the Win32Exception class with the specified error.
                    Trace.WriteLine(String.Format("Error in global mouse hook : {0}", errorCode));
                }
            }
        }
        #endregion
    }
    class GlobalClickInterceptor : GlobalMouseHook
    {
        private event EventHandler<MouseEventExtArgs> _MouseClick;
        internal event EventHandler<MouseEventExtArgs> MouseClick
        {
            add
            {
                if (_MouseClick == null)
                {
                    EnsureSubscribedToGlobalMouseEvents();
                    _MouseClick += value;
                }
            }
            remove
            {
                if (_MouseClick != null)
                {
                    _MouseClick -= value;
                    TryUnsubscribeFromGlobalMouseEvents();
                }
            }
        }
        internal GlobalClickInterceptor(IWin32 _win32Helper)
            : base(_win32Helper)
        {
        }
        internal override int MouseHookProc(int nCode, UIntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                VisualUtilities.MouseMessages aMsg = (VisualUtilities.MouseMessages)wParam.ToUInt32();
                //Marshall the data from callback.
                MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));
                //detect button clicked
                System.Windows.Forms.MouseButtons button = System.Windows.Forms.MouseButtons.None;
                switch (aMsg)
                {
                    case VisualUtilities.MouseMessages.WM_LBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Left;
                        break;
                    case VisualUtilities.MouseMessages.WM_RBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Right;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCXBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Left;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCMBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Middle;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCRBUTTONDBLCLK:
                        button = System.Windows.Forms.MouseButtons.Right;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCRBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Right;
                        break;
                    case VisualUtilities.MouseMessages.WM_NCLBUTTONDOWN:
                        button = System.Windows.Forms.MouseButtons.Left;
                        break;
                    default:
                        return _win32Helper.ICallNextHookEx(_MouseHookHandle, nCode, wParam, lParam);
                }
                //generate event
                MouseEventExtArgs e = new MouseEventExtArgs(button, 1, mouseHookStruct.Point.X, mouseHookStruct.Point.Y, 0);
                if (_MouseClick != null)
                {
                    _MouseClick.Invoke(null, e);
                }
                if (e.Handled)
                {
                    return -1;
                }
            }
            //call next hook
            return _win32Helper.ICallNextHookEx(_MouseHookHandle, nCode, wParam, lParam);
        }
        override protected void TryUnsubscribeFromGlobalMouseEvents()
        {
            //if no subsribers are registered unsubsribe from hook
            if (_MouseClick == null)
            {
                ForceUnsunscribeFromGlobalMouseEvents();
            }
        }
    }
}