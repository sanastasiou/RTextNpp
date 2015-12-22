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
    using System.Diagnostics;
    public class Npp : INpp
    {
        #region [Singleton Data Members]
        private static volatile Npp instance                    = null;
        private static object syncRoot                          = new Object();
        private IntPtr _scintillaMainNativePtr                  = IntPtr.Zero;
        private IntPtr _scintillaSubNativePtr                   = IntPtr.Zero;
        private static Scintilla_DirectFunction _directFunction = null;
        private readonly INativeHelpers _nativeHelpers          = new NativeHelpers();

        #endregion

        private Npp()
        {
        }
        
        static public Npp Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new Npp();
                        }
                    }
                }
                return instance;
            }
        }

        public void InitializeNativePointers()
        {
            _scintillaMainNativePtr      = _nativeHelpers.ISendMessage(MainScintilla, (int)SciMsg.SCI_GETDIRECTPOINTER, IntPtr.Zero, IntPtr.Zero);
            _scintillaSubNativePtr       = _nativeHelpers.ISendMessage(SecondaryScintilla, (int)SciMsg.SCI_GETDIRECTPOINTER, IntPtr.Zero, IntPtr.Zero);
            IntPtr directFunctionPointer = _nativeHelpers.ISendMessage(MainScintilla, (int)SciMsg.SCI_GETDIRECTFUNCTION, IntPtr.Zero, IntPtr.Zero);
            _directFunction              = (Scintilla_DirectFunction)Marshal.GetDelegateForFunctionPointer(directFunctionPointer,typeof(Scintilla_DirectFunction));
        }

        public unsafe void AddAnnotation(int line, System.Text.StringBuilder errorDescription)
        {
            if (errorDescription.Length == 0)
            {
                // Scintilla docs suggest that setting to NULL rather than an empty string will free memory
                SendMessage( CurrentScintilla, SciMsg.SCI_ANNOTATIONGETTEXT, new IntPtr(line), IntPtr.Zero);
            }
            else
            {
                var bytes = GetBytes(errorDescription.ToString(), Encoding, zeroTerminated: true);
                fixed (byte* bp = bytes)
                {
                    SendMessage(CurrentScintilla, SciMsg.SCI_ANNOTATIONSETTEXT, new IntPtr(line), new IntPtr(bp));
                }
            }
        }

        public void SetAnnotationVisible(IntPtr handle, int annotationStyle)
        {
            SendMessage(handle, SciMsg.SCI_ANNOTATIONSETVISIBLE, new IntPtr(annotationStyle));
        }

        public void SetAnnotationStyle(int line, int annotationStyle)
        {
            SendMessage(CurrentScintilla, SciMsg.SCI_ANNOTATIONSETSTYLE, new IntPtr(line), new IntPtr(annotationStyle));
        }

        public unsafe void SetAnnotationStyles(int line, byte [] styleDescriptions)
        {
            var length = SendMessage(CurrentScintilla, SciMsg.SCI_ANNOTATIONGETTEXT, new IntPtr(line)).ToInt32();
            if (length == 0)
            {
                return;
            }

            var text = new byte[length + 1];
            fixed (byte* textPtr = text)
            {
                SendMessage(CurrentScintilla, SciMsg.SCI_ANNOTATIONGETTEXT, new IntPtr(line), new IntPtr(textPtr));

                var styles = CharToByteStyles(styleDescriptions ?? new byte[0], textPtr, length, Encoding);
                fixed (byte* stylePtr = styles)
                {
                    SendMessage(CurrentScintilla, SciMsg.SCI_ANNOTATIONSETSTYLES, new IntPtr(line), new IntPtr(stylePtr));
                }
            }
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
                _nativeHelpers.ISendMessage(Plugin.Instance.NppData._nppHandle, (int)NppMsg.NPPM_GETCURRENTSCINTILLA, 0, out curScintilla);
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
            return SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETCURRENTDOCINDEX, new IntPtr(0), new IntPtr((int)(scintilla == MainScintilla ? NppMsg.MAIN_VIEW : NppMsg.SUB_VIEW))).ToInt32();
        }
        
        public void SetEditorFocus(int setFocus = 1)
        {
            SendMessage(CurrentScintilla, SciMsg.SCI_SETFOCUS, new IntPtr(setFocus));
        }
        
        public int GetZoomLevel(IntPtr sciPtr)
        {
            return SendMessage(sciPtr, SciMsg.SCI_GETZOOM).ToInt32();
        }
        
        public int GetSelectionStart()
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_GETSELECTIONNSTART).ToInt32();
        }
        
        public int GetSelectionLength()
        {
            int aSelStart = SendMessage(CurrentScintilla, SciMsg.SCI_GETSELECTIONNSTART).ToInt32();
            int aSelEnd   = SendMessage(CurrentScintilla, SciMsg.SCI_GETSELECTIONNEND).ToInt32();
            if (aSelStart == aSelEnd)
            {
                return 1;
            }
            return aSelEnd - aSelStart;
        }
        
        public int GetSelections()
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_GETSELECTIONS).ToInt32();
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
                SendMessage(CurrentScintilla, SciMsg.SCI_DELETEBACK);
            }
        }

        public void ClearAllAnnotations(IntPtr sciPtr)
        {
            SendMessage(sciPtr, SciMsg.SCI_ANNOTATIONCLEARALL);
        }
        
        public void DeleteRange(int position, int length)
        {
            SendMessage(CurrentScintilla, SciMsg.SCI_DELETERANGE, new IntPtr(position), new IntPtr(length));
        }
        
        /**
         * \brief   Gets current file path.
         *
         * \return  The file path of the currently viewed document.
         */        
        public string GetCurrentFilePath()
        {
            StringBuilder path = new StringBuilder(Constants.WIN_32.MAX_PATH);
            SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETFULLCURRENTPATH, 0, path);
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
            return (SendMessage(CurrentScintilla, SciMsg.SCI_GETMODIFY).ToInt32() != 0);
        }
        
        /**
         * Saves the currently viewed file.
         *
         * \param   file    The file.
         */
        public unsafe void SaveFile(string file)
        {
            SwitchToFile(file);
            SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_SAVECURRENTFILE);
        }
        
        /**
         * Switches active view to file.
         *
         * \param   file    The file.
         */
        public unsafe void SwitchToFile(string file)
        {
            fixed (byte* bp = GetBytes(file, Encoding.Unicode, zeroTerminated: true))
            {
                SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, IntPtr.Zero, new IntPtr(bp));
            }
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
                SendMessage(CurrentScintilla, SciMsg.SCI_ADDTEXT, new IntPtr(bytes.Length), new IntPtr(bp));
            }
        }

        public void ChangeMenuItemCheck(int CmdId, bool isChecked)
        {
            SendMessage(instance.NppHandle, NppMsg.NPPM_SETMENUITEMCHECK, new IntPtr(CmdId), new IntPtr(isChecked ? 1 : 0));
        }
               
        public void SaveCurrentFile()
        {
            SendMessage(instance.NppHandle, NppMsg.NPPM_SAVECURRENTFILE);
        }
        
        public void SetIndicatorStyle(IntPtr sciPtr, int indicator, SciMsg style, Color color)
        {
            SendMessage(sciPtr, SciMsg.SCI_INDICSETSTYLE, new IntPtr(indicator), new IntPtr((int)style));
            SendMessage(sciPtr, SciMsg.SCI_INDICSETFORE,  new IntPtr(indicator),  new IntPtr(ColorTranslator.ToWin32(color)));
        }

        public void ClearIndicator(IntPtr sciPtr, int indicator, int startPos, int length)
        {
            SendMessage(sciPtr, SciMsg.SCI_SETINDICATORCURRENT,  new IntPtr(indicator));
            SendMessage(sciPtr, SciMsg.SCI_INDICATORCLEARRANGE,  new IntPtr(startPos),  new IntPtr(length));
        }
        
        public void PlaceIndicator(IntPtr sciPtr, int startPos, int length)
        {
            SendMessage(sciPtr, SciMsg.SCI_INDICATORFILLRANGE, new IntPtr(startPos), new IntPtr(length));
        }
        
        public string GetConfigDir()
        {
            var buffer = new StringBuilder(Constants.WIN_32.MAX_PATH);
            SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Constants.WIN_32.MAX_PATH, buffer);
            return buffer.ToString();
        }
        
        public IList<Tuple<int, int>> FindIndicatorRanges(int indicator, IntPtr sciPtr)
        {
            var ranges       = new List<Tuple<int, int>>();
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
                int rangeStart = SendMessage(sciPtr, SciMsg.SCI_INDICATORSTART, new IntPtr(indicator), new IntPtr(testPosition)).ToInt32();
                int rangeEnd   = SendMessage(sciPtr, SciMsg.SCI_INDICATOREND, new IntPtr(indicator), new IntPtr(testPosition)).ToInt32();
                int value      = SendMessage(sciPtr, SciMsg.SCI_INDICATORVALUEAT, new IntPtr(indicator), new IntPtr(testPosition)).ToInt32();
                if (value == 1) //indicator is present
                {
                    ranges.Add(new Tuple<int, int>(rangeStart, rangeEnd));
                }
                if (testPosition == rangeEnd)
                {
                    break;
                }
                testPosition = rangeEnd;
            }
            return ranges;
        }

        public int IndicatorStart(IntPtr sciPtr, int indicator, int testPosition)
        {
            return SendMessage(sciPtr, SciMsg.SCI_INDICATORSTART, new IntPtr(indicator), new IntPtr(testPosition)).ToInt32();
            
        }

        public int IndicatorEnd(IntPtr sciPtr, int indicator, int testPosition)
        {
            return SendMessage(sciPtr, SciMsg.SCI_INDICATOREND, new IntPtr(indicator), new IntPtr(testPosition)).ToInt32();
        }
        
        public unsafe string GetLine(int line, IntPtr sciPtr)
        {
            int length = SendMessage(sciPtr, SciMsg.SCI_LINELENGTH, new IntPtr(line)).ToInt32();
            var bytes = new byte[length];

            fixed (byte* ptr = bytes)
            {
                SendMessage(sciPtr, SciMsg.SCI_GETLINE, new IntPtr(line), new IntPtr(ptr));
            }

            return Encoding.GetString(bytes);
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
            return SendMessage(CurrentScintilla, SciMsg.SCI_LINEFROMPOSITION, new IntPtr(position)).ToInt32();
        }
        
        public int GetLengthToEndOfLine(int line, int position)
        {
            return (SendMessage(CurrentScintilla, SciMsg.SCI_GETLINEENDPOSITION, new IntPtr(line)).ToInt32() - position);
        }
               
        public int GetColumn(int position)
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_GETCOLUMN, new IntPtr(position)).ToInt32();
        }
        
        public int GetColumn()
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_GETCOLUMN, new IntPtr(GetCaretPosition())).ToInt32();
        }
        
        public int GetLineEnd(int position, int line)
        {
            return GetLineStart(GetLineNumber(position)) + SendMessage(CurrentScintilla, SciMsg.SCI_LINELENGTH, new IntPtr(line)).ToInt32();
        }
        
        public int GetLineStart(int line)
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_POSITIONFROMLINE, new IntPtr(line)).ToInt32();
        }
        
        public void SetFirstVisibleLine(int line)
        {
            SendMessage(CurrentScintilla, SciMsg.SCI_SETFIRSTVISIBLELINE, new IntPtr(line));
        }
        
        public int GetLineCount(IntPtr sciPtr)
        {
            return SendMessage(sciPtr, SciMsg.SCI_GETLINECOUNT).ToInt32();
        }
        
        public string GetShortcutsFile()
        {
            return Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(GetConfigDir())), Constants.Scintilla.SHORTCUTS_FILE);
        }

        /**
         * Gets caret screen location relative to buffer position.
         *
         * \param   position    The buffer position.
         *
         * \return  A point from the relative buffer position.
         */
        public Point GetCaretScreenLocationRelativeToPosition(int position)
        {
            int x           = SendMessage(CurrentScintilla, SciMsg.SCI_POINTXFROMPOSITION, IntPtr.Zero, new IntPtr(position)).ToInt32();
            int y           = SendMessage(CurrentScintilla, SciMsg.SCI_POINTYFROMPOSITION, IntPtr.Zero, new IntPtr(position)).ToInt32();
            Point aPoint    = new Point(x, y);
            _nativeHelpers.IClientToScreen(CurrentScintilla, ref aPoint);
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
            int x           = SendMessage(CurrentScintilla, SciMsg.SCI_POINTXFROMPOSITION, IntPtr.Zero, new IntPtr(position)).ToInt32();
            int y           = SendMessage(CurrentScintilla, SciMsg.SCI_POINTYFROMPOSITION, IntPtr.Zero, new IntPtr(position)).ToInt32();
            Point aPoint    = new Point(x, y);
            _nativeHelpers.IClientToScreen(CurrentScintilla, ref aPoint);
            double dpiScale = VisualUtilities.GetDpiScalingFactor();
            aPoint.X        = (int)((double)(aPoint.X) / dpiScale);
            aPoint.Y        = (int)((double)(aPoint.Y) / dpiScale);
            return aPoint;
        }
        
        public Point GetCaretScreenLocation()
        {
            int pos = SendMessage(CurrentScintilla, SciMsg.SCI_GETCURRENTPOS).ToInt32();
            return GetCaretScreenLocationForFormAboveWord(pos);
        }
        
        public int GetPositionFromMouseLocation()
        {
            Point point = Cursor.Position;
            _nativeHelpers.IScreenToClient(CurrentScintilla, ref point);
            //dpi conversion here?
            return SendMessage(CurrentScintilla, SciMsg.SCI_CHARPOSITIONFROMPOINTCLOSE, new IntPtr(point.X), new IntPtr(point.Y)).ToInt32();
        }
               
        public string GetTextBetween(int start, int end = -1)
        {
            if (end == -1)
            {
                end = SendMessage(CurrentScintilla, SciMsg.SCI_GETLENGTH).ToInt32();
            }
            using (var tr = new Sci_TextRange(start, end, end - start + 1)) //+1 for null termination
            {
                SendMessage(CurrentScintilla, SciMsg.SCI_GETTEXTRANGE, IntPtr.Zero, tr.NativePointer);
                return tr.lpstrText;
            }
        }
        
        public unsafe void ReplaceWordFromToken(Tokenizer.TokenTag ? token, string insertionText)
        {
            int aCaretPos           = GetCaretPosition();
            bool isCaretInsideToken = (aCaretPos >= token.Value.BufferPosition && aCaretPos < (token.Value.BufferPosition + token.Value.Context.Length));
            if (token.HasValue && 
                (token.Value.Type != RTextTokenTypes.Space && 
                 token.Value.Type != RTextTokenTypes.Comma && 
                 token.Value.Type != RTextTokenTypes.Label && 
                 token.Value.Type != RTextTokenTypes.NewLine) || 
               (token.Value.Type == RTextTokenTypes.Label && isCaretInsideToken))
            {
                //if token is space or comma or label, add the new text after it!
                SendMessage(CurrentScintilla, SciMsg.SCI_SETSELECTION, new IntPtr(token.Value.BufferPosition), new IntPtr(token.Value.BufferPosition + token.Value.Context.Length));
            }
            else
            {
                SendMessage(CurrentScintilla, SciMsg.SCI_SETSELECTION, new IntPtr(aCaretPos), new IntPtr(aCaretPos));
            }

            var bytes = GetBytes(insertionText ?? string.Empty, Encoding, zeroTerminated: false);
            fixed (byte* bp = bytes)
            {
                SendMessage(CurrentScintilla, SciMsg.SCI_REPLACESEL, new IntPtr(bytes.Length), new IntPtr(bp));
            }
        }
               
        public IntPtr NppHandle
        {
            get { return Plugin.Instance.NppData._nppHandle; }
        }
        
        public int GetCaretPosition()
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_GETCURRENTPOS).ToInt32();
        }
        
        public int GetCaretLineNumber()
        {
            int currentPos = SendMessage(CurrentScintilla, SciMsg.SCI_GETCURRENTPOS).ToInt32();
            return SendMessage(CurrentScintilla, SciMsg.SCI_LINEFROMPOSITION, new IntPtr(currentPos)).ToInt32();
        }
        
        public void SetCaretPosition(int pos)
        {
            SendMessage(CurrentScintilla, SciMsg.SCI_SETCURRENTPOS, new IntPtr(pos));
        }
        
        public void ClearSelection()
        {
            SendMessage(CurrentScintilla, SciMsg.SCI_CLEARSELECTIONS);
        }
        
        public void SetSelection(int start, int end)
        {
            SendMessage(CurrentScintilla, SciMsg.SCI_SETSELECTIONSTART, new IntPtr(start));
            SendMessage(CurrentScintilla, SciMsg.SCI_SETSELECTIONEND, new IntPtr(end));
        }
        
        public int GrabFocus(IntPtr sciPtr)
        {
            int currentPos = SendMessage(sciPtr, SciMsg.SCI_GRABFOCUS).ToInt32();
            return currentPos;
        }
        
        public void ScrollToCaret()
        {
            SendMessage(CurrentScintilla, SciMsg.SCI_SCROLLCARET);
            SendMessage(CurrentScintilla, SciMsg.SCI_LINESCROLL, IntPtr.Zero, new IntPtr(1)); //bottom scrollbar can hide the line
            SendMessage(CurrentScintilla, SciMsg.SCI_SCROLLCARET);
        }
        
        public unsafe void OpenFile(string file)
        {
            fixed (byte* bp = GetBytes(file, Encoding.Unicode, zeroTerminated: true))
            {
                SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_DOOPEN, IntPtr.Zero, new IntPtr(bp));
            }
        }

        public int GetFirstVisibleLine(IntPtr sciPtr)
        {
            //this is the first "visible" line on screen - it may differ from the actual doc line
            int firstVisibleLine = SendMessage(sciPtr, SciMsg.SCI_GETFIRSTVISIBLELINE).ToInt32();
            return SendMessage(sciPtr, SciMsg.SCI_DOCLINEFROMVISIBLE, new IntPtr(firstVisibleLine)).ToInt32() + 1;
        }

        public int GetLastVisibleLine(IntPtr sciPtr)
        { 
            int firstVisibleLine = SendMessage(sciPtr, SciMsg.SCI_GETFIRSTVISIBLELINE).ToInt32();
            int firstLine        = SendMessage(sciPtr, SciMsg.SCI_DOCLINEFROMVISIBLE, new IntPtr(firstVisibleLine)).ToInt32();
            return firstLine + GetLinesOnScreen(sciPtr);
        }

        public int GetLinesOnScreen(IntPtr sciPtr)
        {
            return SendMessage(sciPtr, SciMsg.SCI_LINESONSCREEN).ToInt32();
        }

        public void GoToLine(int line)
        {
            int firstVisibleDocLine = GetFirstVisibleLine(CurrentScintilla);
            int lastVisibleDocLine  = GetLastVisibleLine(CurrentScintilla);
            if(IsLineVisible(firstVisibleDocLine, lastVisibleDocLine, line))
            {
                //just move cursor, line is already visible
                SendMessage(CurrentScintilla, SciMsg.SCI_GOTOPOS, new IntPtr(GetLineStart(line - 1)));
            }
            else
            {
                //line is not visible
                //move line in the middle of the screen
                int linesOnScreen = SendMessage(CurrentScintilla, SciMsg.SCI_LINESONSCREEN).ToInt32();
                int offset        = linesOnScreen >> 1;
                //check if we are behind new line, or after new line
                int currentLine = GetLineNumber();
                if(currentLine > line)
                {
                    offset = -offset;
                }
                SendMessage(CurrentScintilla, SciMsg.SCI_ENSUREVISIBLE, new IntPtr(line - 1));
                SendMessage(CurrentScintilla, SciMsg.SCI_GOTOLINE, new IntPtr(line - 1));
                SendMessage(CurrentScintilla, SciMsg.SCI_LINESCROLL, IntPtr.Zero, new IntPtr(offset));
            }
        }
        
        /**
         * \brief   Scroll up to line. Makes "line" the first visible line of the document, if possible by scrolling up, else has no effect.
         *
         * \param   line    The line.
         */
        public bool IsLineVisible(int line)
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_GETLINEVISIBLE,new IntPtr(line)).ToInt32() == 1;
        }
              
        /// <summary>
        /// Retrieve the height of a particular line of text in pixels.
        /// </summary>
        public int GetTextHeight(int line)
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_TEXTHEIGHT, new IntPtr(line)).ToInt32();
        }

        public int GetCodepage()
        {
            return SendMessage(CurrentScintilla, SciMsg.SCI_GETCODEPAGE).ToInt32();
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

        public int NumberOfOpenFiles 
        { 
            get
            {
                return SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, IntPtr.Zero, new IntPtr((int)NppMsg.ALL_OPEN_FILES)).ToInt32();
            }
        }

        public int NumberOfOpenFilesInPrimaryView 
        { 
            get
            {
                return SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, IntPtr.Zero, new IntPtr((int)NppMsg.PRIMARY_VIEW)).ToInt32();
            }
        }

        public int NumberOfOpenFilesInSecondaryView
        { 
            get
            {
                return SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, IntPtr.Zero, new IntPtr((int)NppMsg.SECOND_VIEW)).ToInt32();
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
                    SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMESPRIMARY, out aFileList, NumberOfOpenFilesInPrimaryView);
                    break;
                case NppMsg.SECOND_VIEW:
                    SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMESSECOND, out aFileList, NumberOfOpenFilesInSecondaryView);
                    break;
                case NppMsg.ALL_OPEN_FILES:
                default:
                    SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMES, out aFileList, NumberOfOpenFiles);
                    break;
            }
            return aFileList;
        }

        public void ActivateDoc(int view, int index)
        {
            SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_ACTIVATEDOC, new IntPtr(view), new IntPtr(index));
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
            int filePathLength      = SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETFULLPATHFROMBUFFERID, new IntPtr(bufferid), IntPtr.Zero).ToInt32();
            StringBuilder aFilePath = new StringBuilder(filePathLength + 1);
            SendMessage(Plugin.Instance.NppData._nppHandle, NppMsg.NPPM_GETFULLPATHFROMBUFFERID, bufferid, aFilePath);
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
            SendMessage(sciPtr, SciMsg.SCI_MARGINTEXTCLEARALL);
        }

        public unsafe void SetMarginText(IntPtr sciPtr, int line, string text)
        {
            fixed (byte* bp = GetBytes(text, Encoding, zeroTerminated: true))
            {
                SendMessage(sciPtr, SciMsg.SCI_MARGINSETTEXT, new IntPtr(line), new IntPtr(bp));
            }
        }

        public void SetMarginStyle(IntPtr sciPtr, int line, int style)
        {
            SendMessage(sciPtr, SciMsg.SCI_MARGINSETSTYLE, new IntPtr(line), new IntPtr(style));
        }

        public void SetMarginWidthN(IntPtr sciPtr, int margin, int pixelWidth)
        {
            SendMessage(sciPtr, SciMsg.SCI_SETMARGINWIDTHN, new IntPtr(margin), new IntPtr(pixelWidth));
        }

        public void SetMarginTypeN(IntPtr sciPtr, int margin, SciMsg iType)
        {
            SendMessage(sciPtr, SciMsg.SCI_SETMARGINTYPEN, new IntPtr(margin), new IntPtr((int)iType));
        }

        public int GetMarginTypeN(IntPtr sciPtr, int margin)
        {
            return SendMessage(sciPtr, SciMsg.SCI_GETMARGINTYPEN, new IntPtr(margin)).ToInt32();
        }

        public int GetMarginWidthN(IntPtr sciPtr, int margin)
        {
           return SendMessage(sciPtr, SciMsg.SCI_GETMARGINWIDTHN, new IntPtr(margin)).ToInt32();
        }

        public int GetMarginMaskN(IntPtr sciPtr, int margin)
        {
            return SendMessage(sciPtr, SciMsg.SCI_GETMARGINMASKN, new IntPtr(margin)).ToInt32();
        }

        public int SetMarginMaskN(IntPtr sciPtr, int margin, int mask)
        {
            return SendMessage(sciPtr, SciMsg.SCI_SETMARGINMASKN, new IntPtr(margin), new IntPtr(mask)).ToInt32();
        }

        public int GetStyleBackground(IntPtr sciPtr, int styleNumber)
        {
            return SendMessage(sciPtr, SciMsg.SCI_STYLEGETBACK, new IntPtr(styleNumber)).ToInt32();
        }

        public void SetStyleBackground(IntPtr sciPtr, int styleNumber, int background)
        {
            SendMessage(sciPtr, SciMsg.SCI_STYLESETBACK, new IntPtr(styleNumber), new IntPtr(background));
        }

        public int GetStyleForeground(IntPtr sciPtr, int styleNumber)
        {
            return SendMessage(sciPtr, SciMsg.SCI_STYLEGETFORE, new IntPtr(styleNumber)).ToInt32();
        }

        public void SetCurrentIndicator(IntPtr sciPtr, int index)
        {
            SendMessage(sciPtr, SciMsg.SCI_SETINDICATORCURRENT, new IntPtr(index));
        }

        public void SetModEventMask(int eventMask)
        {
            SendMessage(MainScintilla, SciMsg.SCI_SETMODEVENTMASK, new IntPtr(eventMask));
            SendMessage(SecondaryScintilla, SciMsg.SCI_SETMODEVENTMASK, new IntPtr(eventMask));
        }

        public void ClearAllIndicators(IntPtr sciPtr, int currentIndicator)
        {
            SendMessage(sciPtr, SciMsg.SCI_SETINDICATORCURRENT, new IntPtr(currentIndicator));
            int endPos = SendMessage(sciPtr, SciMsg.SCI_GETLINEENDPOSITION, new IntPtr(GetLineCount(sciPtr))).ToInt32();
            SendMessage(sciPtr, SciMsg.SCI_INDICATORCLEARRANGE, IntPtr.Zero, new IntPtr(endPos));
        }

        public string GetActiveFile(IntPtr sciPtr)
        {
            return GetOpenFiles(sciPtr)[CurrentDocIndex(sciPtr)];
        }

        public IntPtr SendMessage(IntPtr hWnd, SciMsg msg, IntPtr wParam = default(IntPtr), IntPtr lParam = default(IntPtr))
        {
            //get native pointer
            //IntPtr nativePtr = (hWnd == MainScintilla) ? _scintillaMainNativePtr : _scintillaSubNativePtr;
            return _nativeHelpers.ISendMessage(hWnd, (int)msg, wParam, lParam);
        }

        public IntPtr SendMessage(IntPtr hWnd, NppMsg msg, IntPtr wParam = default(IntPtr), IntPtr lParam = default(IntPtr))
        {
            return _nativeHelpers.ISendMessage(hWnd, (int)msg, wParam, lParam);
        }
        #region [Helpers]

        private bool IsLineVisible(int firstLine, int lastLine, int line)
        {
            return (line >= firstLine && line <= lastLine);
        }

        private IntPtr SendMessage(IntPtr hWnd, NppMsg msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] StringBuilder lParam)
        {
            return _nativeHelpers.ISendMessage(hWnd, (int)msg, wParam, lParam);
        }

        private int SendMessage(IntPtr hWnd, NppMsg Msg, out string[] files, int numOfFiles)
        {
            string[] fileList = new string[numOfFiles];
            for (int i = 0; i < fileList.Length; ++i)
            {
                fileList[i] = new string('\0', Constants.WIN_32.MAX_PATH);
            }
            var allocatedStrings  = AllocStringArray(fileList);
            int numOfNppOpenFiles = _nativeHelpers.ISendMessage(hWnd, (int)Msg, allocatedStrings, numOfFiles);
            files = new string[numOfFiles];

            for (int i = 0; i < allocatedStrings.Length; ++i)
            {
                files[i] = Marshal.PtrToStringUni(allocatedStrings[i]);
            }

            FreeStringArray(allocatedStrings);

            return numOfNppOpenFiles;
        }

        private IntPtr[] AllocStringArray(string[] vals)
        {
            IntPtr[] ptrs = new IntPtr[vals.Length];

            for (int i = 0; i < vals.Length; i++)
            {
                ptrs[i] = Marshal.StringToHGlobalUni(vals[i]);
            }

            return ptrs;
        }

        private void FreeStringArray(IntPtr[] ptrs)
        {
            for (int i = 0; i < ptrs.Length; i++)
            {
                if (ptrs[i] != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(ptrs[i]);
                    ptrs[i] = IntPtr.Zero;
                }
            }
        }

        private unsafe byte[] CharToByteStyles(byte[] styles, byte* text, int length, Encoding encoding)
        {
            // This is used by annotations and margins to style all the text in one call.
            // It converts an array of styles where each element corresponds to a CHARACTER
            // to an array of styles where each element corresponds to a BYTE.

            var bytePos = 0; // Position within text BYTES and style BYTES (should be the same)
            var charPos = 0; // Position within style CHARACTERS
            var decoder = encoding.GetDecoder();
            var result = new byte[length];

            while (bytePos < length && charPos < styles.Length)
            {
                result[bytePos] = styles[charPos];
                if (decoder.GetCharCount(text + bytePos, 1, false) > 0)
                    charPos++; // Move a char

                bytePos++;
            }

            return result;
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

        #endregion
    }
}