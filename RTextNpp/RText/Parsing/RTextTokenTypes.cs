namespace RTextNppPlugin.RText.Parsing
{
    /**
     * @enum    RTextTokenTypes
     * 
     * @brief   Enum with all possible RText types.
     */
    public enum RTextTokenTypes : int
    {
        Default,
        Comment,
        Notation,
        Reference,
        Float,
        Integer,
        QuotedString,
        Boolean,
        Label,
        Command,
        Identifier,
        Template,
        Space,
        Other,
        Error,
        LeftBracket,
        RightBrakcet,
        LeftAngleBrakcet,
        RightAngleBracket,
        Comma,
        NewLine,
        AnnotationDebug = 16,
        AnnotationInfo,
        AnnotationWarning,
        AnnotationError,
        AnnotationFatalError
    }
}
