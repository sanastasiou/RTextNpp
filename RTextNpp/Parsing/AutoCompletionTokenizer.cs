using System;
using System.Collections.Generic;

namespace RTextNppPlugin.Parsing
{
    using Utilities;
    /**
     * \brief   An automatic completion tokenizer. This class finds the token for which autocompletion is invoked.
     */
    public class AutoCompletionTokenizer : Tokenizer
    {
        /**
         * \brief   Constructor.
         *
         * \param   line                    The line.
         * \param   currentCaretPosition    The current caret position.
         * \param   nppHelper               The npp helper which provides access to npp buffer information.
         */
        public AutoCompletionTokenizer(int line, int currentCaretPosition, INpp nppHelper)
            : base(line, nppHelper)
        {
            _nppHelper  = nppHelper;
            _currentPos = currentCaretPosition;
            FindTriggerToken();
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
        void FindTriggerToken()
        {
            foreach (var t in base.Tokenize())
            {
                if (_currentPos >= t.BufferPosition && _currentPos <= t.BufferPosition + (t.EndColumn - t.StartColumn))
                {
                    _triggerToken = new TokenTag
                    {
                        BufferPosition = t.BufferPosition,
                        Context        = t.Context,
                        EndColumn      = t.EndColumn,
                        Line           = t.Line,
                        StartColumn    = t.StartColumn,
                        Type           = t.Type
                    };
                    break;
                }
                if (t.Type != RTextTokenTypes.Space)
                {
                    _tokenList.Add(t.Context);
                }
            }

            if (_triggerToken.HasValue)
            {
                var aTokenType = _triggerToken.Value.Type;
                if (aTokenType == RTextTokenTypes.QuotedString      ||
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
        private readonly int _currentPos          = 0;
        private Tokenizer.TokenTag? _triggerToken = null;
        private readonly INpp _nppHelper          = null;
        private List<string> _tokenList           = new List<string>(50);
        #endregion
    }
}
