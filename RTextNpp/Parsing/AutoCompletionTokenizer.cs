using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSScriptIntellisense;

namespace RTextNppPlugin.Parsing
{
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
                if (_currentPos >= t.BufferPosition && _currentPos <= t.BufferPosition + (t.EndColumn - t.StartColumn))
                {
                    _triggerToken = new TokenTag
                    {
                        BufferPosition = t.BufferPosition,
                        CaretColumn = currentCursorColumn,
                        Context = t.Context,
                        EndColumn = t.EndColumn,
                        Line = t.Line,
                        StartColumn = t.StartColumn,
                        Type = t.Type
                    };
                    break;
                }
                if (t.Type != RTextTokenTypes.Space)
                {
                    _tokenList.Add(t.Context);
                }
            }
            //special case when token is a space or label - auto completion needs to start at the end of the token
            if (_triggerToken.HasValue && ((_triggerToken.Value.Type == RTextTokenTypes.Space) || (_triggerToken.Value.Type == RTextTokenTypes.Label)))
            {
                //move buffer position, start column, end column, at the end of the token
                _triggerToken = new TokenTag
                {
                    BufferPosition = Npp.GetCaretPosition(),
                    CaretColumn = Npp.GetCaretPosition(),
                    Context = String.Empty,
                    EndColumn = Npp.GetCaretPosition(),
                    Line = _triggerToken.Value.Line,
                    StartColumn = Npp.GetCaretPosition(),
                    Type = _triggerToken.Value.Type
                };
            }

            if (_triggerToken.HasValue)
            {
                var aTokenType = _triggerToken.Value.Type;
                if (aTokenType == RTextTokenTypes.QuotedString ||
                   aTokenType == RTextTokenTypes.Comment ||
                   aTokenType == RTextTokenTypes.Error ||
                   aTokenType == RTextTokenTypes.LeftAngleBrakcet ||
                   aTokenType == RTextTokenTypes.NewLine ||
                   aTokenType == RTextTokenTypes.Notation ||
                   aTokenType == RTextTokenTypes.RightAngleBracket ||
                   aTokenType == RTextTokenTypes.RightBrakcet ||
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
