using System;
using System.Drawing;
using System.Windows.Forms;
namespace RTextNppPlugin.Utilities
{
    interface INativeHelpers
    {
        int ICallNextHookEx(IntPtr hhook, int code, UIntPtr wParam, IntPtr lParam);
        bool IClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);
        int IGetKeyboardState(byte[] pbKeyState);
        long IGetWindowRect(IntPtr hWnd, ref System.Drawing.Rectangle lpRect);
        int IMapVirtualKey(uint uCode, uint uMapType);
        uint IMapVirtualKeyEx(uint uCode, NativeHelpers.MapVirtualKeyMapTypes uMapType, IntPtr dwhkl);
        bool IScreenToClient(IntPtr hWnd, ref System.Drawing.Point lpPoint);
        IntPtr ISendMessage(IntPtr hWnd, int msg, int wParam, out int lParam);
        IntPtr ISendMessage(IntPtr hWnd, int msg, int wParam, System.Text.StringBuilder lParam);
        IntPtr ISendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        int ISendMessage(IntPtr hWnd, int msg, IntPtr[] wParam, int lParam);
        IntPtr ISetWindowsHookEx(VisualUtilities.HookType code, NativeHelpers.HookProc func, IntPtr hInstance, int threadID);
        int IToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, System.Text.StringBuilder receivingBuffer, int bufferSize, uint flags);
        int IUnhookWindowsHookEx(IntPtr hhook);
        string GetCharsFromKeys(Keys key, bool shift, bool altGr);

        Rectangle GetClientRectFromControl(IntPtr hwnd);

        Rectangle GetClientRectFromPoint(Point p);

        IntPtr IGetFocus();
    }
}
