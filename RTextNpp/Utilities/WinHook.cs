// ***********************************************************************
// Taken as is from CSScriptNpp
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Diagnostics;

namespace CSScriptIntellisense
{
    public class WinHook<T> : LocalWindowsHook, IDisposable where T : new()
    {
        static T instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                    instance = new T();
                return instance;
            }
        }

        protected WinHook()
            : base(HookType.WH_DEBUG)
        {
            m_filterFunc = this.Proc;
        }

        ~WinHook()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            if (IsInstalled)
                Uninstall();

            if (disposing)
                GC.SuppressFinalize(this);
        }

        protected void Install(HookType type)
        {
            base.m_hookType = type;
            base.Install();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected int Proc(int code, IntPtr wParam, IntPtr lParam)
        {
            if (code == 0) //Win32.HC_ACTION
            {
                if (HandleHookEvent(wParam, lParam))
                {
                    return 1;
                }
            }

            return CallNextHookEx(m_hhook, code, wParam, lParam);
        }

        virtual protected bool HandleHookEvent(IntPtr wParam, IntPtr lParam)
        {
            throw new NotSupportedException();
        }
    }

    public class MouseMonitor : WinHook<MouseMonitor>
    {
        public event Action MouseMove;
        public event Action MouseClicked;
        public event Action MouseReleased;
        public event Func<int, bool> MouseWheelMoved;

        private enum MouseMessages
        {
            WM_LBUTTONDOWN     = 0x0201,
            WM_LBUTTONUP       = 0x0202,
            WM_MOUSEMOVE       = 0x0200,
            WM_MOUSEWHEEL      = 0x020A,
            WM_RBUTTONDOWN     = 0x0204,
            WM_RBUTTONUP       = 0x0205,
            WM_NCMOUSEMOVE     = 0x00A0,
            WM_NCLBUTTONDOWN   = 0x00A1,
            WM_NCLBUTTONDBLCLK = 0x00A3,
            WM_NCRBUTTONDOWN   = 0x00A4,
            WM_NCRBUTTONDBLCLK = 0x00A6,
            WM_NCMBUTTONDOWN   = 0x00A7,
            WM_NCMBUTTONDBLCLK = 0x00A9,
            WM_NCXBUTTONDOWN   = 0x00AB,
            WM_NCXBUTTONDBLCLK = 0x00AD
        }

        override protected bool HandleHookEvent(IntPtr wParam, IntPtr lParam)
        {
            MouseMessages aMsg = (MouseMessages)wParam.ToInt32();
            switch (aMsg)
            {
                case MouseMessages.WM_LBUTTONDOWN:
                case MouseMessages.WM_RBUTTONDOWN:
                case MouseMessages.WM_NCXBUTTONDBLCLK:
                case MouseMessages.WM_NCXBUTTONDOWN:
                case MouseMessages.WM_NCMBUTTONDBLCLK:
                case MouseMessages.WM_NCMBUTTONDOWN:
                case MouseMessages.WM_NCRBUTTONDBLCLK:
                case MouseMessages.WM_NCRBUTTONDOWN:
                case MouseMessages.WM_NCLBUTTONDBLCLK:
                case MouseMessages.WM_NCLBUTTONDOWN:
                    if(MouseClicked != null)
                    {
                        MouseClicked();
                    }
                    break;
                case MouseMessages.WM_LBUTTONUP:
                case MouseMessages.WM_RBUTTONUP:
                    if(MouseReleased != null)
                    {
                        MouseReleased();
                    }
                    break;
                case MouseMessages.WM_MOUSEMOVE:
                case MouseMessages.WM_NCMOUSEMOVE:
                    if(MouseMove != null)
                    {
                        MouseMove();
                    }
                    break;                    
                case MouseMessages.WM_MOUSEWHEEL:
                    MouseHookStructEx aMouseData = (MouseHookStructEx)Marshal.PtrToStructure(lParam, typeof(MouseHookStructEx));
                    var wheelMovement            = GetWheelDeltaWParam(aMouseData.MouseData);

                    if(MouseWheelMoved != null)
                    {
                        return MouseWheelMoved(wheelMovement);
                    }
                    break;
                default:
                    break;
            }
            //return false to allow routing of the event
            return false;
        }

        public new void Install()
        {
            base.Install(HookType.WH_MOUSE);
        }

        private int GetWheelDeltaWParam(int mouseData) { return (short)(mouseData >> 16); }
    }

    public struct Modifiers
    {
        public bool IsCtrl;
        public bool IsShift;
        public bool IsAlt;
        public bool IsCapsLock;
    }

    public class KeyInterceptor : WinHook<KeyInterceptor>
    {
        [DllImport("USER32.dll")]
        static extern short GetKeyState(int nVirtKey);

        public static bool IsPressed(Keys key)
        {
            const int KEY_PRESSED = 0x8000;
            return Convert.ToBoolean(GetKeyState((int)key) & KEY_PRESSED);
        }

        public static Modifiers GetModifiers()
        {
            return new Modifiers
            {
                IsCtrl     = KeyInterceptor.IsPressed(Keys.ControlKey),
                IsShift    = (KeyInterceptor.IsPressed(Keys.LShiftKey) || KeyInterceptor.IsPressed(Keys.RShiftKey)),
                IsAlt      = KeyInterceptor.IsPressed(Keys.Menu),
                IsCapsLock = Control.IsKeyLocked(Keys.CapsLock)
            };
        }

        public delegate void KeyDownHandler(Keys key, int repeatCount, ref bool handled);

        public List<int> KeysToIntercept = new List<int>();

        public new void Install()
        {
            base.Install(HookType.WH_KEYBOARD);
        }

        public event KeyDownHandler KeyDown;

        public void Add(params Keys[] keys)
        {
            foreach (int key in keys)
                if (!KeysToIntercept.Contains(key))
                    KeysToIntercept.Add(key);
        }

        public void RemoveAll()
        {
            KeysToIntercept.Clear();
        }

        public const int KF_UP = 0x8000;
        public const long KB_TRANSITION_FLAG = 0x80000000;

        override protected bool HandleHookEvent(IntPtr wParam, IntPtr lParam)
        {
            int key = (int)wParam;
            int context = (int)lParam;

            if (KeysToIntercept.Contains(key))
            {
                bool down = ((context & KB_TRANSITION_FLAG) != KB_TRANSITION_FLAG);
                int repeatCount = (context & 0xFF00);
                if (down && KeyDown != null)
                {
                    bool handled = false;
                    KeyDown((Keys)key, repeatCount, ref handled);
                    return handled;
                }
            }
            return false;
        }
    }
}