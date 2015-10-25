using System;
using System.Runtime.InteropServices;
using System.Text;
using RGiesecke.DllExport;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
namespace RTextNppPlugin
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        static void GetLexerName(uint index, StringBuilder name, int bufLength)
        {
            name.Append("RTextNpp");
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        static void GetLexerStatusText(uint index, [MarshalAs(UnmanagedType.LPWStr, SizeConst = 32)]StringBuilder desc, int bufLength)
        {
            desc.Append("RText file.");
        }
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        static int GetLexerCount()
        {
            return 1;
        }
        static RTextLexerCliWrapper _lexerWrapper = new RTextLexerCliWrapper();
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        static IntPtr GetLexerFactory(uint index)
        {
            return (index == 0) ? _lexerWrapper.GetLexerFactory() : IntPtr.Zero;
        }
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
                _ptrPluginName = Marshal.StringToHGlobalUni(RTextNppPlugin.Constants.PluginName);
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
                    Plugin.OnZoomLevelModified();
                    Plugin.OnBufferActivated();
                    Logging.Logger.Instance.Append("File {0} is modification status : {1}", Npp.Instance.GetCurrentFilePath(), Npp.Instance.IsFileModified(Npp.Instance.GetCurrentFilePath()));
                    break;
                case (uint)SciMsg.SCN_SAVEPOINTLEFT:
                    Plugin.OnFileConsideredModified();
                    Logging.Logger.Instance.Append("Catching SCN_SAVEPOINTLEFT for file : {0}", Npp.Instance.GetCurrentFilePath());
                    break;
                case (uint)SciMsg.SCN_SAVEPOINTREACHED:
                    Plugin.OnFileConsideredUnmodified();
                    Logging.Logger.Instance.Append("Catching SCN_SAVEPOINTREACHED for file : {0}", Npp.Instance.GetCurrentFilePath());
                    break;
                case (uint)SciMsg.SCN_ZOOM:
                    Plugin.OnZoomLevelModified();
                    break;
            }
        }
    }
}