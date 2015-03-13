/**
 * \file    Utilities\Npp.cs
 *
 * Taken as is fron CSScript plugin for notepad++.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using RTextNppPlugin;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Parsing;

namespace CSScriptIntellisense
{
    public class Npp
    {
        public const int DocEnd = -1;

        static public void CancelNppAutoCompletion()
        {
            Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_AUTOCCANCEL, 0, 0);
        }

        static public int GetZoomLevel()
        {
            return (int)Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_GETZOOM, 0, 0);
        }

        static public int GetSelectionLength()
        {
            int aSelStart = (int)Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_GETSELECTIONNSTART, 0, 0);
            int aSelEnd = (int)Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_GETSELECTIONNEND, 0, 0);
            if (aSelStart == aSelEnd)
            {
                return 1;
            }
            return aSelEnd - aSelStart;
        }

        static public void DeleteFront()
        {
            int aSelStart = (int)Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_GETSELECTIONNSTART, 0, 0);
            int aSelEnd = (int)Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_GETSELECTIONNEND, 0, 0);
            int aMin = Math.Min(aSelStart, aSelEnd);

            int aSelectionLength = GetSelectionLength();

            if(aSelectionLength > 1)
            {
                DeleteBack(1);
            }
            else
            {
                SetCaretPosition(GetCaretPosition() + 1);
                DeleteBack(1);
            }
        }

        static public void DeleteBack(int length)
        {
            for (int i = 0; i < length; ++i)
            {
                Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_DELETEBACK, 0, 0);
            }
        }

        static public void AddText(string s)
        {
            Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_ADDTEXT, s.GetByteCount(), s);
        }

        static public string GetCurrentFile()
        {
            var path = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);
            return path.ToString();
        }
        static public void SaveCurrentFile()
        {
            Win32.SendMessage(Npp.NppHandle, NppMsg.NPPM_SAVECURRENTFILE, 0, 0);
        }

        static public void DisplayInNewDocument(string text)
        {
            Win32.SendMessage(Npp.NppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
            Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_GRABFOCUS, 0, 0);
            Win32.SendMessage(Npp.CurrentScintilla, SciMsg.SCI_ADDTEXT, text);
        }

        /// <summary>
        /// Determines whether the current file has the specified extension (e.g. ".cs").
        /// <para>Note it is case insensitive.</para>
        /// </summary>
        /// <param name="extension">The extension.</param>
        /// <returns></returns>
        static public bool IsCurrentFileHasExtension(string extension)
        {
            var path = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);
            string file = path.ToString();
            return !string.IsNullOrWhiteSpace(file) && file.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
        }

        static public void SetIndicatorStyle(int indicator, SciMsg style, Color color)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            Win32.SendMessage(sci, SciMsg.SCI_INDICSETSTYLE, indicator, (int)style);
            Win32.SendMessage(sci, SciMsg.SCI_INDICSETFORE, indicator, ColorTranslator.ToWin32(color));
        }

        static public void ClearIndicator(int indicator, int startPos, int endPos)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            Win32.SendMessage(sci, SciMsg.SCI_SETINDICATORCURRENT, indicator, 0);
            Win32.SendMessage(sci, SciMsg.SCI_INDICATORCLEARRANGE, startPos, endPos - startPos);
        }

        static public void PlaceIndicator(int indicator, int startPos, int endPos)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            Win32.SendMessage(sci, SciMsg.SCI_SETINDICATORCURRENT, indicator, 0);
            Win32.SendMessage(sci, SciMsg.SCI_INDICATORFILLRANGE, startPos, endPos - startPos);
        }

        static public string GetConfigDir()
        {
            var buffer = new StringBuilder(260);
            Win32.SendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, 260, buffer);
            return buffer.ToString();
        }

        static public Point[] FindIndicatorRanges(int indicator)
        {
            var ranges = new List<Point>();

            IntPtr sci = Plugin.GetCurrentScintilla();

            int testPosition = 0;

            while (true)
            {
                //finding the indicator ranges
                //For example indicator 4..6 in the doc 0..10 will have three logical regions:
                //0..4, 4..6, 6..10
                //Probing will produce following when outcome:
                //probe for 0 : 0..4
                //probe for 4 : 4..6
                //probe for 6 : 4..10

                int rangeStart = (int)Win32.SendMessage(sci, SciMsg.SCI_INDICATORSTART, indicator, testPosition);
                int rangeEnd = (int)Win32.SendMessage(sci, SciMsg.SCI_INDICATOREND, indicator, testPosition);
                int value = (int)Win32.SendMessage(sci, SciMsg.SCI_INDICATORVALUEAT, indicator, testPosition);
                if (value == 1) //indicator is present
                    ranges.Add(new Point(rangeStart, rangeEnd));

                if (testPosition == rangeEnd)
                    break;

                testPosition = rangeEnd;
            }

            return ranges.ToArray();
        }

        /**
         * Gets the line.
         *
         * \return  The current line from the caret position.
         */
        static public string GetLine()
        {
            return Npp.GetLine(Npp.GetLineNumber(Npp.GetCaretPosition()));
        }

        static public string GetLine(int line)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();

            int length = (int)Win32.SendMessage(sci, SciMsg.SCI_LINELENGTH, line, 0);
            var buffer = new StringBuilder(length + 1);
            Win32.SendMessage(sci, SciMsg.SCI_GETLINE, line, buffer);
            buffer.Length = length; //NPP may inject some rubbish at the end of the line
            return buffer.ToString();
        }

        static public StringBuilder GetLineAsStringBuilder(int line)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();

            int length = (int)Win32.SendMessage(sci, SciMsg.SCI_LINELENGTH, line, 0);
            var buffer = new StringBuilder(length + 1);
            Win32.SendMessage(sci, SciMsg.SCI_GETLINE, line, buffer);
            buffer.Length = length; //NPP may inject some rubbish at the end of the line
            return buffer;
        }

        /**
         * Gets line number.
         *
         * \return  The line number from the current caret position.
         */
        static public int GetLineNumber()
        {
            return GetLineNumber(Npp.GetCaretPosition());
        }

        static public int GetLineNumber(int position)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            return (int)Win32.SendMessage(sci, SciMsg.SCI_LINEFROMPOSITION, position, 0);
        }

        static public int GetLengthToEndOfLine(int currentCharacterColumn)
        {
            return CSScriptIntellisense.Npp.GetLine().RemoveNewLine().Length - currentCharacterColumn;
        }

        static public int GetColumn()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            return (int)Win32.SendMessage(sci, SciMsg.SCI_GETCOLUMN, GetCaretPosition(), 0);
        }

        static public int GetLineEnd(int line)
        {
            return (GetLineStart(line) + GetLine().Length);
        }

        static public int GetLineStart(int line)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            return (int)Win32.SendMessage(sci, SciMsg.SCI_POSITIONFROMLINE, line, 0);
        }

        static public int GetFirstVisibleLine()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            return (int)Win32.SendMessage(sci, SciMsg.SCI_GETFIRSTVISIBLELINE, 0, 0);
        }

        static public void SetFirstVisibleLine(int line)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            Win32.SendMessage(sci, SciMsg.SCI_SETFIRSTVISIBLELINE, line, 0);
        }

        static public int GetLineCount()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            return (int)Win32.SendMessage(sci, SciMsg.SCI_GETLINECOUNT, 0, 0);
        }

        static public string GetShortcutsFile()
        {
            return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(Npp.GetConfigDir())), "shortcuts.xml");
        }

        [DllImport("user32")]
        public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32")]
        public static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        public static extern long GetWindowRect(IntPtr hWnd, ref Rectangle lpRect);


        /**
         * Gets caret screen location relative to buffer position.
         *
         * \param   position    The buffer position.
         *
         * \return  A point from the relative buffer position.
         */
        public static Point GetCaretScreenLocationRelativeToPosition(int position)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            int x = (int)Win32.SendMessage(sci, SciMsg.SCI_POINTXFROMPOSITION, 0, position);
            int y = (int)Win32.SendMessage(sci, SciMsg.SCI_POINTYFROMPOSITION, 0, position);
            Point aPoint = new Point(x, y);
            ClientToScreen(sci, ref aPoint);
            aPoint.Y += CSScriptIntellisense.Npp.GetTextHeight(CSScriptIntellisense.Npp.GetCaretLineNumber());
            return aPoint;
        }

        /**
         * Gets caret screen location for form. ( under a certain buffer position )
         *
         * \return  The caret screen location for form.
         */
        static public Point GetCaretScreenLocationForForm(int position)
        {
            Point aPoint = GetCaretScreenLocationRelativeToPosition(position);
            aPoint.Y += CSScriptIntellisense.Npp.GetTextHeight(CSScriptIntellisense.Npp.GetCaretLineNumber());
            return aPoint;
        }

        /**
         * Gets caret screen location for form. ( under caret character )
         *
         * \return  The caret screen location for form.
         */
        static public Point GetCaretScreenLocationForForm()
        {
            Point aPoint = CSScriptIntellisense.Npp.GetCaretScreenLocation();
            aPoint.Y += CSScriptIntellisense.Npp.GetTextHeight(CSScriptIntellisense.Npp.GetCaretLineNumber());
            return aPoint;
        }

        static public Point GetCaretScreenLocation()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            int pos = (int)Win32.SendMessage(sci, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            int x = (int)Win32.SendMessage(sci, SciMsg.SCI_POINTXFROMPOSITION, 0, pos);
            int y = (int)Win32.SendMessage(sci, SciMsg.SCI_POINTYFROMPOSITION, 0, pos);

            Point point = new Point(x, y);
            ClientToScreen(sci, ref point);
            return point;
        }

        static public int GetPositionFromMouseLocation()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();

            Point point = Cursor.Position;
            ScreenToClient(sci, ref point);

            int pos = (int)Win32.SendMessage(sci, SciMsg.SCI_CHARPOSITIONFROMPOINTCLOSE, point.X, point.Y);

            return pos;
        }

        static public void Exit()
        {
            const int WM_COMMAND = 0x111;
            Win32.SendMessage(Plugin.nppData._nppHandle, (NppMsg)WM_COMMAND, (int)NppMenuCmd.IDM_FILE_EXIT, 0);
        }

        static public string GetTextBetween(Point point)
        {
            return GetTextBetween(point.X, point.Y);
        }

        static public string GetTextBetween(int start, int end = -1)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();

            if (end == -1)
                end = (int)Win32.SendMessage(sci, SciMsg.SCI_GETLENGTH, 0, 0);

            using (var tr = new Sci_TextRange(start, end, end - start + 1)) //+1 for null termination
            {
                Win32.SendMessage(sci, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                return tr.lpstrText;
            }
        }

        static public void SetTextBetween(string text, Point point)
        {
            SetTextBetween(text, point.X, point.Y);
        }

        static public void SetTextBetween(string text, int start, int end = -1)
        {
            //supposed not to scroll
            IntPtr sci = Plugin.GetCurrentScintilla();

            if (end == -1)
                end = (int)Win32.SendMessage(sci, SciMsg.SCI_GETLENGTH, 0, 0);

            Win32.SendMessage(sci, SciMsg.SCI_SETTARGETSTART, start, 0);
            Win32.SendMessage(sci, SciMsg.SCI_SETTARGETEND, end, 0);
            Win32.SendMessage(sci, SciMsg.SCI_REPLACETARGET, text);
        }

        static public string TextAfterCursor(int maxLength)
        {
            IntPtr hCurrentEditView = Plugin.GetCurrentScintilla();
            int currentPos = (int)Win32.SendMessage(hCurrentEditView, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            return TextAfterPosition(currentPos, maxLength);
        }

        static public string TextAfterPosition(int position, int maxLength)
        {
            int bufCapacity = maxLength + 1;
            IntPtr hCurrentEditView = Plugin.GetCurrentScintilla();
            int currentPos = position;
            int fullLength = (int)Win32.SendMessage(hCurrentEditView, SciMsg.SCI_GETLENGTH, 0, 0);
            int startPos = currentPos;
            int endPos = Math.Min(currentPos + bufCapacity, fullLength);
            int size = endPos - startPos;

            if (size > 0)
            {
                using (var tr = new Sci_TextRange(startPos, endPos, bufCapacity))
                {
                    Win32.SendMessage(hCurrentEditView, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                    return tr.lpstrText;
                }
            }
            else
                return null;
        }

        static public void ReplaceWordFromToken(Tokenizer.TokenTag ? token, string insertionText)
        {            
            IntPtr sci = Plugin.GetCurrentScintilla();
            int aCaretPos = GetCaretPosition();
            bool isCaretInsideToken = (aCaretPos >= token.Value.BufferPosition && aCaretPos < (token.Value.BufferPosition + token.Value.Context.Length));

            if (token.HasValue && (token.Value.Type != RTextTokenTypes.Space && token.Value.Type != RTextTokenTypes.Comma && token.Value.Type != RTextTokenTypes.Label) || (token.Value.Type == RTextTokenTypes.Label && isCaretInsideToken))
            {
                //if token is space or comma or label, add the new text after it!
                Win32.SendMessage(sci, SciMsg.SCI_SETSELECTION, token.Value.BufferPosition, token.Value.BufferPosition + token.Value.Context.Length);
            }
            else
            {
                Win32.SendMessage(sci, SciMsg.SCI_SETSELECTION, aCaretPos, aCaretPos);
            }
            Win32.SendMessage(sci, SciMsg.SCI_REPLACESEL, insertionText);
        }                      

        static public IntPtr CurrentScintilla
        {
            get { return Plugin.GetCurrentScintilla(); }
        }

        static public IntPtr NppHandle
        {
            get { return Plugin.nppData._nppHandle; }
        }

        public static int GetCaretPosition()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            int currentPos = (int)Win32.SendMessage(sci, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            return currentPos;
        }

        public static int GetCaretLineNumber()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            int currentPos = (int)Win32.SendMessage(sci, SciMsg.SCI_GETCURRENTPOS, 0, 0);

            return (int)Win32.SendMessage(sci, SciMsg.SCI_LINEFROMPOSITION, currentPos, 0);
        }

        public static void SetCaretPosition(int pos)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            Win32.SendMessage(sci, SciMsg.SCI_SETCURRENTPOS, pos, 0);
        }

        public static void ClearSelection()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            int currentPos = (int)Win32.SendMessage(sci, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            Win32.SendMessage(sci, SciMsg.SCI_SETSELECTIONSTART, currentPos, 0);
            Win32.SendMessage(sci, SciMsg.SCI_SETSELECTIONEND, currentPos, 0); ;
        }

        public static void SetSelection(int start, int end)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            Win32.SendMessage(sci, SciMsg.SCI_SETSELECTIONSTART, start, 0);
            Win32.SendMessage(sci, SciMsg.SCI_SETSELECTIONEND, end, 0); ;
        }

        static public int GrabFocus()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            int currentPos = (int)Win32.SendMessage(sci, SciMsg.SCI_GRABFOCUS, 0, 0);
            return currentPos;
        }

        static public void ScrollToCaret()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            Win32.SendMessage(sci, SciMsg.SCI_SCROLLCARET, 0, 0);
            Win32.SendMessage(sci, SciMsg.SCI_LINESCROLL, 0, 1); //bottom scrollbar can hide the line
            Win32.SendMessage(sci, SciMsg.SCI_SCROLLCARET, 0, 0);
        }

        static public void OpenFile(string file)
        {
            IntPtr sci = Plugin.nppData._nppHandle;
            Win32.SendMessage(sci, NppMsg.NPPM_DOOPEN, 0, file);
        }

        static public void GoToLine(int line)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            //Win32.SendMessage(sci, SciMsg.SCI_ENSUREVISIBLE, line - 1, 0);
            Win32.SendMessage(sci, SciMsg.SCI_GOTOLINE, line + 20, 0);
            Win32.SendMessage(sci, SciMsg.SCI_GOTOLINE, line - 1, 0);
            /*
             SCI_GETFIRSTVISIBLELINE = 2152,
        SCI_GETLINE = 2153,
        SCI_GETLINECOUNT = 2154,*/
        }

        static public Rectangle GetClientRect()
        {
            IntPtr sci = Plugin.GetCurrentScintilla();

            Rectangle r = new Rectangle();
            GetWindowRect(sci, ref r);
            return r;
        }

        static public string TextBeforePosition(int position, int maxLength)
        {
            int bufCapacity = maxLength + 1;
            IntPtr hCurrentEditView = Plugin.GetCurrentScintilla();
            int currentPos = position;
            int beginPos = currentPos - maxLength;
            int startPos = (beginPos > 0) ? beginPos : 0;
            int size = currentPos - startPos;

            if (size > 0)
            {
                using (var tr = new Sci_TextRange(startPos, currentPos, bufCapacity))
                {
                    Win32.SendMessage(hCurrentEditView, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                    return tr.lpstrText;
                }
            }
            else
                return null;
        }

        static public string TextBeforeCursor(int maxLength)
        {
            IntPtr hCurrentEditView = Plugin.GetCurrentScintilla();
            int currentPos = (int)Win32.SendMessage(hCurrentEditView, SciMsg.SCI_GETCURRENTPOS, 0, 0);

            return TextBeforePosition(currentPos, maxLength);
        }

        /// <summary>
        /// Retrieve the height of a particular line of text in pixels.
        /// </summary>
        static public int GetTextHeight(int line)
        {
            return (int)Win32.SendMessage(CurrentScintilla, SciMsg.SCI_TEXTHEIGHT, line, 0);
        }

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(uint uCode, uint uMapType);

        [DllImportAttribute("user32.dll")]
        public static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpChar, int uFlags);

        [DllImportAttribute("user32.dll")]
        public static extern int GetKeyboardState(byte[] pbKeyState);

        public static char GetAsciiCharacter(int uVirtKey, int uScanCode)
        {
            byte[] lpKeyState = new byte[256];
            GetKeyboardState(lpKeyState);
            byte[] lpChar = new byte[2];
            if (ToAscii(uVirtKey, uScanCode, lpKeyState, lpChar, 0) == 1)
                return (char)lpChar[0];
            else
                return new char();
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, IntPtr windowTitle);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, IntPtr lParam);

        public const int SB_SETTEXT = 1035;
        public const int SB_SETPARTS = 1028;
        public const int SB_GETPARTS = 1030;

        private const uint WM_USER = 0x0400;
        //private const uint SB_SETPARTS = WM_USER + 4;
        //private const uint SB_GETPARTS = WM_USER + 6;
        private const uint SB_GETTEXTLENGTH = WM_USER + 12;
        private const uint SB_GETTEXT = WM_USER + 13;

        static public string GetStatusbarLabel()
        {
            throw new NotImplementedException();
        }

        //static public unsafe void SetStatusbarLabel(string labelText)
        static public string SetStatusbarLabel(string labelText)
        {
            string retval = null;
            IntPtr mainWindowHandle = Plugin.nppData._nppHandle;
            // find status bar control on the main window of the application
            IntPtr statusBarHandle = FindWindowEx(mainWindowHandle, IntPtr.Zero, "msctls_statusbar32", IntPtr.Zero);
            if (statusBarHandle != IntPtr.Zero)
            {

                //cut current text
                var size = (int)SendMessage(statusBarHandle, SB_GETTEXTLENGTH, 0, IntPtr.Zero);
                var buffer = new StringBuilder(size);
                Win32.SendMessage(statusBarHandle, (NppMsg)SB_GETTEXT, 0, buffer);
                retval = buffer.ToString();

                // set text for the existing part with index 0
                IntPtr text = Marshal.StringToHGlobalAuto(labelText);
                SendMessage(statusBarHandle, SB_SETTEXT, 0, text);

                Marshal.FreeHGlobal(text);

                //the foolowing may be needed for puture features
                // create new parts width array
                //int nParts = SendMessage(statusBarHandle, SB_GETPARTS, 0, IntPtr.Zero).ToInt32();
                //nParts++;
                //IntPtr memPtr = Marshal.AllocHGlobal(sizeof(int) * nParts);
                //int partWidth = 100; // set parts width according to the form size
                //for (int i = 0; i < nParts; i++)
                //{
                //    Marshal.WriteInt32(memPtr, i * sizeof(int), partWidth);
                //    partWidth += partWidth;
                //}
                //SendMessage(statusBarHandle, SB_SETPARTS, nParts, memPtr);
                //Marshal.FreeHGlobal(memPtr);

                //// set text for the new part
                //IntPtr text0 = Marshal.StringToHGlobalAuto("new section text 1");
                //SendMessage(statusBarHandle, SB_SETTEXT, nParts - 1, text0);
                //Marshal.FreeHGlobal(text0);
            }
            return retval;
        }
    }
}