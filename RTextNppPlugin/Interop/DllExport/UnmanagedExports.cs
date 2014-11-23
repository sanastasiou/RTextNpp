using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using RGiesecke.DllExport;
using System.Reflection;
using System.Text.RegularExpressions;

namespace NppPluginNET
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static bool isUnicode()
        {
            return true;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void setInfo(NppData notepadPlusData)
        {
            Plugin.NppData = notepadPlusData;
            InitPlugin();
        }

        /**
         * Initialises the plugin. All initialization should take place here.
         */
        static void InitPlugin()
        {
            Plugin.CommandMenuInit(); //this will also call NppPluginNET.Plugin.CommandMenuInit

            //foreach (var item in NppPluginNET.Plugin.FuncItems.Items)
            //    Plugin.FuncItems.Add(item.ToLocal());

            //NppPluginNET.Plugin.FuncItems.Items.Clear();
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = Plugin.FuncItems.Items.Count;
            return Plugin.FuncItems.NativePointer;
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

        const int _SC_MARGE_SYBOLE = 1; //bookmark and breakpoint margin
        const int SCI_CTRL = 2; //Ctrl pressed modifier for SCN_MARGINCLICK

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            try
            {
                //NppPluginNET.Interop.NppUI.OnNppTick();

                SCNotification nc = (SCNotification)Marshal.PtrToStructure(notifyCode, typeof(SCNotification));
                if (nc.nmhdr.code == (uint)NppMsg.NPPN_READY)
                {
                    NppPluginNET.Plugin.OnNppReady();
                    NppPluginNET.Plugin.OnNppReady();
                    Npp.SetCalltipTime(200);
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_TBMODIFICATION)
                {
                    NppPluginNET.Plugin.OnToolbarUpdate();
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_CHARADDED)
                {
                    NppPluginNET.Plugin.OnCharTyped((char)nc.ch);
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_MARGINCLICK)
                {
                    if (nc.margin == _SC_MARGE_SYBOLE && nc.modifiers == SCI_CTRL)
                    {
                        int lineClick = Npp.GetLineFromPosition(nc.position);
                        //Debugger.ToggleBreakpoint(lineClick);
                    }
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_DWELLSTART) //tooltip
                {
                    //Npp.ShowCalltip(nc.position, "\u0001  1 of 3 \u0002  test tooltip " + Environment.TickCount);
                    //Npp.ShowCalltip(nc.position, NppPluginNET.Npp.GetWordAtPosition(nc.position));
                    //                    tooltip = @"Creates all directories and subdirectories as specified by path.

                    //Npp.OnCalltipRequest(nc.position);
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_DWELLEND)
                {
                    Npp.CancelCalltip();
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_BUFFERACTIVATED)
                {
                    string file = Npp.GetCurrentFile();

                    if (file.EndsWith("npp.args"))
                    {
                        Win32.SendMessage(Npp.NppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_CLOSE);

                        string args = File.ReadAllText(file);

                        Plugin.ProcessCommandArgs(args);

                        try { File.Delete(file); }
                        catch { }
                    }
                    else
                    {
                        NppPluginNET.Plugin.OnCurrentFileChanged();
                    }
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_FILEOPENED)
                {
                    string file = Npp.GetTabFile((int)nc.nmhdr.idFrom);
                    //Debugger.LoadBreakPointsFor(file);
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_FILESAVED || nc.nmhdr.code == (uint)NppMsg.NPPN_FILEBEFORECLOSE)
                {
                    string file = Npp.GetTabFile((int)nc.nmhdr.idFrom);
                    //Debugger.RefreshBreakPointsFromContent();
                    //Debugger.SaveBreakPointsFor(file);

                    if (nc.nmhdr.code == (uint)NppMsg.NPPN_FILESAVED)
                        Plugin.OnDocumentSaved();
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_SHUTDOWN)
                {
                    Marshal.FreeHGlobal(_ptrPluginName);

                    Plugin.CleanUp();
                }

                Plugin.OnNotification(nc);
            }
            catch { }//this is indeed the last line of defense as all CS-S calls have the error handling inside
        }
    }
}