using System;
using System.Collections.Generic;
using System.Linq;
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
                                      Line,
                                      StartColumn,
                                      EndColumn,
                                      BufferPosition,
                                      Context,
                                      CaretColumn,
                                      Type
                                    );
            }

            public static bool operator ==(TokenTag lhs, TokenTag rhs)
            {
                return (lhs.Type == lhs.Type && lhs.Line == rhs.Line && lhs.StartColumn == rhs.StartColumn);

            }

            public static bool operator != (TokenTag lhs, TokenTag rhs)
            {
                return !(lhs == rhs);
            }

            public override bool Equals(object obj)
            {
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
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

        /**
         * Gets the line tokens up until the trigger point has been found.
         *
         * \return  The line tokens.
         */
        public IEnumerable<string> LineTokens
        {
            get
            {
                return _tokenList;
            }
        }

        #region [Helpers]
        void FindTriggerToken(int currentCursorColumn)
        {
            foreach (var t in base.Tokenize())
            {
                _tokenList.Add(t.Context);                
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
            //special case when token is a space or label - auto completion needs to start at the end of the token
            if (_triggerToken.HasValue && ((_triggerToken.Value.Type == RTextTokenTypes.Space) || (_triggerToken.Value.Type == RTextTokenTypes.Label)))
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

            if (_triggerToken.HasValue)
            {
                var aTokenType = _triggerToken.Value.Type;
                if(aTokenType == RTextTokenTypes.QuotedString      || 
                   aTokenType == RTextTokenTypes.Comment           || 
                   aTokenType == RTextTokenTypes.Error             || 
                   aTokenType == RTextTokenTypes.LeftAngleBrakcet  ||
                   aTokenType == RTextTokenTypes.NewLine           ||
                   aTokenType == RTextTokenTypes.Notation          ||
                   aTokenType == RTextTokenTypes.RightAngleBracket ||
                   aTokenType == RTextTokenTypes.RightBrakcet      ||
                   aTokenType == RTextTokenTypes.Template
                  )
                {
                    //no auto completion for above types
                    _triggerToken = null;
                }
            }
        }
        #endregion

        #region [Data Members]
        private readonly int _currentPos;
        private Tokenizer.TokenTag? _triggerToken = null;
        private List<string> _tokenList = new List<string>(50);
        #endregion
    }
}
