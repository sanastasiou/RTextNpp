using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;

namespace RTextNppPlugin
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention=CallingConvention.Cdecl)]
        static bool isUnicode()
        {
            return true;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void setInfo(NppData notepadPlusData)
        {
            Plugin.nppData = notepadPlusData;
            Plugin.CommandMenuInit();
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = Plugin._funcItems.Items.Count;
            return Plugin._funcItems.NativePointer;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static uint messageProc(uint Message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        static IntPtr _ptrPluginName = IntPtr.Zero;
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getName()
        {
            if (_ptrPluginName == IntPtr.Zero)
                _ptrPluginName = Marshal.StringToHGlobalUni(Plugin.PluginName);
            return _ptrPluginName;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            SCNotification nc = (SCNotification)Marshal.PtrToStructure(notifyCode, typeof(SCNotification));
            switch (nc.nmhdr.code)
            {
                case (uint)NppMsg.NPPN_TBMODIFICATION:
                    Plugin._funcItems.RefreshItems();
                    Plugin.SetToolBarIcon();
                    break;
                case (uint)NppMsg.NPPN_SHUTDOWN:
                    Plugin.PluginCleanUp();
                    Marshal.FreeHGlobal(_ptrPluginName);
                    break;
                case (uint)NppMsg.NPPN_READY:
                    Plugin.LoadSettings();
                    Plugin.PrestartBackend();
                    break;
                case (uint)NppMsg.NPPN_BUFFERACTIVATED:
                    Plugin.OnFileOpened();
                    break;
            }
        }
    }
}
