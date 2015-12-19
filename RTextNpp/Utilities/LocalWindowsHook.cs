// ***********************************************************************
//  LocalWindowsHook class
//  Dino Esposito, summer 2002
//
//  Provide a general infrastructure for using Win32
//  hooks in .NET applications
//
// ***********************************************************************
#pragma warning disable 618
//
// I took this class from the example at http://msdn.microsoft.com/msdnmag/issues/02/10/cuttingedge
// and made a couple of minor tweaks to it - dpk
//
using System;
using System.Runtime.InteropServices;
using System.Reflection;
using RTextNppPlugin.Utilities;
using RTextNppPlugin;
using RTextNppPlugin.DllExport;
namespace CSScriptIntellisense
{
    #region [Mouse Wheel Helper]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
        public POINT(int x, int y)
        {
            X = x;
            Y = y;
        }
        public POINT(System.Drawing.Point pt) : this(pt.X, pt.Y) { }
        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }
        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public class MouseHookStruct
    {
        public POINT pt;
        public IntPtr hwnd;
        public uint wHitTestCode;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    struct MouseHookStructEx
    {
        public MouseHookStruct mouseHookStruct;
        public int MouseData;
    }
    [StructLayout(LayoutKind.Sequential)]
    public class MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }
    #endregion
    
    #region Class HookEventArgs
    public class HookEventArgs : EventArgs
    {
        public int HookCode;    // Hook code
        public UIntPtr wParam;  // WPARAM argument
        public IntPtr lParam;   // LPARAM argument
    }
    #endregion
    
    [StructLayout(LayoutKind.Sequential)]
    struct MouseLLHookStruct
    {
        public POINT Point;
        /// <summary>
        /// If the message is WM_MOUSEWHEEL, the high-order word of this member is the wheel delta.
        /// The low-order word is reserved. A positive value indicates that the wheel was rotated forward,
        /// away from the user; a negative value indicates that the wheel was rotated backward, toward the user.
        /// One wheel click is defined as WHEEL_DELTA, which is 120.
        ///If the message is WM_XBUTTONDOWN, WM_XBUTTONUP, WM_XBUTTONDBLCLK, WM_NCXBUTTONDOWN, WM_NCXBUTTONUP,
        /// or WM_NCXBUTTONDBLCLK, the high-order word specifies which X button was pressed or released,
        /// and the low-order word is reserved. This value can be one or more of the following values. Otherwise, MouseData is not used.
        ///XBUTTON1
        ///The first X button was pressed or released.
        ///XBUTTON2
        ///The second X button was pressed or released.
        /// </summary>
        public int MouseData;
        /// <summary>
        /// Specifies the event-injected flag. An application can use the following value to test the mouse Flags. Value Purpose
        ///LLMHF_INJECTED Test the event-injected flag.
        ///0
        ///Specifies whether the event was injected. The value is 1 if the event was injected; otherwise, it is 0.
        ///1-15
        ///Reserved.
        /// </summary>
        public int Flags;
        /// <summary>
        /// Specifies the Time stamp for this message.
        /// </summary>
        public int Time;
        /// <summary>
        /// Specifies extra information associated with the message.
        /// </summary>
        public int ExtraInfo;
    }
    
    #region Class LocalWindowsHook
    internal class LocalWindowsHook : NativeHelpers
    {
        // ************************************************************************
        // Internal properties
        protected IntPtr m_hhook                      = IntPtr.Zero;
        protected NativeHelpers.HookProc m_filterFunc = null;
        protected VisualUtilities.HookType m_hookType = default(VisualUtilities.HookType);
        // ************************************************************************
        // ************************************************************************
        // Event delegate
        public delegate void HookEventHandler(object sender, HookEventArgs e);
        // ************************************************************************
        // ************************************************************************
        // Event: HookInvoked
        public event HookEventHandler HookInvoked;
        protected void OnHookInvoked(HookEventArgs e)
        {
            if (HookInvoked != null)
                HookInvoked(this, e);
        }
        // ************************************************************************
        // ************************************************************************
        // Class constructor(s)
        protected LocalWindowsHook(VisualUtilities.HookType hook)
        {
            m_hookType = hook;
            m_filterFunc = new NativeHelpers.HookProc(CoreHookProc);
        }
        protected LocalWindowsHook(VisualUtilities.HookType hook, NativeHelpers.HookProc func)
        {
            m_hookType = hook;
            m_filterFunc = func;
        }
        // ************************************************************************
        // ************************************************************************
        // Default filter function
        protected int CoreHookProc(int code, UIntPtr wParam, IntPtr lParam)
        {
            if (code < 0)
            {
                return NativeHelpers.CallNextHookEx(m_hhook, code, wParam, lParam);
            }
            // Let clients determine what to do
            HookEventArgs e = new HookEventArgs();
            e.HookCode = code;
            e.wParam = wParam;
            e.lParam = lParam;
            OnHookInvoked(e);
            System.Diagnostics.Trace.WriteLine(String.Format("Hook called : code {0}", e.HookCode));
            // Yield to the next hook in the chain
            return NativeHelpers.CallNextHookEx(m_hhook, code, wParam, lParam);
        }
        // ************************************************************************
        // ************************************************************************
        // Install the hook
        public void Install()
        {
            if (!IsInstalled)
            {
                m_hhook = NativeHelpers.SetWindowsHookEx(
                    m_hookType,
                    m_filterFunc,
                    IntPtr.Zero,
                    (int)AppDomain.GetCurrentThreadId());
            }
        }
        // ************************************************************************
        // ************************************************************************
        // Uninstall the hook
        public void Uninstall()
        {
            if (IsInstalled)
            {
                NativeHelpers.UnhookWindowsHookEx(m_hhook);
                m_hhook = IntPtr.Zero;
            }
        }
        // ************************************************************************
        public bool IsInstalled
        {
            get{ return m_hhook != IntPtr.Zero; }
        }
    }
    #endregion
}