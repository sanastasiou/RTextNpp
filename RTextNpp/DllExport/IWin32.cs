using RTextNppPlugin.Utilities;
using System;
using System.Runtime.InteropServices;
using System.Text;
namespace RTextNppPlugin.DllExport
{
    interface IWin32
    {
        int ICallNextHookEx(IntPtr hhook, int code, UIntPtr wParam, IntPtr lParam);
        int ICheckMenuItem(IntPtr hmenu, int uIDCheckItem, int uCheck);
        IntPtr IGetMenu(IntPtr hWnd);
        int IGetPrivateProfileInt(string lpAppName, string lpKeyName, int nDefault, string lpFileName);
        IntPtr ISendMessage(IntPtr hWnd, NppMsg Msg, int wParam, out int lParam);
        int ISendMessage(IntPtr hWnd, NppMsg Msg, out string[] files, int numOfFiles);
        IntPtr ISendMessage(IntPtr hWnd, NppMsg Msg, int wParam, NppMenuCmd lParam);
        IntPtr ISendMessage(IntPtr hWnd, NppMsg Msg, int wParam, int lParam);
        IntPtr ISendMessage(IntPtr hWnd, NppMsg Msg, int wParam, IntPtr lParam);
        IntPtr ISendMessage(IntPtr hWnd, NppMsg Msg, int wParam, string lParam);
        IntPtr ISendMessage(IntPtr hWnd, NppMsg Msg, int wParam, StringBuilder lParam);
        IntPtr ISendMessage(IntPtr hWnd, NppMsg Msg, IntPtr wParam, int lParam);
        IntPtr ISendMessage(IntPtr hWnd, NppMsg Msg, IntPtr wParam, string lParam);
        IntPtr ISendMessage(IntPtr hWnd, SciMsg Msg, int wParam, int lParam);
        IntPtr ISendMessage(IntPtr hWnd, SciMsg Msg, int wParam, IntPtr lParam);
        IntPtr ISendMessage(IntPtr hWnd, SciMsg Msg, IntPtr wParam, IntPtr lParam);
        IntPtr ISendMessage(IntPtr hWnd, SciMsg Msg, int wParam, string lParam);
        IntPtr ISendMessage(IntPtr hWnd, SciMsg Msg, int wParam, StringBuilder lParam);
        IntPtr ISetWindowsHookEx(VisualUtilities.HookType code, Win32.HookProc func, IntPtr hInstance, int threadID);
        int IUnhookWindowsHookEx(IntPtr hhook);
        bool IWritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);
        IntPtr SendMenuCmd(IntPtr hWnd, NppMenuCmd wParam, int lParam);
        IntPtr ISendMessage(IntPtr hWnd, SciMsg Msg, string text);
        IntPtr ToUnmanagedArray(byte[] data);
    }
}