// ***********************************************************************
// Taken as is from CSScriptNpp
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using RTextNppPlugin.Utilities;
namespace CSScriptIntellisense
{
    internal class WinHook<T> : LocalWindowsHook, IDisposable where T : new()
    {
        static T instance;
        internal static T Instance
        {
            get
            {
                if (instance == null)
                    instance = new T();
                return instance;
            }
        }
        protected WinHook()
            : base(VisualUtilities.HookType.WH_DEBUG)
        {
            m_filterFunc = Proc;
        }
        ~WinHook()
        {
            Dispose(false);
        }
        internal void Dispose(bool disposing)
        {
            if (IsInstalled)
                Uninstall();
            if (disposing)
                GC.SuppressFinalize(this);
        }
        protected void Install(VisualUtilities.HookType type)
        {
            base.m_hookType = type;
            base.Install();
        }
        public void Dispose()
        {
            Dispose(true);
        }
        protected int Proc(int code, UIntPtr wParam, IntPtr lParam)
        {
            if (code == 0) //Win32.HC_ACTION
            {
                if (HandleHookEvent(wParam, lParam))
                {
                    return 1;
                }
            }
            return base.ICallNextHookEx(m_hhook, code, wParam, lParam);
        }
        virtual protected bool HandleHookEvent(UIntPtr wParam, IntPtr lParam)
        {
            throw new NotSupportedException();
        }
    }
    internal class MouseMonitor : WinHook<MouseMonitor>
    {
        internal event Action MouseMove;
        internal event Func<VisualUtilities.MouseMessages, bool> MouseClicked;
        internal event Action MouseReleased;
        internal event Func<int, bool> MouseWheelMoved;
        override protected bool HandleHookEvent(UIntPtr wParam, IntPtr lParam)
        {
            VisualUtilities.MouseMessages aMsg = (VisualUtilities.MouseMessages)wParam.ToUInt32();
            switch (aMsg)
            {
                case VisualUtilities.MouseMessages.WM_LBUTTONDOWN:
                case VisualUtilities.MouseMessages.WM_RBUTTONDOWN:
                case VisualUtilities.MouseMessages.WM_NCXBUTTONDBLCLK:
                case VisualUtilities.MouseMessages.WM_NCXBUTTONDOWN:
                case VisualUtilities.MouseMessages.WM_NCMBUTTONDBLCLK:
                case VisualUtilities.MouseMessages.WM_NCMBUTTONDOWN:
                case VisualUtilities.MouseMessages.WM_NCRBUTTONDBLCLK:
                case VisualUtilities.MouseMessages.WM_NCRBUTTONDOWN:
                case VisualUtilities.MouseMessages.WM_NCLBUTTONDBLCLK:
                case VisualUtilities.MouseMessages.WM_NCLBUTTONDOWN:
                    if (MouseClicked != null)
                    {
                        return MouseClicked(aMsg);
                    }
                    break;
                case VisualUtilities.MouseMessages.WM_LBUTTONUP:
                case VisualUtilities.MouseMessages.WM_RBUTTONUP:
                    if (MouseReleased != null)
                    {
                        MouseReleased();
                    }
                    break;
                case VisualUtilities.MouseMessages.WM_MOUSEMOVE:
                case VisualUtilities.MouseMessages.WM_NCMOUSEMOVE:
                    if (MouseMove != null)
                    {
                        MouseMove();
                    }
                    break;
                case VisualUtilities.MouseMessages.WM_MOUSEWHEEL:
                    MouseHookStructEx aMouseData = (MouseHookStructEx)Marshal.PtrToStructure(lParam, typeof(MouseHookStructEx));
                    var wheelMovement = GetWheelDeltaWParam(aMouseData.MouseData);
                    if (MouseWheelMoved != null)
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
        internal new void Install()
        {
            base.Install(VisualUtilities.HookType.WH_MOUSE);
        }
        private int GetWheelDeltaWParam(int mouseData) { return (short)(mouseData >> 16); }
    }
    internal struct Modifiers
    {
        internal bool IsCtrl;
        internal bool IsShift;
        internal bool IsAlt;
        internal bool IsCapsLock;
        internal bool IsInsert;
    }
    internal class KeyInterceptor : WinHook<KeyInterceptor>
    {
        [DllImport("USER32.dll")]
        static extern short GetKeyState(int nVirtKey);
        internal static bool IsPressed(Keys key)
        {
            const int KEY_PRESSED = 0x8000;
            return Convert.ToBoolean(GetKeyState((int)key) & KEY_PRESSED);
        }
        internal static Modifiers GetModifiers()
        {
            return new Modifiers
            {
                IsCtrl     = KeyInterceptor.IsPressed(Keys.ControlKey),
                IsShift    = (KeyInterceptor.IsPressed(Keys.LShiftKey) || KeyInterceptor.IsPressed(Keys.RShiftKey)),
                IsAlt      = KeyInterceptor.IsPressed(Keys.Menu),
                IsCapsLock = Control.IsKeyLocked(Keys.CapsLock),
                IsInsert   = Control.IsKeyLocked(Keys.Insert)
            };
        }
        internal delegate void KeyDownHandler(Keys key, int repeatCount, ref bool handled);
        internal List<int> KeysToIntercept = new List<int>();
        internal new void Install()
        {
            base.Install(VisualUtilities.HookType.WH_KEYBOARD);
        }
        internal event KeyDownHandler KeyDown;
        internal event KeyDownHandler KeyUp;
        internal void Add(params Keys[] keys)
        {
            foreach (int key in keys)
                if (!KeysToIntercept.Contains(key))
                    KeysToIntercept.Add(key);
        }
        internal void RemoveAll()
        {
            KeysToIntercept.Clear();
        }
        private const long KB_TRANSITION_FLAG = 0x80000000;
        override protected bool HandleHookEvent(UIntPtr wParam, IntPtr lParam)
        {
            int key     = (int)wParam;
            uint context = (uint)lParam;
            if (KeysToIntercept.Contains(key))
            {
                bool down = ((context & KB_TRANSITION_FLAG) == 0);
                bool up   = ((context & KB_TRANSITION_FLAG) == KB_TRANSITION_FLAG);
                int repeatCount = (int)(context & 0xFF00);
                if (down && KeyDown != null)
                {
                    bool handled = false;
                    KeyDown((Keys)key, repeatCount, ref handled);
                    return handled;
                }
                if (up && KeyUp != null)
                {
                    bool handled = false;
                    KeyUp((Keys)key, repeatCount, ref handled);
                    return handled;
                }
            }
            return false;
        }
    }
}