using System.Collections.Generic;
using RTextNppPlugin.Parsing;

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
        internal static bool AreTokenStreamsEqual(IEnumerable<Tokenizer.TokenTag> lhs, IEnumerable<Tokenizer.TokenTag> rhs)
        {

            return true;
        }
    }
}
