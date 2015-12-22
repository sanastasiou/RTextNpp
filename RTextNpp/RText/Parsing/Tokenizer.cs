using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Scintilla;
namespace RTextNppPlugin.RText.Parsing
{
    public class Tokenizer
    {
        public struct TokenTag : IEquatable<TokenTag>
        {
            internal RTextTokenTypes Type { get; set; }
            internal string Context { get; set; }
            internal int Line { get; set; }
            internal int StartColumn { get; set; }
            internal int EndColumn { get; set; }
            /**
             * \brief   Gets or sets the buffer position.
             *
             * \return  The buffer position at the start of the token. End position can be found by adding the length of the context to it.
             */
            internal int BufferPosition { get; set; }
            public override string ToString()
            {
                return String.Format("Token : {0}\nLine : {1}\nStart column : {2}\nEnd column : {3}\nCaret position at start : {4}\nType : {5}",
                                      Context,
                                      Line,
                                      StartColumn,
                                      EndColumn,
                                      BufferPosition,
                                      Type
                                    );
            }
            internal bool CanTokenHaveReference()
            {
                return (Type == RTextTokenTypes.Reference ||
                        Type == RTextTokenTypes.Identifier);
            }
            internal int EndPosition
            {
                get
                {
                    return (BufferPosition + (EndColumn - StartColumn));
                }
            }
            #region IEquatable<TokenTag> Members
            public bool Equals(TokenTag other)
            {
                return Type           == other.Type           &&
                       BufferPosition == other.BufferPosition &&
                       Context        == other.Context        &&
                       EndColumn      == other.EndColumn      &&
                       Line           == other.Line           &&
                       StartColumn    == other.StartColumn;
            }
            #endregion
        }
        #region[Interface]
        internal static Tokenizer.TokenTag FindTokenUnderCursor(INpp nppHelper, IntPtr sciPtr)
        {
            int aBufferPosition = nppHelper.GetPositionFromMouseLocation();
            if (aBufferPosition != -1)
            {
                int aCurrentLine  = nppHelper.GetLineNumber(aBufferPosition);
                bool aIsExtended  = IsLineExtended(aCurrentLine, nppHelper, sciPtr);
                Tokenizer aTokenizer = new Tokenizer(aCurrentLine, nppHelper.GetLineStart(aCurrentLine), nppHelper, sciPtr, aIsExtended);
                foreach (var t in aTokenizer.Tokenize())
                {
                    if (t.BufferPosition <= aBufferPosition && t.EndPosition >= aBufferPosition)
                    {
                        return t;
                    }
                }
            }
            return default(Tokenizer.TokenTag);
        }

        internal static bool IsLineExtended(int currentLine, INpp nppHelper, IntPtr sciPtr)
        {
            if (currentLine <= 0)
            {
                return false;
            }
            else
            {
                //get previous line - if Scintilla loses focus we have an endless loop -> stack overflow think of a way to fix this...
                string aline = nppHelper.GetLine(--currentLine, sciPtr);
                if (String.IsNullOrWhiteSpace(aline))
                {
                    return (IsLineExtended(currentLine, nppHelper, sciPtr));
                }
                else
                {
                    char c = aline.TrimEnd().Last();
                    return (c == ',' || c == '[' || c == '\\');
                }
            }
        }

        internal Tokenizer(int line, int startPosition, INpp nppHelper, IntPtr sciPtr, bool isExtended = false)
        {
            _lineNumber     = line;
            _lineText       = new StringBuilder(nppHelper.GetLine(_lineNumber, sciPtr));
            _startPosition  = startPosition;
            _isLineExtended = isExtended;
        }

        internal Tokenizer(int line, int startPosition, string text, bool isExtended = false)
        {
            _lineNumber     = line;
            _lineText       = new StringBuilder(text);
            _startPosition  = startPosition;
            _isLineExtended = isExtended;
        }

        internal IEnumerable<TokenTag> Tokenize(params RTextTokenTypes[] typesToKeep)
        {
            bool aFirstToken = true;
            //column in RText protocol starts at 1
            int aColumn = 0;
            while (_lineText.Length > 0)
            {
                foreach (var type in RTextRegexMap.REGEX_MAP.Keys)
                {
                    Match aMatch = RTextRegexMap.REGEX_MAP[type].Match(_lineText.ToString());
                    if (aMatch.Success)
                    {
                        if (typesToKeep.Count() == 0 || typesToKeep.Contains(type))
                        {
                            TokenTag aCurrentTag = new TokenTag
                            {
                                Line           = _lineNumber,
                                Context        = aMatch.Value,
                                StartColumn    = aColumn + aMatch.Index,
                                EndColumn      = aColumn + aMatch.Length,
                                BufferPosition = _startPosition + aColumn,
                                Type           = type
                            };
                            //special case for identifier
                            if (type == RTextTokenTypes.Label)
                            {
                                aFirstToken = false;
                            }
                            else if (type == RTextTokenTypes.Identifier)
                            {
                                if (aFirstToken && !_isLineExtended)
                                {
                                    aCurrentTag.Type = RTextTokenTypes.Command;
                                    aFirstToken = false;
                                }
                            }
                            yield return aCurrentTag;
                        }
                        aColumn += aMatch.Length;
                        _lineText.Remove(0, aMatch.Length);
                        break;
                    }
                }
            }
            yield break;
        }
        #endregion

        #region[Data Members]
        private StringBuilder _lineText     = null;    //!< Line to tokenize.
        private readonly int _lineNumber    = 0;       //!< Line number.
        private readonly int _startPosition = 0;       //!< Starting position.
        private readonly bool _isLineExtended = false; //!< Indicates if the line to be tokenized is an extended line.
        #endregion
    }
}