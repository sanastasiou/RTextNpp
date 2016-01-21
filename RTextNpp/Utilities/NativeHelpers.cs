using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RTextNppPlugin.Utilities
{
    internal class NativeHelpers : RTextNppPlugin.Utilities.INativeHelpers
    {
        public delegate int HookProc(int code, UIntPtr wParam, IntPtr lParam);

        #region [Dll Imports]
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern int SendMessage(IntPtr hWnd, int msg, IntPtr[] wParam, int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, out int lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern long GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern IntPtr SetWindowsHookEx(VisualUtilities.HookType code, HookProc func, IntPtr hInstance, int threadID);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern int UnhookWindowsHookEx(IntPtr hhook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern int MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern uint MapVirtualKeyEx(uint uCode, MapVirtualKeyMapTypes uMapType, IntPtr dwhkl);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll")]
        protected static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer, int bufferSize, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        protected static extern int CallNextHookEx(IntPtr hhook, int code, UIntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        protected static extern IntPtr GetFocus();

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        protected static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, [MarshalAs(UnmanagedType.LPStr)] string lParam);
        #endregion        

        public Rectangle GetClientRectFromControl(IntPtr hwnd)
        {
            return Screen.FromHandle(hwnd).WorkingArea;
        }

        public Rectangle GetClientRectFromPoint(Point p)
        {
            return Screen.FromPoint(p).WorkingArea;
        }

        internal enum MapVirtualKeyMapTypes : uint
        {
            /// <summary>
            /// uCode is a virtual-key code and is translated into a scan code.
            /// If it is a virtual-key code that does not distinguish between left- and
            /// right-hand keys, the left-hand scan code is returned.
            /// If there is no translation, the function returns 0.
            /// </summary>
            MAPVK_VK_TO_VSC = 0x00,

            /// <summary>
            /// uCode is a scan code and is translated into a virtual-key code that
            /// does not distinguish between left- and right-hand keys. If there is no
            /// translation, the function returns 0.
            /// </summary>
            MAPVK_VSC_TO_VK = 0x01,

            /// <summary>
            /// uCode is a virtual-key code and is translated into an unshifted
            /// character value in the low-order word of the return value. Dead keys (diacritics)
            /// are indicated by setting the top bit of the return value. If there is no
            /// translation, the function returns 0.
            /// </summary>
            MAPVK_VK_TO_CHAR = 0x02,

            /// <summary>
            /// Windows NT/2000/XP: uCode is a scan code and is translated into a
            /// virtual-key code that distinguishes between left- and right-hand keys. If
            /// there is no translation, the function returns 0.
            /// </summary>
            MAPVK_VSC_TO_VK_EX = 0x03,

            /// <summary>
            /// Not currently documented
            /// </summary>
            MAPVK_VK_TO_VSC_EX = 0x04
        }

        internal const int KL_NAMELENGTH = 9;
        internal const int KLF_ACTIVATE = 0x00000001;

        public int ISendMessage(IntPtr hWnd, int msg, IntPtr[] wParam, int lParam)
        {
            return NativeHelpers.SendMessage(hWnd, msg, wParam, lParam);
        }

        public IntPtr ISendMessage(IntPtr hWnd, int msg, IntPtr wParam, string str)
        {
            return SendMessage(hWnd, msg, wParam, str);
        }
        
        public IntPtr ISendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            return SendMessage(hWnd, msg, wParam, lParam);
        }

        public IntPtr ISendMessage(IntPtr hWnd, int msg, int wParam, out int lParam)
        {
            return SendMessage(hWnd, msg, wParam, out lParam);
        }
       
        public IntPtr ISendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam)
        {
            return SendMessage(hWnd, msg, wParam, lParam);
        }

        
        public bool IClientToScreen(IntPtr hWnd, ref Point lpPoint)
        {
            return ClientToScreen(hWnd, ref lpPoint);
        }

        public bool IScreenToClient(IntPtr hWnd, ref Point lpPoint)
        {
           return ScreenToClient(hWnd, ref lpPoint);
        }

        public long IGetWindowRect(IntPtr hWnd, ref Rectangle lpRect)
        {
            return GetWindowRect(hWnd, ref lpRect);
        }

        public IntPtr ISetWindowsHookEx(VisualUtilities.HookType code, HookProc func, IntPtr hInstance, int threadID)
        {
            return SetWindowsHookEx(code, func, hInstance, threadID);
        }

        public int IUnhookWindowsHookEx(IntPtr hhook)
        {
            return UnhookWindowsHookEx(hhook);
        }

        public int IMapVirtualKey(uint uCode, uint uMapType)
        {
            return MapVirtualKey(uCode, uMapType);
        }

        public uint IMapVirtualKeyEx(uint uCode, MapVirtualKeyMapTypes uMapType, IntPtr dwhkl)
        {
            return MapVirtualKeyEx(uCode, uMapType, dwhkl);
        }

        public int IGetKeyboardState(byte[] pbKeyState)
        {
            return GetKeyboardState(pbKeyState);
        }

        
        public int IToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer, int bufferSize, uint flags)
        {
            return ToUnicode(virtualKeyCode, scanCode, keyboardState, receivingBuffer, bufferSize, flags);
        }

        public int ICallNextHookEx(IntPtr hhook, int code, UIntPtr wParam, IntPtr lParam)
        {
            return CallNextHookEx(hhook, code, wParam, lParam);
        }

        public string GetCharsFromKeys(Keys key, bool shift, bool altGr)
        {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];
            if (shift)
            {
                keyboardState[(int)Keys.ShiftKey]   = 0xff;
            }
            if (altGr)
            {
                keyboardState[(int)Keys.ControlKey] = 0xff;
                keyboardState[(int)Keys.Menu]       = 0xff;
            }
            ToUnicode((uint)key, 0, keyboardState, buf, 256, 0);
            return buf.ToString();
        }

        public IntPtr IGetFocus()
        {
            return GetFocus();
        }
    }
}
