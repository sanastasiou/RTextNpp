﻿using System;
using System.Collections.Generic;
namespace RTextNppPlugin.RText.Parsing
{
    using RTextNppPlugin.Scintilla;
    using Utilities;
    /**
     * \brief   An automatic completion tokenizer. This class finds the token for which auto-completion is invoked.
     */
    internal class AutoCompletionTokenizer : Tokenizer
    {
        /**
         * \brief   Constructor.
         *
         * \param   line                    The line.
         * \param   currentCaretPosition    The current caret position.
         * \param   startPosition           The start position.
         * \param   nppHelper               The npp helper which provides access to npp buffer
         *                                  information.
         * \param   sciPtr                  The sci pointer.
         */
        internal AutoCompletionTokenizer(int line, int currentCaretPosition, int startPosition, INpp nppHelper, IntPtr sciPtr)
            : base(line, startPosition, nppHelper, sciPtr, Tokenizer.IsLineExtended(line, nppHelper, sciPtr))
        {
            _currentPos     = currentCaretPosition;
            FindTriggerToken();
        }
        /**
         * Gets the trigger token.
         *
         * \return  The trigger token.
         */
        internal Tokenizer.TokenTag? TriggerToken
        {
            get
            {
                return _triggerToken;
            }
        }
        /**
         * Gets the line tokens excluding the trigger point token.
         *
         * \return  The line tokens except the token of the trigger point.
         */
        internal IEnumerable<TokenTag> LineTokens
        {
            get
            {
                return _tokenList;
            }
        }
        #region [Helpers]
        private void FindTriggerToken()
        {
            foreach (var t in base.Tokenize())
            {
                _tokenList.Add(t);
                if (TokenLocationPredicate(_currentPos, t))
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
            }
            if (_triggerToken.HasValue)
            {
                var aTokenType = _triggerToken.Value.Type;
                if (aTokenType == RTextTokenTypes.QuotedString      ||
                    aTokenType == RTextTokenTypes.Comment           ||
                    aTokenType == RTextTokenTypes.Error             ||
                    aTokenType == RTextTokenTypes.LeftAngleBrakcet  ||
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
        internal static bool TokenLocationPredicate(int caretPosition, TokenTag token)
        {
            return (caretPosition >= token.BufferPosition && caretPosition <= token.BufferPosition + (token.EndColumn - token.StartColumn));
        }
        #endregion
        #region [Data Members]
        private readonly int _currentPos  = 0;
        private TokenTag? _triggerToken   = null;
        private List<TokenTag> _tokenList = new List<TokenTag>(50);
        #endregion
    }
}