using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using RTextNppPlugin.Utilities;

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

        internal static Tokenizer.TokenTag FindTokenUnderCursor(INpp nppHelper)
        {
            int aBufferPosition = nppHelper.GetPositionFromMouseLocation();
            if (aBufferPosition != -1)
            {
                int aCurrentLine = nppHelper.GetLineNumber(aBufferPosition);
                Tokenizer aTokenizer = new Tokenizer(aCurrentLine, nppHelper);
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

        internal Tokenizer(int line, INpp nppHelper)
        {
            _lineNumber = line;
            _nppHelper  = nppHelper;
            _lineText   = new StringBuilder(_nppHelper.GetLine(_lineNumber));
        }

        internal IEnumerable<TokenTag> Tokenize(params RTextTokenTypes[] typesToKeep)
        {
            int aOffset = _nppHelper.GetLineStart(_lineNumber);
            bool aFirstToken = true;
            //column in rtext protocol starts at 1
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
                                BufferPosition = aOffset + aColumn,
                                Type           = type
                            };
                            //special case for identifier
                            if (type == RTextTokenTypes.Label)
                            {
                                aFirstToken = false;
                            }
                            else if (type == RTextTokenTypes.Identifier)
                            {
                                if (aFirstToken && !IsLineExtended(_lineNumber))
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

        #region[Helpers]
        private bool IsLineExtended(int currentLine)
        {
            if (currentLine <= 0)
            {
                return false;
            }
            else
            {
                //get previous line
                string aline = _nppHelper.GetLine(--currentLine);
                if (String.IsNullOrWhiteSpace(aline))
                {
                    return (IsLineExtended(currentLine));
                }
                else
                {
                    char c = aline.TrimEnd().Last();
                    return (c == ',' || c == '[' || c == '\\');
                }
            }
        }
        #endregion

        #region[Data Members]
        StringBuilder _lineText          = null; //!< Line to tokenize.
        readonly int _lineNumber         = 0;    //!< Line number.
        readonly private INpp _nppHelper = null; //!< Npp helper.
        #endregion
    }
}
