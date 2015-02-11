using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CSScriptIntellisense;

namespace RTextNppPlugin.Parsing
{
    public class Tokenizer
    {
        public struct TokenTag
        {
            public RTextTokenTypes Type { get; set; }
            public string Context { get; set; }
            public int Line { get; set; }
            public int StartColumn { get; set; }
            public int EndColumn { get; set; }
            public int BufferPosition { get; set; }
            public int CaretColumn { get; set; }

            public override string ToString()
            {
                return String.Format("Token\nline : {0}\nsc : {1}\nec : {2}\npos : {3}\ncontext : {4}\ncc : {5}\ntype : {6}",
                                      this.Line,
                                      this.StartColumn,
                                      this.EndColumn,
                                      this.BufferPosition,
                                      this.Context,
                                      this.CaretColumn,
                                      this.Type
                                    );
            }
        }

        #region[Interface]
        public Tokenizer(int line)
        {
            _lineNumber = line;
            _lineText = CSScriptIntellisense.Npp.GetLine(line);
        }

        public IEnumerable<TokenTag> Tokenize(params RTextTokenTypes[] typesToKeep)
        {
            int aOffset = CSScriptIntellisense.Npp.GetLineStart(_lineNumber);
            bool aFirstToken = true;
            //column in rtext protocol starts at 1
            int aColumn = 0;
            while (!string.IsNullOrEmpty(_lineText))
            {
                foreach (var type in RTextRegexMap.REGEX_MAP.Keys)
                {
                    Match aMatch = RTextRegexMap.REGEX_MAP[type].Match(_lineText);
                    if (aMatch.Success)
                    {
                        if (typesToKeep.Count() == 0 || typesToKeep.Contains(type))
                        {
                            TokenTag aCurrentTag = new TokenTag
                            {
                                Line = _lineNumber,
                                Context = aMatch.Value,
                                StartColumn = aColumn + aMatch.Index,
                                EndColumn = aColumn + aMatch.Length,
                                BufferPosition = aOffset + aColumn,
                                Type = type
                            };
                            //special case for identifier
                            if (type == RTextTokenTypes.Label)
                            {
                                aFirstToken = false;
                            }
                            else if (type == RTextTokenTypes.RTextName)
                            {
                                if (aFirstToken && !isLineExtended(_lineNumber))
                                {
                                    aCurrentTag.Type = RTextTokenTypes.Command;
                                    aFirstToken = false;
                                }

                            }
                            yield return aCurrentTag;
                        }
                        aColumn += aMatch.Length;
                        _lineText = _lineText.Substring(aMatch.Length);
                        break;
                    }
                }
            }
            yield break;
        }


        #endregion

        #region[Helpers]
        bool isLineExtended(int currentLine)
        {
            if (currentLine < 0)
            {
                return false;
            }
            else
            {
                //get previous line
                string aline = CSScriptIntellisense.Npp.GetLine(--currentLine);
                if (String.IsNullOrWhiteSpace(aline))
                {
                    return (isLineExtended(currentLine));
                }
                else
                {
                    foreach (var c in aline.Reverse())
                    {
                        if (Char.IsWhiteSpace(c))
                        {
                            continue;
                        }
                        else if (c == ',' || c == '[' || c == '\\')
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return false;
                }
            }
        }
        #endregion

        #region[Data Members]
        string _lineText;    //!< Line to tokenize.
        int _lineNumber;  //!< Line number.
        #endregion
    }

    /**
     * \brief   An automatic completion tokenizer. This class finds the token for which autocompletion is invoked.
     */
    public class AutoCompletionTokenizer : Tokenizer
    {
        public AutoCompletionTokenizer(int line, int currentCaretPosition, int currentCursorColumn)
            : base(line)
        {
            _currentPos = currentCaretPosition;
            FindTriggerToken(currentCursorColumn);
        }

        /**
         * Gets the trigger token.
         *
         * \return  The trigger token.
         */
        public Tokenizer.TokenTag? TriggerToken
        {
            get
            {
                return _triggerToken;
            }
        }

        /**
         * Gets or sets the caret position.
         *
         * \return  The caret position.
         */
        public int CaretColumn { get; set; }

        #region [Helpers]
        void FindTriggerToken(int currentCursorColumn)
        {
            foreach (var t in base.Tokenize(RTextTokenTypes.Boolean, RTextTokenTypes.Comma,
                                            RTextTokenTypes.Command, RTextTokenTypes.Float,
                                            RTextTokenTypes.Integer, RTextTokenTypes.Label,
                                            RTextTokenTypes.LeftAngleBrakcet, RTextTokenTypes.LeftBracket,
                                            RTextTokenTypes.Reference, RTextTokenTypes.RightAngleBracket,
                                            RTextTokenTypes.RightBrakcet, RTextTokenTypes.RTextName,
                                            RTextTokenTypes.Template, RTextTokenTypes.Space))
            {
                if (_currentPos >= t.BufferPosition && _currentPos <= t.BufferPosition + (t.EndColumn - t.StartColumn))
                {
                    _triggerToken = new TokenTag
                    {
                        BufferPosition = t.BufferPosition,
                        CaretColumn    = currentCursorColumn,
                        Context        = t.Context,
                        EndColumn      = t.EndColumn,
                        Line           = t.Line,
                        StartColumn    = t.StartColumn,
                        Type           = t.Type
                    };
                    break;
                }
            }
            //special case when token is a space - auto completion needs to start
            if (_triggerToken.HasValue && (_triggerToken.Value.Type == RTextTokenTypes.Space))
            {
                //move buffer position, start column, end column, at the end of the token
                _triggerToken = new TokenTag
                {
                    BufferPosition = Npp.GetCaretPosition(),
                    CaretColumn    = Npp.GetCaretPosition(),
                    Context        = String.Empty,
                    EndColumn      = Npp.GetCaretPosition(),
                    Line           = _triggerToken.Value.Line,
                    StartColumn    = Npp.GetCaretPosition(),
                    Type           = _triggerToken.Value.Type
                };
            }

            #if DEBUG
            if (_triggerToken.HasValue)
            {
                System.Diagnostics.Trace.WriteLine(_triggerToken.Value);
            }
            #endif
        }
        #endregion

        #region [Data Members]
        private readonly int _currentPos;
        private Tokenizer.TokenTag? _triggerToken = null;
        #endregion
    }
}
