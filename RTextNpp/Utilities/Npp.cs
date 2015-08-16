/**
 * \file    Utilities\instancecs
 *
 * Taken as is fron CSScript plugin for notepad++.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace RTextNppPlugin.Utilities
{
    using CSScriptIntellisense;
    using RTextNppPlugin.DllExport;
    using RTextNppPlugin.RText.Parsing;
    public class Npp : INpp
    {
        #region [Singleton Data Members]
        private static volatile Npp instance;
        private static object syncRoot = new Object();
        private static IWin32 _win32 = new Win32();
        #endregion        

        private Npp() { }

        static public Npp Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                            instance = new Npp();
                    }
                }

                return instance;
            }
        }

        public void JumpToLine(string file, int line)
        {            
            OpenFile(file);
            GoToLine(line);
            ScrollUpToLine(line);
        }

        public IntPtr GetCurrentScintilla(NppData nppData)
        {
            int curScintilla;
            _win32.ISendMessage(nppData._nppHandle, NppMsg.NPPM_GETCURRENTSCINTILLA, 0, out curScintilla);
            return (curScintilla == 0) ? nppData._scintillaMainHandle : nppData._scintillaSecondHandle;
        }

        public void SetEditorFocus(int setFocus = 1)
        {
            _win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_SETFOCUS, setFocus, 0);
        }

        public int GetZoomLevel()
        {
            return (int)_win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_GETZOOM, 0, 0);
        }

        public int GetSelectionStart()
        {
            return (int)_win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_GETSELECTIONNSTART, 0, 0);
        }

        public int GetSelectionLength()
        {
            int aSelStart = (int)_win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_GETSELECTIONNSTART, 0, 0);
            int aSelEnd = (int)_win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_GETSELECTIONNEND, 0, 0);
            if (aSelStart == aSelEnd)
            {
                return 1;
            }
            return aSelEnd - aSelStart;
        }

        public int GetSelections()
        {
            return (int)_win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_GETSELECTIONS, 0, 0);
        }

        public void DeleteFront()
        {
            if (GetSelectionLength() > 1)
            {
                DeleteBack(1);
            }
            else
            {
                SetCaretPosition(GetCaretPosition() + 1);
                DeleteBack(1);
            }
        }

        public void DeleteBack(int length)
        {
            for (int i = 0; i < length; ++i)
            {
                _win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_DELETEBACK, 0, 0);
            }
        }

        public void DeleteRange(int position, int length)
        {
            _win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_DELETERANGE, position, length);
        }

        /**
         * \brief   Gets current file path.
         *
         * \return  The file path of the currently viewed document.
         */
        public string GetCurrentFilePath()
        {
            NppMsg msg = NppMsg.NPPM_GETFULLCURRENTPATH;
            StringBuilder path = new StringBuilder(Win32.MAX_PATH);
            _win32.ISendMessage(Plugin.nppData._nppHandle, msg, 0, path);
            return path.ToString();
        }

        /**
         * Query if 'file' is file modified.
         *
         * \param   file    The file.
         *
         * \return  true if file is considered to be modified, false if not.
         */
        public bool IsFileModified(string file)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            return ((int)_win32.ISendMessage(sci, SciMsg.SCI_GETMODIFY, 0, 0) != 0);
        }

        /**
         * Saves the currently viewed file.
         *
         * \param   file    The file.
         */
        public void SaveFile(string file)
        {
            _win32.ISendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, file);
            _win32.ISendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_SAVECURRENTFILE, 0, 0);
        }

        /**
         * Switches active view to file.
         *
         * \param   file    The file.
         */
        public void SwitchToFile(string file)
        {
            _win32.ISendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, file);
        }


        public void AddText(string s)
        {
            if (GetSelectionLength() > 1)
            {
                DeleteRange(GetSelectionStart(), GetSelectionLength());
            }
            //if insert is active replace the next char
            if (KeyInterceptor.GetModifiers().IsInsert)
            {
                //delete only if not at end of line
                if (GetCaretPosition() < GetLineEnd())
                {
                    DeleteFront();
                }
            }
            _win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_ADDTEXT, s.GetByteCount(), s);
        }

        public void ChangeMenuItemCheck(int CmdId, bool isChecked)
        {
            _win32.ISendMessage(instance.NppHandle, NppMsg.NPPM_SETMENUITEMCHECK, CmdId, isChecked ? 1 : 0);
        }

        public string GetCurrentFile()
        {
            var path = new StringBuilder(Win32.MAX_PATH);
            _win32.ISendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);
            return path.ToString();
        }
        public void SaveCurrentFile()
        {
            _win32.ISendMessage(instance.NppHandle, NppMsg.NPPM_SAVECURRENTFILE, 0, 0);
        }

        public void DisplayInNewDocument(string text)
        {
            _win32.ISendMessage(instance.NppHandle, NppMsg.NPPM_MENUCOMMAND, 0, NppMenuCmd.IDM_FILE_NEW);
            _win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_GRABFOCUS, 0, 0);
            _win32.ISendMessage(instance.CurrentScintilla, SciMsg.SCI_ADDTEXT, text);
        }

        public void SetIndicatorStyle(int indicator, SciMsg style, Color color)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            _win32.ISendMessage(sci, SciMsg.SCI_INDICSETSTYLE, indicator, (int)style);
            _win32.ISendMessage(sci, SciMsg.SCI_INDICSETFORE, indicator, ColorTranslator.ToWin32(color));
        }

        public void ClearIndicator(int indicator, int startPos, int endPos)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            _win32.ISendMessage(sci, SciMsg.SCI_SETINDICATORCURRENT, indicator, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_INDICATORCLEARRANGE, startPos, endPos - startPos);
        }

        public void PlaceIndicator(int indicator, int startPos, int endPos)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            _win32.ISendMessage(sci, SciMsg.SCI_SETINDICATORCURRENT, indicator, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_INDICATORFILLRANGE, startPos, endPos - startPos);
        }

        public string GetConfigDir()
        {
            var buffer = new StringBuilder(260);
            _win32.ISendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, 260, buffer);
            return buffer.ToString();
        }

        public Point[] FindIndicatorRanges(int indicator)
        {
            var ranges = new List<Point>();

            IntPtr sci = GetCurrentScintilla(Plugin.nppData);

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

                int rangeStart = (int)_win32.ISendMessage(sci, SciMsg.SCI_INDICATORSTART, indicator, testPosition);
                int rangeEnd = (int)_win32.ISendMessage(sci, SciMsg.SCI_INDICATOREND, indicator, testPosition);
                int value = (int)_win32.ISendMessage(sci, SciMsg.SCI_INDICATORVALUEAT, indicator, testPosition);
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
        public string GetLine()
        {
            return GetLine(GetLineNumber(GetCaretPosition()));
        }

        public string GetLine(int line)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);

            int length = (int)_win32.ISendMessage(sci, SciMsg.SCI_LINELENGTH, line, 0);
            var buffer = new StringBuilder(length + 1);
            _win32.ISendMessage(sci, SciMsg.SCI_GETLINE, line, buffer);
            buffer.Length = length; //NPP may inject some rubbish at the end of the line
            return buffer.ToString();
        }

        public StringBuilder GetLineAsStringBuilder(int line)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);

            int length = (int)_win32.ISendMessage(sci, SciMsg.SCI_LINELENGTH, line, 0);
            var buffer = new StringBuilder(length + 1);
            _win32.ISendMessage(sci, SciMsg.SCI_GETLINE, line, buffer);
            buffer.Length = length; //NPP may inject some rubbish at the end of the line
            return buffer;
        }

        /**
         * Gets line number.
         *
         * \return  The line number from the current caret position.
         */
        public int GetLineNumber()
        {
            return GetLineNumber(GetCaretPosition());
        }

        public int GetLineNumber(int position)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            return (int)_win32.ISendMessage(sci, SciMsg.SCI_LINEFROMPOSITION, position, 0);
        }

        public int GetLengthToEndOfLine(int currentCharacterColumn, int line)
        {
            return GetLine(line).RemoveNewLine().Length - currentCharacterColumn;
        }

        public int GetLengthToEndOfLine(int currentCharacterColumn)
        {
            return GetLine().RemoveNewLine().Length - currentCharacterColumn;
        }

        public int GetColumn(int position)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            return (int)_win32.ISendMessage(sci, SciMsg.SCI_GETCOLUMN, position, 0);
        }

        public int GetColumn()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            return (int)_win32.ISendMessage(sci, SciMsg.SCI_GETCOLUMN, GetCaretPosition(), 0);
        }

        public int GetLineEnd(int line = -1)
        {
            if (line == -1)
            {
                return (int)_win32.ISendMessage(GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_GETLINEENDPOSITION, GetLineNumber(), 0);
            }
            else
            {
                return (int)_win32.ISendMessage(GetCurrentScintilla(Plugin.nppData), SciMsg.SCI_GETLINEENDPOSITION, line, 0);
            }
        }

        public int GetLineStart(int line)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            return (int)_win32.ISendMessage(sci, SciMsg.SCI_POSITIONFROMLINE, line, 0);
        }

        public int GetFirstVisibleLine()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            return (int)_win32.ISendMessage(sci, SciMsg.SCI_GETFIRSTVISIBLELINE, 0, 0);
        }

        public void SetFirstVisibleLine(int line)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            _win32.ISendMessage(sci, SciMsg.SCI_SETFIRSTVISIBLELINE, line, 0);
        }

        public int GetLineCount()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            return (int)_win32.ISendMessage(sci, SciMsg.SCI_GETLINECOUNT, 0, 0);
        }

        public string GetShortcutsFile()
        {
            return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(GetConfigDir())), "shortcuts.xml");
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
        public Point GetCaretScreenLocationRelativeToPosition(int position)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int x = (int)_win32.ISendMessage(sci, SciMsg.SCI_POINTXFROMPOSITION, 0, position);
            int y = (int)_win32.ISendMessage(sci, SciMsg.SCI_POINTYFROMPOSITION, 0, position);
            Point aPoint = new Point(x, y);
            ClientToScreen(sci, ref aPoint);
            aPoint.Y += GetTextHeight(GetCaretLineNumber());
            return aPoint;
        }

        /**
         * Gets caret screen location for form. ( under a certain buffer position )
         *
         * \return  The caret screen location for form.
         */
        public Point GetCaretScreenLocationForForm(int position)
        {
            Point aPoint = GetCaretScreenLocationRelativeToPosition(position);
            aPoint.Y += GetTextHeight(GetCaretLineNumber());
            return aPoint;
        }

        /**
         * Gets caret screen location for form. ( under caret character )
         *
         * \return  The caret screen location for form.
         */
        public Point GetCaretScreenLocationForForm()
        {
            Point aPoint    = GetCaretScreenLocation();
            int aTextHeight = GetTextHeight(GetCaretLineNumber());
            aPoint.Y        += aTextHeight;
            return aPoint;
        }

        /**
         * Gets caret screen location for form above word.
         *
         * \return  The caret screen location for form above word is exactly the position of the cursor.
         */
        public Point GetCaretScreenLocationForFormAboveWord()
        {
            return GetCaretScreenLocation();
        }

        /**
         * Gets caret screen location for form above word.
         *
         * \param   position    The buffer position.
         *
         * \return  The caret screen location for form above word from a word starting at position.
         */
        public Point GetCaretScreenLocationForFormAboveWord(int position)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int x = (int)_win32.ISendMessage(sci, SciMsg.SCI_POINTXFROMPOSITION, 0, position);
            int y = (int)_win32.ISendMessage(sci, SciMsg.SCI_POINTYFROMPOSITION, 0, position);

            Point point = new Point(x, y);
            ClientToScreen(sci, ref point);
            return point;
        }

        public Point GetCaretScreenLocation()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int pos = (int)_win32.ISendMessage(sci, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            int x = (int)_win32.ISendMessage(sci, SciMsg.SCI_POINTXFROMPOSITION, 0, pos);
            int y = (int)_win32.ISendMessage(sci, SciMsg.SCI_POINTYFROMPOSITION, 0, pos);
            
            Point point = new Point(x, y);
            ClientToScreen(sci, ref point);
            return point;
        }

        public int GetPositionFromMouseLocation()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);

            Point point = Cursor.Position;
            ScreenToClient(sci, ref point);

            return (int)_win32.ISendMessage(sci, SciMsg.SCI_CHARPOSITIONFROMPOINTCLOSE, point.X, point.Y);
        }

        public void Exit()
        {
            const int WM_COMMAND = 0x111;
            _win32.ISendMessage(Plugin.nppData._nppHandle, (NppMsg)WM_COMMAND, (int)NppMenuCmd.IDM_FILE_EXIT, 0);
        }

        public string GetTextBetween(Point point)
        {
            return GetTextBetween(point.X, point.Y);
        }

        public string GetTextBetween(int start, int end = -1)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);

            if (end == -1)
                end = (int)_win32.ISendMessage(sci, SciMsg.SCI_GETLENGTH, 0, 0);

            using (var tr = new Sci_TextRange(start, end, end - start + 1)) //+1 for null termination
            {
                _win32.ISendMessage(sci, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                return tr.lpstrText;
            }
        }

        public void SetTextBetween(string text, Point point)
        {
            SetTextBetween(text, point.X, point.Y);
        }

        public void SetTextBetween(string text, int start, int end = -1)
        {
            //supposed not to scroll
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);

            if (end == -1)
                end = (int)_win32.ISendMessage(sci, SciMsg.SCI_GETLENGTH, 0, 0);

            _win32.ISendMessage(sci, SciMsg.SCI_SETTARGETSTART, start, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_SETTARGETEND, end, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_REPLACETARGET, text);
        }

        public string TextAfterCursor(int maxLength)
        {
            IntPtr hCurrentEditView = GetCurrentScintilla(Plugin.nppData);
            int currentPos = (int)_win32.ISendMessage(hCurrentEditView, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            return TextAfterPosition(currentPos, maxLength);
        }

        public string TextAfterPosition(int position, int maxLength)
        {
            int bufCapacity = maxLength + 1;
            IntPtr hCurrentEditView = GetCurrentScintilla(Plugin.nppData);
            int currentPos = position;
            int fullLength = (int)_win32.ISendMessage(hCurrentEditView, SciMsg.SCI_GETLENGTH, 0, 0);
            int startPos = currentPos;
            int endPos = Math.Min(currentPos + bufCapacity, fullLength);
            int size = endPos - startPos;

            if (size > 0)
            {
                using (var tr = new Sci_TextRange(startPos, endPos, bufCapacity))
                {
                    _win32.ISendMessage(hCurrentEditView, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                    return tr.lpstrText;
                }
            }
            else
                return null;
        }

        public void ReplaceWordFromToken(Tokenizer.TokenTag ? token, string insertionText)
        {            
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int aCaretPos = GetCaretPosition();
            bool isCaretInsideToken = (aCaretPos >= token.Value.BufferPosition && aCaretPos < (token.Value.BufferPosition + token.Value.Context.Length));

            if (token.HasValue && (token.Value.Type != RTextTokenTypes.Space && token.Value.Type != RTextTokenTypes.Comma && token.Value.Type != RTextTokenTypes.Label) || (token.Value.Type == RTextTokenTypes.Label && isCaretInsideToken))
            {
                //if token is space or comma or label, add the new text after it!
                _win32.ISendMessage(sci, SciMsg.SCI_SETSELECTION, token.Value.BufferPosition, token.Value.BufferPosition + token.Value.Context.Length);
            }
            else
            {
                _win32.ISendMessage(sci, SciMsg.SCI_SETSELECTION, aCaretPos, aCaretPos);
            }
            _win32.ISendMessage(sci, SciMsg.SCI_REPLACESEL, insertionText);
        }                      

        public IntPtr CurrentScintilla
        {
            get { return GetCurrentScintilla(Plugin.nppData); }
        }

        public IntPtr NppHandle
        {
            get { return Plugin.nppData._nppHandle; }
        }

        public int GetCaretPosition()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int currentPos = (int)_win32.ISendMessage(sci, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            return currentPos;
        }

        public int GetCaretLineNumber()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int currentPos = (int)_win32.ISendMessage(sci, SciMsg.SCI_GETCURRENTPOS, 0, 0);

            return (int)_win32.ISendMessage(sci, SciMsg.SCI_LINEFROMPOSITION, currentPos, 0);
        }

        public void SetCaretPosition(int pos)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            _win32.ISendMessage(sci, SciMsg.SCI_SETCURRENTPOS, pos, 0);
        }

        public void ClearSelection()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int currentPos = (int)_win32.ISendMessage(sci, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_SETSELECTIONSTART, currentPos, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_SETSELECTIONEND, currentPos, 0); ;
        }

        public void SetSelection(int start, int end)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            _win32.ISendMessage(sci, SciMsg.SCI_SETSELECTIONSTART, start, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_SETSELECTIONEND, end, 0); ;
        }

        public int GrabFocus()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int currentPos = (int)_win32.ISendMessage(sci, SciMsg.SCI_GRABFOCUS, 0, 0);
            return currentPos;
        }

        public void ScrollToCaret()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            _win32.ISendMessage(sci, SciMsg.SCI_SCROLLCARET, 0, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_LINESCROLL, 0, 1); //bottom scrollbar can hide the line
            _win32.ISendMessage(sci, SciMsg.SCI_SCROLLCARET, 0, 0);
        }

        public void OpenFile(string file)
        {
            IntPtr sci = Plugin.nppData._nppHandle;
            _win32.ISendMessage(sci, NppMsg.NPPM_DOOPEN, 0, file);
        }

        public void GoToLine(int line)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            _win32.ISendMessage(sci, SciMsg.SCI_ENSUREVISIBLE, line - 1, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_GOTOLINE, line - 1, 0);

            
        }

        /**
         * \brief   Scroll up to line. Makes "line" the first visible line of the document, if possible by scrolling up, else has no effect.
         *
         * \param   line    The line.
         */
        public void ScrollUpToLine(int line)
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);
            int firstVisibleLine = (int)_win32.ISendMessage(sci, SciMsg.SCI_GETFIRSTVISIBLELINE, 0, 0);
            _win32.ISendMessage(sci, SciMsg.SCI_LINESCROLL, 0, (line - (1 + firstVisibleLine)));            
        }

        /**
         * Gets client rectangle from control.
         *
         * \param   hwnd    The window handle of the control, e.g. the N++ handle.
         *
         * \return  The client rectangle from control.
         */
        public Rectangle GetClientRectFromControl(IntPtr hwnd)
        {
            return Screen.FromHandle(hwnd).WorkingArea;
        }

        public Rectangle GetClientRectFromPoint(Point p)
        {
            return Screen.FromPoint(p).WorkingArea;
        }

        public Rectangle GetClientRect()
        {
            IntPtr sci = GetCurrentScintilla(Plugin.nppData);

            Rectangle r = new Rectangle();
            GetWindowRect(sci, ref r);
            return r;
        }

        public string TextBeforePosition(int position, int maxLength)
        {
            int bufCapacity = maxLength + 1;
            IntPtr hCurrentEditView = GetCurrentScintilla(Plugin.nppData);
            int currentPos = position;
            int beginPos = currentPos - maxLength;
            int startPos = (beginPos > 0) ? beginPos : 0;
            int size = currentPos - startPos;

            if (size > 0)
            {
                using (var tr = new Sci_TextRange(startPos, currentPos, bufCapacity))
                {
                    _win32.ISendMessage(hCurrentEditView, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                    return tr.lpstrText;
                }
            }
            else
                return null;
        }

        public string TextBeforeCursor(int maxLength)
        {
            IntPtr hCurrentEditView = GetCurrentScintilla(Plugin.nppData);
            int currentPos = (int)_win32.ISendMessage(hCurrentEditView, SciMsg.SCI_GETCURRENTPOS, 0, 0);

            return TextBeforePosition(currentPos, maxLength);
        }

        /// <summary>
        /// Retrieve the height of a particular line of text in pixels.
        /// </summary>
        public int GetTextHeight(int line)
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_TEXTHEIGHT, line, 0);
        }

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(uint uCode, uint uMapType);

        [DllImportAttribute("user32.dll")]
        public static extern int ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpChar, int uFlags);

        [DllImportAttribute("user32.dll")]
        public static extern int GetKeyboardState(byte[] pbKeyState);

        public char GetAsciiCharacter(int uVirtKey, int uScanCode)
        {
            byte[] lpKeyState = new byte[256];
            GetKeyboardState(lpKeyState);
            byte[] lpChar = new byte[2];
            if (ToAscii(uVirtKey, uScanCode, lpKeyState, lpChar, 0) == 1)
            {
                return (char)lpChar[0];
            }
            else
            {
                return new char();
            }
        }        
    }
}