namespace RTextNppPlugin.RText.Parsing
{
    /**
     * @enum    RTextTokenTypes
     * 
     * @brief   Enum with all possible RText types.
     */
    public enum RTextTokenTypes
    {
        Error,
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
        Template,
        LeftBracket,
        RightBrakcet,
        LeftAngleBrakcet,
        RightAngleBracket,
        Comma,
        Space,
        NewLine        
    }
}
