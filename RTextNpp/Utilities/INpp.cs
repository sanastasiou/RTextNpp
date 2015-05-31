using System;
namespace RTextNppPlugin.Utilities
{
    internal interface INpp
    {
        void SwitchToFile(string file);
        void SaveFile(string file);
        bool IsFileModified(string file);
        string GetCurrentFilePath();
        void ChangeMenuItemCheck(int CmdId, bool isChecked);
        void AddText(string s);
        void ClearIndicator(int indicator, int startPos, int endPos);
        void ClearSelection();
        void DeleteBack(int length);
        void DeleteFront();
        void DeleteRange(int position, int length);
        void DisplayInNewDocument(string text);
        void Exit();
        System.Drawing.Point[] FindIndicatorRanges(int indicator);
        char GetAsciiCharacter(int uVirtKey, int uScanCode);
        int GetCaretLineNumber();
        int GetCaretPosition();
        System.Drawing.Point GetCaretScreenLocation();
        System.Drawing.Point GetCaretScreenLocationForForm();
        System.Drawing.Point GetCaretScreenLocationForForm(int position);
        System.Drawing.Point GetCaretScreenLocationForFormAboveWord();
        System.Drawing.Point GetCaretScreenLocationRelativeToPosition(int position);
        System.Drawing.Rectangle GetClientRect();
        System.Drawing.Rectangle GetClientRectFromControl(IntPtr hwnd);
        System.Drawing.Rectangle GetClientRectFromPoint(System.Drawing.Point p);
        int GetColumn();
        string GetConfigDir();
        string GetCurrentFile();
        int GetFirstVisibleLine();
        int GetLengthToEndOfLine(int currentCharacterColumn);
        string GetLine();
        string GetLine(int line);
        System.Text.StringBuilder GetLineAsStringBuilder(int line);
        int GetLineCount();
        int GetLineEnd(int line = -1);
        int GetLineNumber();
        int GetLineNumber(int position);
        int GetLineStart(int line);
        int GetPositionFromMouseLocation();
        int GetSelectionLength();
        int GetSelections();
        int GetSelectionStart();
        string GetShortcutsFile();
        string GetTextBetween(System.Drawing.Point point);
        string GetTextBetween(int start, int end = -1);
        int GetTextHeight(int line);
        int GetZoomLevel();
        void GoToLine(int line);
        int GrabFocus();
        IntPtr NppHandle { get; }
        void OpenFile(string file);
        void PlaceIndicator(int indicator, int startPos, int endPos);
        void ReplaceWordFromToken(RTextNppPlugin.Parsing.Tokenizer.TokenTag? token, string insertionText);
        void SaveCurrentFile();
        void ScrollToCaret();
        void SetCaretPosition(int pos);
        void SetEditorFocus();
        void SetFirstVisibleLine(int line);
        void SetIndicatorStyle(int indicator, RTextNppPlugin.SciMsg style, System.Drawing.Color color);
        void SetSelection(int start, int end);
        void SetTextBetween(string text, System.Drawing.Point point);
        void SetTextBetween(string text, int start, int end = -1);
        string TextAfterCursor(int maxLength);
        string TextAfterPosition(int position, int maxLength);
        string TextBeforeCursor(int maxLength);
        string TextBeforePosition(int position, int maxLength);
    }
}
