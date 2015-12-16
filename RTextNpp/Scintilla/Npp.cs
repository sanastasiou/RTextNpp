using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace RTextNppPlugin.Scintilla
{
    using CSScriptIntellisense;
    using RTextNppPlugin.DllExport;
    using RTextNppPlugin.RText.Parsing;
    using RTextNppPlugin.Utilities;
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

        public void AddAnnotation(int line, System.Text.StringBuilder errorDescription)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_ANNOTATIONSETTEXT, line, errorDescription.ToString());
        }

        public void SetAnnotationVisible(IntPtr handle, int annotationStyle)
        {
            _win32.ISendMessage(handle, SciMsg.SCI_ANNOTATIONSETVISIBLE, annotationStyle, 0);
        }

        public void SetAnnotationStyle(int line, int annotationStyle)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_ANNOTATIONSETSTYLE, line, annotationStyle);
        }

        public void SetAnnotationStyles(int line, System.Text.StringBuilder stylesDescription)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_ANNOTATIONSETSTYLES, line, stylesDescription.ToString());
        }

        public object JumpToLine(string file, int line)
        {
            OpenFile(file);
            GoToLine(line);
            return null;
        }
        
        public IntPtr CurrentScintilla
        {
            get
            {
                int curScintilla;
                _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETCURRENTSCINTILLA, 0, out curScintilla);
                return (curScintilla == 0) ? Plugin.Instance.NppData._scintillaMainHandle : Plugin.Instance.NppData._scintillaSecondHandle;
            }
        }

        public IntPtr MainScintilla
        {
            get
            {
                return Plugin.Instance.NppData._scintillaMainHandle;
            }
        }

        public IntPtr SecondaryScintilla
        {
            get
            {
                return Plugin.Instance.NppData._scintillaSecondHandle;
            }
        }

        public int CurrentDocIndex(IntPtr scintilla)
        {
            return (int)_win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETCURRENTDOCINDEX, 0, (int)(scintilla == MainScintilla ? NppMsg.MAIN_VIEW : NppMsg.SUB_VIEW));
        }
        
        public void SetEditorFocus(int setFocus = 1)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETFOCUS, setFocus, 0);
        }
        
        public int GetZoomLevel(IntPtr sciPtr)
        {
            return (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_GETZOOM, 0, 0);
        }
        
        public int GetSelectionStart()
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETSELECTIONNSTART, 0, 0);
        }
        
        public int GetSelectionLength()
        {
            int aSelStart = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETSELECTIONNSTART, 0, 0);
            int aSelEnd = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETSELECTIONNEND, 0, 0);
            if (aSelStart == aSelEnd)
            {
                return 1;
            }
            return aSelEnd - aSelStart;
        }
        
        public int GetSelections()
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETSELECTIONS, 0, 0);
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
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_DELETEBACK, 0, 0);
            }
        }

        public void ClearAllAnnotations(IntPtr sciPtr)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_ANNOTATIONCLEARALL, 0, 0);
        }
        
        public void DeleteRange(int position, int length)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_DELETERANGE, position, length);
        }
        
        /**
         * \brief   Gets current file path.
         *
         * \return  The file path of the currently viewed document.
         */        
        public string GetCurrentFilePath()
        {
            StringBuilder path = new StringBuilder(Win32.MAX_PATH);
            _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);
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
            return ((int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETMODIFY, 0, 0) != 0);
        }
        
        /**
         * Saves the currently viewed file.
         *
         * \param   file    The file.
         */
        public void SaveFile(string file)
        {
            _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, file);
            _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_SAVECURRENTFILE, 0, 0);
        }
        
        /**
         * Switches active view to file.
         *
         * \param   file    The file.
         */
        public void SwitchToFile(string file)
        {
            _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, file);
        }
        
        public unsafe void AddText(string text)
        {
            if (GetSelectionLength() > 1)
            {
                DeleteRange(GetSelectionStart(), GetSelectionLength());
            }
            //if insert is active replace the next char
            if (KeyInterceptor.GetModifiers().IsInsert)
            {
                //delete only if not at end of line
                if (GetCaretPosition() < GetLineEnd(GetCaretPosition(), GetLineNumber()))
                {
                    DeleteFront();
                }
            }

            var bytes = GetBytes(text ?? string.Empty, Encoding, zeroTerminated: false);
            fixed (byte* bp = bytes)
            {
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_ADDTEXT, new IntPtr(bytes.Length), new IntPtr(bp));
            }
        }

        public void ChangeMenuItemCheck(int CmdId, bool isChecked)
        {
            _win32.ISendMessage(instance.NppHandle, NppMsg.NPPM_SETMENUITEMCHECK, CmdId, isChecked ? 1 : 0);
        }
        
        public string GetCurrentFile()
        {
            var path = new StringBuilder(Win32.MAX_PATH);
            _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);
            return path.ToString();
        }
        
        public void SaveCurrentFile()
        {
            _win32.ISendMessage(instance.NppHandle, NppMsg.NPPM_SAVECURRENTFILE, 0, 0);
        }
        
        public void SetIndicatorStyle(IntPtr sciPtr, int indicator, SciMsg style, Color color)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_INDICSETSTYLE, indicator, (int)style);
            _win32.ISendMessage(sciPtr, SciMsg.SCI_INDICSETFORE, indicator, ColorTranslator.ToWin32(color));
        }
        
        public void ClearIndicator(int indicator, int startPos, int endPos)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETINDICATORCURRENT, indicator, 0);
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_INDICATORCLEARRANGE, startPos, endPos - startPos);
        }
        
        public void PlaceIndicator(int indicator, int startPos, int endPos)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETINDICATORCURRENT, indicator, 0);
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_INDICATORFILLRANGE, startPos, endPos - startPos);
        }
        
        public string GetConfigDir()
        {
            var buffer = new StringBuilder(260);
            _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, 260, buffer);
            return buffer.ToString();
        }
        
        public Point[] FindIndicatorRanges(int indicator)
        {
            var ranges = new List<Point>();
            IntPtr sci = CurrentScintilla;
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
        
        public string GetLine(int line)
        {
            return GetLineAsStringBuilder(line, CurrentScintilla).ToString();
        }

        public string GetLine(int line, IntPtr sciPtr)
        {
            return GetLineAsStringBuilder(line, sciPtr).ToString();
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
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_LINEFROMPOSITION, position, 0);
        }
        
        public int GetLengthToEndOfLine(int line, int position)
        {
            return ((int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETLINEENDPOSITION, line, 0) - position);
        }
               
        public int GetColumn(int position)
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETCOLUMN, position, 0);
        }
        
        public int GetColumn()
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETCOLUMN, GetCaretPosition(), 0);
        }
        
        public int GetLineEnd(int position, int line)
        {
            return GetLineStart(GetLineNumber(position)) + (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_LINELENGTH, line, 0);            
        }
        
        public int GetLineStart(int line)
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_POSITIONFROMLINE, line, 0);
        }
        
        public int GetFirstVisibleLine()
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETFIRSTVISIBLELINE, 0, 0);
        }
        
        public void SetFirstVisibleLine(int line)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETFIRSTVISIBLELINE, line, 0);
        }
        
        public int GetLineCount(IntPtr sciPtr)
        {
            return (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_GETLINECOUNT, 0, 0);
        }
        
        public string GetShortcutsFile()
        {
            return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(GetConfigDir())), Constants.Scintilla.SHORTCUTS_FILE);
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
            int x           = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_POINTXFROMPOSITION, 0, position);
            int y           = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_POINTYFROMPOSITION, 0, position);
            Point aPoint    = new Point(x, y);
            ClientToScreen(CurrentScintilla, ref aPoint);
            aPoint.Y        += GetTextHeight(GetCaretLineNumber());
            double dpiScale = VisualUtilities.GetDpiScalingFactor();
            aPoint.X        = (int)((double)(aPoint.X) / dpiScale);
            aPoint.Y        = (int)((double)(aPoint.Y) / dpiScale);
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
            double dpiScale = VisualUtilities.GetDpiScalingFactor();
            aPoint.X        = (int)((double)(aPoint.X) / dpiScale);
            aPoint.Y        = (int)((double)(aPoint.Y) / dpiScale);
            return aPoint;
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
            int x           = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_POINTXFROMPOSITION, 0, position);
            int y           = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_POINTYFROMPOSITION, 0, position);
            Point aPoint    = new Point(x, y);
            ClientToScreen(CurrentScintilla, ref aPoint);
            double dpiScale = VisualUtilities.GetDpiScalingFactor();
            aPoint.X        = (int)((double)(aPoint.X) / dpiScale);
            aPoint.Y        = (int)((double)(aPoint.Y) / dpiScale);
            return aPoint;
        }
        
        public Point GetCaretScreenLocation()
        {
            int pos = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            return GetCaretScreenLocationForFormAboveWord(pos);
        }
        
        public int GetPositionFromMouseLocation()
        {
            Point point = Cursor.Position;
            ScreenToClient(CurrentScintilla, ref point);
            //dpi conversion here?
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_CHARPOSITIONFROMPOINTCLOSE, point.X, point.Y);
        }
               
        public string GetTextBetween(int start, int end = -1)
        {
            if (end == -1)
            {
                end = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETLENGTH, 0, 0);
            }
            using (var tr = new Sci_TextRange(start, end, end - start + 1)) //+1 for null termination
            {
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETTEXTRANGE, 0, tr.NativePointer);
                return tr.lpstrText;
            }
        }
        
        unsafe public void ReplaceWordFromToken(Tokenizer.TokenTag ? token, string insertionText)
        {
            int aCaretPos = GetCaretPosition();
            bool isCaretInsideToken = (aCaretPos >= token.Value.BufferPosition && aCaretPos < (token.Value.BufferPosition + token.Value.Context.Length));
            if (token.HasValue && (token.Value.Type != RTextTokenTypes.Space && token.Value.Type != RTextTokenTypes.Comma && token.Value.Type != RTextTokenTypes.Label && token.Value.Type != RTextTokenTypes.NewLine) || (token.Value.Type == RTextTokenTypes.Label && isCaretInsideToken))
            {
                //if token is space or comma or label, add the new text after it!
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETSELECTION, token.Value.BufferPosition, token.Value.BufferPosition + token.Value.Context.Length);
            }
            else
            {
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETSELECTION, aCaretPos, aCaretPos);
            }

            var bytes = GetBytes(insertionText ?? string.Empty, Encoding, zeroTerminated: false);
            fixed (byte* bp = bytes)
            {
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_REPLACESEL, new IntPtr(bytes.Length), new IntPtr(bp));
            }
        }
               
        public IntPtr NppHandle
        {
            get { return Plugin.Instance.NppData._nppHandle; }
        }
        
        public int GetCaretPosition()
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0);
        }
        
        public int GetCaretLineNumber()
        {
            int currentPos = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETCURRENTPOS, 0, 0);
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_LINEFROMPOSITION, currentPos, 0);
        }
        
        public void SetCaretPosition(int pos)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETCURRENTPOS, pos, 0);
        }
        
        public void ClearSelection()
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_CLEARSELECTIONS, 0, 0);
        }
        
        public void SetSelection(int start, int end)
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETSELECTIONSTART, start, 0);
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SETSELECTIONEND, end, 0); ;
        }
        
        public int GrabFocus(IntPtr sciPtr)
        {
            int currentPos = (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_GRABFOCUS, 0, 0);
            return currentPos;
        }
        
        public void ScrollToCaret()
        {
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SCROLLCARET, 0, 0);
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_LINESCROLL, 0, 1); //bottom scrollbar can hide the line
            _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_SCROLLCARET, 0, 0);
        }
        
        public void OpenFile(string file)
        {
            IntPtr sci = Plugin.Instance.NppData._nppHandle;
            _win32.ISendMessage(sci, NppMsg.NPPM_DOOPEN, 0, file);
        }

        public int FirstVisibleLine
        {
            get
            {
                //this is the first "visible" line on screen - it may differ from the actual doc line
                int firstVisibleLine = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETFIRSTVISIBLELINE, 0, 0);
                return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_DOCLINEFROMVISIBLE, firstVisibleLine, 0) + 1;
            }
        }

        public int LastVisibleLine
        { 
            get
            {
                //this is the first "visible" line on screen - it may differ from the actual doc line
                int firstVisibleLine = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETFIRSTVISIBLELINE, 0, 0);
                int lastVisilbeLine  = firstVisibleLine + LinesOnScreen;
                return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_DOCLINEFROMVISIBLE, lastVisilbeLine, 0) + 1;
            }
        }

        public int LinesOnScreen
        { 
            get
            {
                return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_LINESONSCREEN, 0, 0);
            }
        }

        public void GoToLine(int line)
        {
            int firstVisibleDocLine = FirstVisibleLine;
            int lastVisibleDocLine  = LastVisibleLine;
            if(IsLineVisible(firstVisibleDocLine, lastVisibleDocLine, line))
            {
                //just move cursor, line is already visible
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GOTOPOS, GetLineStart(line - 1), 0);
            }
            else
            {
                //line is not visible
                //move line in the middle of the screen
                int linesOnScreen = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_LINESONSCREEN, 0, 0);
                int offset = linesOnScreen >> 1;
                //check if we are behind new line, or after new line
                int currentLine = GetLineNumber();
                if(currentLine > line)
                {
                    offset = -offset;
                }
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_ENSUREVISIBLE, line - 1, 0);
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GOTOLINE, line - 1, 0);
                _win32.ISendMessage(CurrentScintilla, SciMsg.SCI_LINESCROLL, 0, offset);
            }
        }
        
        /**
         * \brief   Scroll up to line. Makes "line" the first visible line of the document, if possible by scrolling up, else has no effect.
         *
         * \param   line    The line.
         */
        public bool IsLineVisible(int line)
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETLINEVISIBLE, line, 0) == 1;
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
            Rectangle r = new Rectangle();
            GetWindowRect(CurrentScintilla, ref r);
            return r;
        }
        
        /// <summary>
        /// Retrieve the height of a particular line of text in pixels.
        /// </summary>
        public int GetTextHeight(int line)
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_TEXTHEIGHT, line, 0);
        }

        /// <summary>
        /// The set of valid MapTypes used in MapVirtualKey
        /// </summary>
        public enum MapVirtualKeyMapTypes : uint
        {
            /// <summary>
            /// uCode is a virtual-key code and is translated into a scan code.
            /// If it is a virtual-key code that does not distinguish between left- and
            /// right-hand keys, the left-hand scan code is returned.
            /// If there is no translation, the function returns 0.
            /// </summary>
            MAPVK_VK_TO_VSC = 0x00,

            /// <summary>
            /// uCode is a scan code and is translated into a virtual-key code that
            /// does not distinguish between left- and right-hand keys. If there is no
            /// translation, the function returns 0.
            /// </summary>
            MAPVK_VSC_TO_VK = 0x01,

            /// <summary>
            /// uCode is a virtual-key code and is translated into an unshifted
            /// character value in the low-order word of the return value. Dead keys (diacritics)
            /// are indicated by setting the top bit of the return value. If there is no
            /// translation, the function returns 0.
            /// </summary>
            MAPVK_VK_TO_CHAR = 0x02,

            /// <summary>
            /// Windows NT/2000/XP: uCode is a scan code and is translated into a
            /// virtual-key code that distinguishes between left- and right-hand keys. If
            /// there is no translation, the function returns 0.
            /// </summary>
            MAPVK_VSC_TO_VK_EX = 0x03,

            /// <summary>
            /// Not currently documented
            /// </summary>
            MAPVK_VK_TO_VSC_EX = 0x04
        }

        const int KL_NAMELENGTH = 9;
        const int KLF_ACTIVATE  = 0x00000001;

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll")]
        public static extern uint MapVirtualKeyEx(uint uCode, MapVirtualKeyMapTypes uMapType, IntPtr dwhkl);
      
        [DllImportAttribute("user32.dll")]        
        public static extern int GetKeyboardState(byte[] pbKeyState);

        [DllImport("user32.dll")]
        public static extern int ToUnicode(uint virtualKeyCode, uint scanCode, byte[] keyboardState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeConst = 64)] StringBuilder receivingBuffer, int bufferSize, uint flags);

        public static string GetCharsFromKeys(Keys key, bool shift, bool altGr)
        {
            var buf = new StringBuilder(256);
            var keyboardState = new byte[256];
            if (shift)
            {
                keyboardState[(int)Keys.ShiftKey] = 0xff;
            }
            if (altGr)
            {
                keyboardState[(int)Keys.ControlKey] = 0xff;
                keyboardState[(int)Keys.Menu]       = 0xff;
            }
            ToUnicode((uint)key, 0, keyboardState, buf, 256, 0);
            return buf.ToString();
        }

        public int GetCodepage()
        {
            return (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_GETCODEPAGE, 0, 0);
        }

        public Encoding Encoding
        {
            get
            {
                // Should always be UTF-8 unless someone has done an end run around us
                int codePage = GetCodepage();
                return (codePage == 0 ? Encoding.Default : Encoding.GetEncoding(codePage));
            }
        }

        private unsafe byte[] GetBytes(string text, Encoding encoding, bool zeroTerminated)
        {
            if (string.IsNullOrEmpty(text))
            {
                return (zeroTerminated ? new byte[] { 0 } : new byte[0]);
            }

            int count = encoding.GetByteCount(text);
            byte[] buffer = new byte[count + (zeroTerminated ? 1 : 0)];

            fixed (byte* bp = buffer)
            fixed (char* ch = text)
            {
                encoding.GetBytes(ch, text.Length, bp, count);
            }

            if (zeroTerminated)
            {
                buffer[buffer.Length - 1] = 0;
            }

            return buffer;
        }

        private bool IsLineVisible(int firstLine, int lastLine, int line)
        {
            return (line >= firstLine && line <= lastLine);
        }

        public int NumberOfOpenFiles 
        { 
            get
            {
                return (int)_win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, (int)NppMsg.ALL_OPEN_FILES);
            }
        }

        public int NumberOfOpenFilesInPrimaryView 
        { 
            get
            {
                return (int)_win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, (int)NppMsg.PRIMARY_VIEW);
            }
        }

        public int NumberOfOpenFilesInSecondaryView
        { 
            get
            {
                return (int)_win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, (int)NppMsg.SECOND_VIEW);
            }
        }

        public string[] GetOpenFiles(IntPtr scintilla)
        {
            string[] aFileList = null;
            NppMsg view = NppMsg.ALL_OPEN_FILES;
            if(scintilla == MainScintilla)
            {
                view = NppMsg.PRIMARY_VIEW;
            }
            else if(scintilla == SecondaryScintilla)
            {
                view = NppMsg.SECOND_VIEW;
            }
            switch (view)
            {
                case NppMsg.PRIMARY_VIEW:
                    _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMESPRIMARY, out  aFileList, NumberOfOpenFilesInPrimaryView);
                    break;
                case NppMsg.SECOND_VIEW:
                    _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMESSECOND, out  aFileList, NumberOfOpenFilesInSecondaryView);
                    break;
                case NppMsg.ALL_OPEN_FILES:
                default:
                    _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMES, out aFileList, NumberOfOpenFiles);
                    break;
            }
            return aFileList;
        }

        public void ActivateDoc(int view, int index)
        {
            _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_ACTIVATEDOC, view, index);
        }

        public void FindActiveBufferViewAndIndex(out NppMsg view, out int index)
        {
            var activeBuffer  = GetCurrentFilePath();
            var mainViewFiles = GetOpenFiles(MainScintilla);
            var subViewFiles  = GetOpenFiles(SecondaryScintilla);
            view  = NppMsg.MAIN_VIEW;
            index = -1;
            for(int i = 0; i < mainViewFiles.Length; ++i)
            {
                if (mainViewFiles[i].Equals(activeBuffer))
                {
                    view = NppMsg.MAIN_VIEW;
                    index = i;
                    return;
                }
            }
            for (int i = 0; i < subViewFiles.Length; ++i)
            {
                if (subViewFiles[i].Equals(activeBuffer))
                {
                    view = NppMsg.SUB_VIEW;
                    index = i;
                    break;
                }
            }
        }

        public string GetPathFromBufferId(int bufferid)
        {
            int filePathLength = (int)_win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETFULLPATHFROMBUFFERID, bufferid, IntPtr.Zero);
            StringBuilder aFilePath = new StringBuilder(filePathLength + 1);
            _win32.ISendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETFULLPATHFROMBUFFERID, bufferid, aFilePath);
            return aFilePath.ToString();
        }

        public IntPtr FindScintillaFromFilepath(string filepath)
        {
            var mainViewFiles = GetOpenFiles(MainScintilla);
            var subViewFiles  = GetOpenFiles(SecondaryScintilla);

            for (int i = 0; i < mainViewFiles.Length; ++i)
            {
                if (mainViewFiles[i].Equals(filepath))
                {
                    return MainScintilla;
                }
            }
            for (int i = 0; i < subViewFiles.Length; ++i)
            {
                if (subViewFiles[i].Equals(filepath))
                {
                    return SecondaryScintilla;
                }
            }
            return IntPtr.Zero;
        }

        public void ClearAllTextMargins(IntPtr sciPtr)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_MARGINTEXTCLEARALL, 0, 0);
        }

        public void SetMarginText(IntPtr sciPtr, int line, string text)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_MARGINSETTEXT, line, text);
        }

        public void SetMarginStyle(IntPtr sciPtr, int line, int style)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_MARGINSETSTYLE, line, style);
        }

        public void SetMarginWidthN(IntPtr sciPtr, int margin, int pixelWidth)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_SETMARGINWIDTHN, margin, pixelWidth);
        }

        public void SetMarginTypeN(IntPtr sciPtr, int margin, SciMsg iType)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_SETMARGINTYPEN, margin, (int)iType);
        }

        public int GetMarginTypeN(IntPtr sciPtr, int margin)
        {
            return (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_GETMARGINTYPEN, margin, 0);
        }

        public int GetMarginWidthN(IntPtr sciPtr, int margin)
        {
           return (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_GETMARGINWIDTHN, margin, 0);
        }

        public int GetMarginMaskN(IntPtr sciPtr, int margin)
        {
            return (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_GETMARGINMASKN, margin, 0);
        }

        public int SetMarginMaskN(IntPtr sciPtr, int margin, int mask)
        {
            return (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_SETMARGINMASKN, margin, mask);
        }

        public int GetStyleBackground(IntPtr sciPtr, int styleNumber)
        {
            return (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_STYLEGETBACK, styleNumber, 0);
        }

        public void SetStyleBackground(IntPtr sciPtr, int styleNumber, int background)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_STYLESETBACK, styleNumber, background);
        }

        public int GetStyleForeground(IntPtr sciPtr, int styleNumber)
        {
            return (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_STYLEGETFORE, styleNumber, 0);
        }

        public void ClearAllIndicators(IntPtr sciPtr, int indicator)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_SETINDICATORCURRENT, indicator, 0);
            //get final position in document
            int endPos = (int)_win32.ISendMessage(sciPtr, SciMsg.SCI_GETLINEENDPOSITION, GetLineCount(sciPtr), 0);
            //clear all indicators
            _win32.ISendMessage(sciPtr, SciMsg.SCI_INDICATORCLEARRANGE, 0, endPos);
        }

        public void SetCurrentIndicator(IntPtr sciPtr, int index)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_SETINDICATORCURRENT, index, 0);
        }

        public void IndicatorFillRange(IntPtr sciPtr, int startPos, int length)
        {
            _win32.ISendMessage(sciPtr, SciMsg.SCI_INDICATORFILLRANGE, startPos, length);
        }

        #region [Helpers]

        private StringBuilder GetLineAsStringBuilder(int line, IntPtr sciPtr)
        {
            int length    = (int)_win32.ISendMessage(CurrentScintilla, SciMsg.SCI_LINELENGTH, line, 0);
            var buffer    = new StringBuilder(length + 1);
            _win32.ISendMessage(sciPtr, SciMsg.SCI_GETLINE, line, buffer);
            buffer.Length = length; //NPP may inject some rubbish at the end of the line
            return buffer;
        }

        #endregion
    }
}