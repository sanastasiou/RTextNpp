using System.Collections.Generic;
using System.Linq;
using RTextNppPlugin.RText.Parsing;
using System;
namespace RTextNppPlugin.RText
{
    /**
     * \brief   A token equality comparer.
     *          The purpose of this class is to determine if two token lists are equal.
     *          There are some peculiarities where determining equality of such list, regarding the position of the cursor in the list.
     *          If the cursor is inside a space token then it's position is irrelevant.
     *          If the cursor is at the edge of a label whereas another cursor is inside a label these lists would have to be unequal.
     */
    internal sealed class TokenEqualityComparer
    {
        #region [Data Members]
        private IEnumerable<Tokenizer.TokenTag> _previousList; //!< Holds the previous tokenizer list
        private string _previousFile;                          //!< Holds the previous file where auto completion request was made
        private int _previousCaretPosition;                    //!< Previous caret position.
        #endregion
        #region [Interface]
        internal TokenEqualityComparer()
        {
            _previousList          = null;
            _previousFile          = string.Empty;
            _previousCaretPosition = -1;
        }
        internal bool AreTokenStreamsEqual(IEnumerable<Tokenizer.TokenTag> currentList, int caretPosition, string file)
        {
            bool areEqual = false;
            int tokenDifference = 1;
            if(_previousList != null && file == _previousFile)
            {
                bool isTokenListEqual = false;
                if(_previousList.Count() != currentList.Count())
                {
                    //check if it is possible that even if the token count doesn't match the context itself is equal
                    isTokenListEqual = AreUnevenTokenListsEqual(currentList, out tokenDifference);
                }
                else
                {
                    if(_previousList.Count() > 1)
                    {
                        isTokenListEqual = AreUnevenTokenListsEqual(currentList, out tokenDifference);
                    }
                    else
                    {
                        //single token
                        isTokenListEqual = true;
                    }
                }
                if (isTokenListEqual)
                {
                    var affectedToken = (from t in currentList
                                        where AutoCompletionTokenizer.TokenLocationPredicate(caretPosition, t) && t.Type != RTextTokenTypes.Space
                                        select t).FirstOrDefault();
                    var previousAffectedToken = (from t in _previousList
                                                 where AutoCompletionTokenizer.TokenLocationPredicate(_previousCaretPosition, t) && t.Type != RTextTokenTypes.Space
                                                 select t).FirstOrDefault();
                    if(previousAffectedToken.Context == null)
                    {
                        areEqual = true;
                    }
                    else
                    {
                        if(tokenDifference == 0)
                        {
                            if (affectedToken.Type == RTextTokenTypes.Label)
                            {
                                //type|: has a different completion list than type:|
                                if(!((_previousCaretPosition == affectedToken.EndPosition && caretPosition < affectedToken.EndPosition) ||
                                   (_previousCaretPosition < affectedToken.EndPosition && caretPosition == affectedToken.EndPosition)))
                                {
                                    areEqual = true;
                                }
                            }
                            else
                            {
                                areEqual = true;
                            }
                        }
                    }
                }
            }
            _previousList          = currentList;
            _previousFile          = file;
            _previousCaretPosition = caretPosition;
            return areEqual;
        }
        #endregion
        #region [Helpers]
        private bool AreUnevenTokenListsEqual(IEnumerable<Tokenizer.TokenTag> currentList, out int tokenDifference)
        {
            var aPreviousListWithoutSpaces = from token in _previousList
                                             where token.Type != RTextTokenTypes.Space || (token.Type == RTextTokenTypes.Space && token.Equals(_previousList.Last()) && _previousList.Count() > 1)
                                             select token;
            var aCurrentListWithoutSpaces  = from token in currentList
                                             where token.Type != RTextTokenTypes.Space || (token.Type == RTextTokenTypes.Space && token.Equals(currentList.Last()) && currentList.Count() > 1 )
                                             select token;
            var minCount = Math.Min(aPreviousListWithoutSpaces.Count(), aCurrentListWithoutSpaces.Count());
            var maxCount = Math.Max(aPreviousListWithoutSpaces.Count(), aCurrentListWithoutSpaces.Count());
            if (minCount != maxCount)
            {
                bool isMinimumContextEqual = aPreviousListWithoutSpaces.Take(minCount).SequenceEqual(aCurrentListWithoutSpaces.Take(minCount));
                //single token difference is means identical context
                return ((tokenDifference = (maxCount - minCount)) <= 1) && isMinimumContextEqual;
            }
            else
            {
                tokenDifference = 0;
                if(minCount > 1)
                {
                    bool isMinimumContextEqual = aPreviousListWithoutSpaces.Take(minCount - 1).SequenceEqual(aCurrentListWithoutSpaces.Take(minCount - 1));
                    return isMinimumContextEqual && AreTokensConsideredEqual(aPreviousListWithoutSpaces.Last(), aCurrentListWithoutSpaces.Last());
                }
                else
                {
                    return AreTokensConsideredEqual(aPreviousListWithoutSpaces.Last(), aCurrentListWithoutSpaces.Last());
                }
            }
        }
        private bool AreTokensConsideredEqual(Tokenizer.TokenTag rhs, Tokenizer.TokenTag lhs)
        {
            return !String.IsNullOrEmpty(rhs.Context) &&
                   !String.IsNullOrEmpty(lhs.Context) &&
                   lhs.Type == rhs.Type &&
                   lhs.BufferPosition == rhs.BufferPosition &&
                   (rhs.Context.ToLower().Contains(lhs.Context.ToLower()) ||
                    lhs.Context.ToLower().Contains(rhs.Context.ToLower()));
        }
        #endregion
    }
}