using System;
using System.Text;

namespace RTextNppPlugin.Utilities
{
    using RTextNppPlugin.RText.Parsing;
    using RTextNppPlugin.DllExport;
    using System.Text;
    using System.Collections.Generic;

    public enum BufferEncoding : int
    {
        Error         = -1,
        Uni8Bit       = 0,
        UniUTF8       = 1,
        Uni16BE       = 2,
        Uni16LE       = 3,
        UniCookie     = 4,
        Uni7Bit       = 5,
        Uni16BE_NoBOM = 6,
        Uni16LE_NoBOM = 7
    }

    internal interface INpp
    {
        IntPtr CurrentScintilla { get; }

        IntPtr MainScintilla { get; }

        IntPtr SecondaryScintilla { get; }

        int CurrentDocIndex(NppMsg currentView);
        
        void SwitchToFile(string file);
        
        void SaveFile(string file);
        
        bool IsFileModified(string file);
        
        string GetCurrentFilePath();
        
        void ChangeMenuItemCheck(int CmdId, bool isChecked);

        unsafe void AddText(string text);
        
        void ClearIndicator(int indicator, int startPos, int endPos);
        
        void ClearSelection();
        
        void DeleteBack(int length);
        
        void DeleteFront();
        
        void DeleteRange(int position, int length);

        System.Drawing.Point[] FindIndicatorRanges(int indicator);
       
        int GetCaretLineNumber();
        
        int GetCaretPosition();
        
        System.Drawing.Point GetCaretScreenLocation();
        
        System.Drawing.Point GetCaretScreenLocationForForm();
              
        System.Drawing.Point GetCaretScreenLocationForFormAboveWord(int position);
        
        System.Drawing.Point GetCaretScreenLocationRelativeToPosition(int position);
        
        System.Drawing.Rectangle GetClientRect();
        
        System.Drawing.Rectangle GetClientRectFromControl(IntPtr hwnd);
        
        System.Drawing.Rectangle GetClientRectFromPoint(System.Drawing.Point p);
        
        int GetColumn(int position);
        
        int GetColumn();
        
        string GetConfigDir();
        
        string GetCurrentFile();
        
        int GetFirstVisibleLine();
        
        int GetLengthToEndOfLine(int line, int position);
               
        /**
         * \brief   Gets a line from a line number.
         *
         * \param   line    The line number.
         *
         * \return  The line string.
         */
        string GetLine(int line);
        
        System.Text.StringBuilder GetLineAsStringBuilder(int line);
        
        int GetLineCount();
        
        int GetLineEnd(int position, int line);
        
        int GetLineNumber();
        
        int GetLineNumber(int position);
        
        int GetLineStart(int line);
        
        int GetPositionFromMouseLocation();
        
        int GetSelectionLength();
        
        int GetSelections();
        
        int GetSelectionStart();
        
        string GetShortcutsFile();
        
        string GetTextBetween(int start, int end = -1);
        
        int GetTextHeight(int line);
        
        int GetZoomLevel();
        
        void GoToLine(int line);
             
        int GrabFocus();
        
        IntPtr NppHandle { get; }
        
        void OpenFile(string file);
        
        void PlaceIndicator(int indicator, int startPos, int endPos);
        
        unsafe void ReplaceWordFromToken(Tokenizer.TokenTag? token, string insertionText);
        
        void SaveCurrentFile();
        
        void ScrollToCaret();
        
        void SetCaretPosition(int pos);
        
        void SetEditorFocus(int setFocus);
        
        void SetFirstVisibleLine(int line);
        
        void SetIndicatorStyle(int indicator, SciMsg style, System.Drawing.Color color);
        
        void SetSelection(int start, int end);

        object JumpToLine(string file, int line);

        void SetAnnotationVisible(IntPtr handle, int annotationStyle);

        void ClearAllAnnotations(IntPtr sciPtr);

        void AddAnnotation(int line, System.Text.StringBuilder errorDescription);

        void SetAnnotationStyle(int line, int annotationStyle);

        void SetAnnotationStyles(int line, System.Text.StringBuilder stylesDescription);

        int GetCodepage();

        Encoding Encoding { get; }

        int FirstVisibleLine { get; }

        int LastVisibleLine { get; }

        int LinesOnScreen { get; }

        int NumberOfOpenFiles { get; }

        int NumberOfOpenFilesInPrimaryView { get; }

        int NumberOfOpenFilesInSecondaryView { get; }

        string[] GetOpenFiles(NppMsg view);
    }
}