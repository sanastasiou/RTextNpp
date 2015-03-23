using System.Windows.Forms;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RTextNppPlugin;

namespace RTextNppPlugin.Utilities
{
    abstract class GlobalMouseHook : Win32
    {
        #region Mouse events       
        protected IntPtr _MouseHookHandle;
        protected HookProc _MouseDelegate;
        #endregion

        #region Helpers

        abstract public int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam);

        protected void EnsureSubscribedToGlobalMouseEvents()
        {
            // install Mouse hook only if it is not installed and must be installed
            if (_MouseHookHandle == IntPtr.Zero)
            {
                //See comment of this field. To avoid GC to clean it up.
                _MouseDelegate = MouseHookProc;
                //install hook
                _MouseHookHandle = SetWindowsHookEx(VisualUtilities.HookType.WH_MOUSE_LL, _MouseDelegate, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
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
                int result = UnhookWindowsHookEx(_MouseHookHandle);
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
}
