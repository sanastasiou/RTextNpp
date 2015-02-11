using RGiesecke.DllExport;
using System;
using System.Runtime.InteropServices;
using RTextNppPlugin.Utilities;

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
                    break;
                case (uint)NppMsg.NPPN_BUFFERACTIVATED:
                    Plugin.OnFileOpened();
                    Logging.Logger.Instance.Append("File {0} is modification status : {1}", FileUtilities.GetCurrentFilePath(), FileUtilities.IsFileModified(FileUtilities.GetCurrentFilePath()));
                    break;
                case (uint)SciMsg.SCN_SAVEPOINTLEFT:
                    Plugin.OnFileConsideredModified();
                    Logging.Logger.Instance.Append("Catching SCN_SAVEPOINTLEFT for file : {0}", FileUtilities.GetCurrentFilePath());
                    break;
                case (uint)SciMsg.SCN_SAVEPOINTREACHED:
                    Plugin.OnFileConsideredUnmodified();
                    Logging.Logger.Instance.Append("Catching SCN_SAVEPOINTREACHED for file : {0}", FileUtilities.GetCurrentFilePath());
                    break;
                case (uint)SciMsg.SCN_CHARADDED:
                    Plugin.OnCharTyped((char)nc.ch);
                    break;
                
            }
        }
    }
}
