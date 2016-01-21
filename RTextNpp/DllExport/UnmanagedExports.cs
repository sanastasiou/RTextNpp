using System;
using System.Runtime.InteropServices;
using System.Text;
using RGiesecke.DllExport;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.Utilities;
using System.Diagnostics;
using System.Drawing;
namespace RTextNppPlugin
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention = CallingConvention.StdCall)]
        static void GetLexerName(uint index, StringBuilder name, int bufLength)
        {
            name.Append(Constants.Scintilla.PLUGIN_NAME);
        }

        [DllExport(CallingConvention = CallingConvention.StdCall)]
        static void GetLexerStatusText(uint index, [MarshalAs(UnmanagedType.LPWStr, SizeConst = 32)]StringBuilder desc, int bufLength)
        {
            desc.Append(Constants.Scintilla.RTEXT_FILE_DESCRIPTION);
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
            Plugin.Instance.NppData = notepadPlusData;
            Plugin.Instance.CommandMenuInit();
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = Plugin.Instance.FuncItems.Items.Count;
            return Plugin.Instance.FuncItems.NativePointer;
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
            {
                _ptrPluginName = Marshal.StringToHGlobalUni(RTextNppPlugin.Constants.Scintilla.PLUGIN_NAME);
            }
            return _ptrPluginName;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            SCNotification nc = (SCNotification)Marshal.PtrToStructure(notifyCode, typeof(SCNotification));

            switch (nc.nmhdr.code)
            {
                case (uint)NppMsg.NPPN_TBMODIFICATION:
                    Plugin.Instance.FuncItems.RefreshItems();
                    Plugin.Instance.SetToolBarIcon();
                    break;
                case (uint)NppMsg.NPPN_SHUTDOWN:
                    Plugin.Instance.PluginCleanUp();
                    Marshal.FreeHGlobal(_ptrPluginName);
                    break;
                case (uint)NppMsg.NPPN_READY:
                    Plugin.Instance.LoadSettings();
                    break;
                case (uint)NppMsg.NPPN_BUFFERACTIVATED:
                    //force zoom update
                    Plugin.Instance.OnZoomLevelModified();
                    Plugin.Instance.OnBufferActivated(nc.nmhdr.hwndFrom, (int)nc.nmhdr.idFrom);
                    break;
                case (uint)SciMsg.SCN_SAVEPOINTLEFT:
                    Plugin.Instance.OnFileConsideredModified();
                    break;
                case (uint)SciMsg.SCN_SAVEPOINTREACHED:
                    Plugin.Instance.OnFileConsideredUnmodified();
                    break;
                case (uint)SciMsg.SCN_ZOOM:
                    Plugin.Instance.OnZoomLevelModified();
                    break;
                case (uint)NppMsg.NPPN_FILEBEFORECLOSE:
                    Plugin.Instance.OnPreviewFileClosed();
                    break;
                case (uint)SciMsg.SCN_HOTSPOTRELEASECLICK:
                    Plugin.Instance.OnHotspotClicked();
                    break;
                case (uint)NppMsg.NPPN_FILESAVED:
                    Plugin.Instance.OnFileSaved();
                    break;
                case (uint)SciMsg.SCN_PAINTED:
                    Plugin.Instance.OnScnPainted();
                    break;
                case (uint)SciMsg.SCN_UPDATEUI:
                    Plugin.Instance.OnScnUpdateUi(nc);
                    break;
                case (uint)SciMsg.SCN_MODIFIED:
                    //Plugin.Instance.OnScnModified(nc);
                    break;
                case(uint)NppMsg.NPPN_BEFORESHUTDOWN:
                    Plugin.Instance.BeforeShutdown();
                    break;
                case(uint)SciMsg.SCN_DWELLEND:
                    Plugin.Instance.OnDwellEnd(nc.nmhdr.hwndFrom, nc.position, new Point(nc.x, nc.y));
                    break;
                case(uint)SciMsg.SCN_DWELLSTART:
                    Plugin.Instance.OnDwellStart(nc.nmhdr.hwndFrom, nc.position, new Point(nc.x, nc.y));
                    break;
            }
        }

        private static bool IsBitSet(int bitNumber, int value)
        {
            int aBitMask = 1;
            aBitMask <<= bitNumber;
            return (value & aBitMask) != 0;
        }
    }
}