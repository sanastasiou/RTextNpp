using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Timers;
using RTextNppPlugin;
using System.Reflection;
using System.Diagnostics;

namespace RTextNppPlugin
{
    partial class Plugin
    {
        #region " Fields "
        private static Utilities.NppControlHost<ConsoleOutputForm> _consoleOutput = null;
        private static Automate.ConnectorManager _connectorManager = Automate.ConnectorManager.Instance;

        public const string PluginName = "RTextNpp";
        static bool doCloseTag = false;
        static Bitmap tbBmp = Properties.Resources.ConsoleIcon;
        static Bitmap tbBmp_tbTab = Properties.Resources.ConsoleIcon;
        static Icon tbIcon = null;
        #endregion

        #region " Startup/CleanUp "

        static internal void CommandMenuInit()
        {
            SetCommand((int)Constants.NppMenuCommands.ConsoleWindow, "Show RText++ Console", ShowConsoleOutput, new ShortcutKey(false, true, true, Keys.R));

            _connectorManager.initialize(nppData);
        }
        static internal void SetToolBarIcon()
        {
            toolbarIcons tbIcons = new toolbarIcons();
            tbIcons.hToolbarBmp = tbBmp.GetHbitmap();
            IntPtr pTbIcons = Marshal.AllocHGlobal(Marshal.SizeOf(tbIcons));
            Marshal.StructureToPtr(tbIcons, pTbIcons, false);
            Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_ADDTOOLBARICON, _funcItems.Items[(int)Constants.NppMenuCommands.ConsoleWindow]._cmdID, pTbIcons);
            Marshal.FreeHGlobal(pTbIcons);
        }

        static internal void PluginCleanUp()
        {
            //Win32.WritePrivateProfileString(sectionName, keyName, doCloseTag ? "1" : "0", iniFilePath);
        }
        #endregion

        #region " Menu functions "
        static void hello()
        {
            // Open a new document
            Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
            // Say hello now :
            // Scintilla control has no Unicode mode, so we use ANSI here (marshalled as ANSI by default)
            Win32.SendMessage(GetCurrentScintilla(), SciMsg.SCI_SETTEXT, 0, "Hello, Notepad++... from .NET!");
        }
        static void helloFX()
        {
            hello();
            new Thread(callbackHelloFX).Start();
        }
        static void callbackHelloFX()
        {
            IntPtr curScintilla = GetCurrentScintilla();
            int currentZoomLevel = (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETZOOM, 0, 0);
            int i = currentZoomLevel;
            for (int j = 0; j < 4; j++)
            {
                for (; i >= -10; i--)
                {
                    Win32.SendMessage(curScintilla, SciMsg.SCI_SETZOOM, i, 0);
                    Thread.Sleep(30);
                }
                Thread.Sleep(100);
                for (; i <= 20; i++)
                {
                    Thread.Sleep(30);
                    Win32.SendMessage(curScintilla, SciMsg.SCI_SETZOOM, i, 0);
                }
                Thread.Sleep(100);
            }
            for (; i >= currentZoomLevel; i--)
            {
                Thread.Sleep(30);
                Win32.SendMessage(curScintilla, SciMsg.SCI_SETZOOM, i, 0);
            }
        }
        static void WhatIsNpp()
        {
            string text2display = "Notepad++ is a free (as in \"free speech\" and also as in \"free beer\") " +
                "source code editor and Notepad replacement that supports several languages.\n" +
                "Running in the MS Windows environment, its use is governed by GPL License.\n\n" +
                "Based on a powerful editing component Scintilla, Notepad++ is written in C++ and " +
                "uses pure Win32 API and STL which ensures a higher execution speed and smaller program size.\n" +
                "By optimizing as many routines as possible without losing user friendliness, Notepad++ is trying " +
                "to reduce the world carbon dioxide emissions. When using less CPU power, the PC can throttle down " +
                "and reduce power consumption, resulting in a greener environment.";
            new Thread(new ParameterizedThreadStart(callbackWhatIsNpp)).Start(text2display);
        }
        static void callbackWhatIsNpp(object data)
        {
            string text2display = (string)data;
            // Open a new document
            Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);

            // Get the current scintilla
            IntPtr curScintilla = GetCurrentScintilla();

