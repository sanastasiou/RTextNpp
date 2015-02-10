using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTextNppPlugin.Parsing
{
    /**
     * @enum    RTextTokenTypes
     * 
     * @brief   Enum with all possible RText types.
     */
    public enum RTextTokenTypes
    {
        Comment,
        Notation,
        Reference,
        Float,
        Integer,
        QuotedString,
        Boolean,
        Label,
        Command,
        RTextName,
        LeftBracket,
        RightBrakcet,
        LeftAngleBrakcet,
        RightAngleBracket,
        Comma,
        Space,
        Template,
        Error,
        NewLine        
    }
}