            Random srand = new Random(DateTime.Now.Millisecond);
            int rangeMin = 0;
            int rangeMax = 250;
            for (int i = 0; i < text2display.Length; i++)
            {
                StringBuilder charToShow = new StringBuilder(text2display[i].ToString());

                int ranNum = srand.Next(rangeMin, rangeMax);
                Thread.Sleep(ranNum + 30);

                Win32.SendMessage(curScintilla, SciMsg.SCI_APPENDTEXT, 1, charToShow);
                Win32.SendMessage(curScintilla, SciMsg.SCI_GOTOPOS, (int)Win32.SendMessage(curScintilla, SciMsg.SCI_GETLENGTH, 0, 0), 0);
            }
        }

        static void insertCurrentFullPath()
        {
            insertCurrentPath(NppMsg.FULL_CURRENT_PATH);
        }
        static void insertCurrentFileName()
        {
            insertCurrentPath(NppMsg.FILE_NAME);
        }
        static void insertCurrentDirectory()
        {
            insertCurrentPath(NppMsg.CURRENT_DIRECTORY);
        }
        static void insertCurrentPath(NppMsg which)
        {
            NppMsg msg = NppMsg.NPPM_GETFULLCURRENTPATH;
            if (which == NppMsg.FILE_NAME)
                msg = NppMsg.NPPM_GETFILENAME;
            else if (which == NppMsg.CURRENT_DIRECTORY)
                msg = NppMsg.NPPM_GETCURRENTDIRECTORY;

            StringBuilder path = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(nppData._nppHandle, msg, 0, path);

            Win32.SendMessage(GetCurrentScintilla(), SciMsg.SCI_REPLACESEL, 0, path);
        }

        static void insertShortDateTime()
        {
            insertDateTime(false);
        }
        static void insertLongDateTime()
        {
            insertDateTime(true);
        }
        static void insertDateTime(bool longFormat)
        {
            string dateTime = string.Format("{0} {1}",
                DateTime.Now.ToShortTimeString(),
                longFormat ? DateTime.Now.ToLongDateString() : DateTime.Now.ToShortDateString());
            Win32.SendMessage(GetCurrentScintilla(), SciMsg.SCI_REPLACESEL, 0, dateTime);
        }

        static void checkInsertHtmlCloseTag()
        {
            doCloseTag = !doCloseTag;

            int i = Win32.CheckMenuItem(Win32.GetMenu(nppData._nppHandle), _funcItems.Items[9]._cmdID,
                Win32.MF_BYCOMMAND | (doCloseTag ? Win32.MF_CHECKED : Win32.MF_UNCHECKED));
        }
        static internal void doInsertHtmlCloseTag(char newChar)
        {
            LangType docType = LangType.L_TEXT;
            Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_GETCURRENTLANGTYPE, 0, ref docType);
            bool isDocTypeHTML = (docType == LangType.L_HTML || docType == LangType.L_XML || docType == LangType.L_PHP);
            if (doCloseTag && isDocTypeHTML)
            {
                if (newChar == '>')
                {
                    int bufCapacity = 512;
                    IntPtr hCurrentEditView = GetCurrentScintilla();
                    int currentPos = (int)Win32.SendMessage(hCurrentEditView, SciMsg.SCI_GETCURRENTPOS, 0, 0);
                    int beginPos = currentPos - (bufCapacity - 1);
                    int startPos = (beginPos > 0) ? beginPos : 0;
                    int size = currentPos - startPos;

                    if (size >= 3)
                    {
                        using (Sci_TextRange tr = new Sci_TextRange(startPos, currentPos, bufCapacity))
                        {
                            Win32.SendMessage(hCurrentEditView, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                            string buf = tr.lpstrText;

                            if (buf[size - 2] != '/')
                            {
                                StringBuilder insertString = new StringBuilder("</");

                                int pCur = size - 2;
                                for (; (pCur > 0) && (buf[pCur] != '<') && (buf[pCur] != '>'); )
                                    pCur--;

                                if (buf[pCur] == '<')
                                {
                                    pCur++;

                                    Regex regex = new Regex(@"[\._\-:\w]");
                                    while (regex.IsMatch(buf[pCur].ToString()))
                                    {
                                        insertString.Append(buf[pCur]);
                                        pCur++;
                                    }
                                    insertString.Append('>');

                                    if (insertString.Length > 3)
                                    {
                                        Win32.SendMessage(hCurrentEditView, SciMsg.SCI_BEGINUNDOACTION, 0, 0);
                                        Win32.SendMessage(hCurrentEditView, SciMsg.SCI_REPLACESEL, 0, insertString);
                                        Win32.SendMessage(hCurrentEditView, SciMsg.SCI_SETSEL, currentPos, currentPos);
                                        Win32.SendMessage(hCurrentEditView, SciMsg.SCI_ENDUNDOACTION, 0, 0);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        static void getFileNamesDemo()
        {
            int nbFile = (int)Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, 0);
            MessageBox.Show(nbFile.ToString(), "Number of opened files:");

            using (ClikeStringArray cStrArray = new ClikeStringArray(nbFile, Win32.MAX_PATH))
            {
                if (Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMES, cStrArray.NativePointer, nbFile) != IntPtr.Zero)
                    foreach (string file in cStrArray.ManagedStringsUnicode) MessageBox.Show(file);
            }
        }        

        static void ShowConsoleOutput()
        {
            if (_consoleOutput == null)
            {
                _consoleOutput = new Utilities.NppControlHost<ConsoleOutputForm>(Constants.CONSOLE_OUTPUT_SETTING_KEY);
                _consoleOutput.SetNppHandle(nppData._nppHandle);

                using (Bitmap newBmp = new Bitmap(16, 16))
                {
                    Graphics g = Graphics.FromImage(newBmp);
                    ColorMap[] colorMap = new ColorMap[1];
                    colorMap[0] = new ColorMap();
                    colorMap[0].OldColor = Color.Fuchsia;
                    colorMap[0].NewColor = Color.FromKnownColor(KnownColor.ButtonFace);
                    ImageAttributes attr = new ImageAttributes();
                    attr.SetRemapTable(colorMap);
                    g.DrawImage(tbBmp_tbTab, new Rectangle(0, 0, 16, 16), 0, 0, 16, 16, GraphicsUnit.Pixel, attr);
                    tbIcon = Icon.FromHandle(newBmp.GetHicon());
                }

                NppTbData _nppTbData = new NppTbData();
                _nppTbData.hClient = _consoleOutput.Handle;
                _nppTbData.pszName = "RText++ Console Output";
                _nppTbData.dlgID = 1;// (int)Constants.NppMenuCommands.ConsoleWindow;
                // define the default docking behaviour
                _nppTbData.uMask = NppTbMsg.DWS_DF_CONT_RIGHT | NppTbMsg.DWS_ICONTAB | NppTbMsg.DWS_ICONBAR;
                _nppTbData.hIconTab = (uint)tbIcon.Handle;
                _nppTbData.pszModuleName = PluginName;
                IntPtr _ptrNppTbData = Marshal.AllocHGlobal(Marshal.SizeOf(_nppTbData));
                Marshal.StructureToPtr(_nppTbData, _ptrNppTbData, false);
                _consoleOutput.CmdId = _funcItems.Items[(int)Constants.NppMenuCommands.ConsoleWindow]._cmdID;
                Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_DMMREGASDCKDLG, 0, _ptrNppTbData);
                Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK, _funcItems.Items[0]._cmdID, 1);
            }
            else
            {
                if (!_consoleOutput.Visible)
                {
                    Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_DMMSHOW, 0, _consoleOutput.Handle);
                }
                else
                {
                    Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_DMMHIDE, 0, _consoleOutput.Handle);
                }
            }
            _consoleOutput.Focus();
        }

        #endregion
        static internal void LoadSettings()
        {
            //Assembly assembly = Assembly.GetExecutingAssembly();
            //FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            //string version = fvi.FileVersion;
            //Logging.Logger.Instance.Append(Plugin.PluginName + "version : " + version, Logging.Logger.MessageType.Info);
            //Logging.Logger.Instance.Append("Loading user settings...", Logging.Logger.MessageType.Info);
            bool wasConsoleOpen = false;
            Utilities.ConfigurationSetter.readSetting(ref wasConsoleOpen, Constants.CONSOLE_OUTPUT_SETTING_KEY);
            if (wasConsoleOpen)
            {
                ShowConsoleOutput();
                Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_SETMENUITEMCHECK, _funcItems.Items[0]._cmdID, 1);
            }
            //Logging.Logger.Instance.Append("User settings loaded.", Logging.Logger.MessageType.Info);
        }

        #region Event Handlers

        public static void OnFileOpened()
        {
            //get active file
            NppMsg msg = NppMsg.NPPM_GETFULLCURRENTPATH;
            StringBuilder path = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(nppData._nppHandle, msg, 0, path);
            Logging.Logger.Instance.Append(Logging.Logger.MessageType.Info, Constants.GENERAL_CHANNEL, String.Format("Buffer activated for file : {0}\n", path.ToString()));
            _connectorManager.createConnector(path.ToString());
        }

        /**
         * \brief   Pre load workspace(s).
         * \todo    Only do this if corresponding options is set.         
         */
        public static void PreLoadWorkspace()
        {
            int nbFile = (int)Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, 0);

            if (nbFile <= 0)
            {
                return;
            }
            else
            {
                using (ClikeStringArray cStrArray = new ClikeStringArray(nbFile, Win32.MAX_PATH))
                {
                    if (Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMES, cStrArray.NativePointer, nbFile) != IntPtr.Zero)
                    {
                        foreach (string file in cStrArray.ManagedStringsUnicode)
                        {
                            _connectorManager.createConnector(file);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
