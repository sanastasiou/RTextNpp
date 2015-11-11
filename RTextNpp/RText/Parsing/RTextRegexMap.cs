using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace RTextNppPlugin.RText.Parsing
{
    public static class RTextRegexMap
    {
        public static readonly Dictionary<RTextTokenTypes, Regex> REGEX_MAP = new Dictionary<RTextTokenTypes, Regex> {
            { RTextTokenTypes.Space,             new Regex(@"\A[ \t]+"                          , RegexOptions.Compiled) },
            { RTextTokenTypes.Comment,           new Regex(@"\A#.*"                             , RegexOptions.Compiled)},
            { RTextTokenTypes.Notation,          new Regex(@"\A@.*"                             , RegexOptions.Compiled)},
            { RTextTokenTypes.Reference,         new Regex(@"\A\w*(?:[/]\w*)+"                  , RegexOptions.Compiled)},
            { RTextTokenTypes.Float,             new Regex(@"\A[-+]?\d+\.\d+(?:e[+-]\d+)?\b"    , RegexOptions.Compiled)},
            { RTextTokenTypes.Integer,           new Regex(@"\A(?:0x[0-9a-f]+|[-+]?\d+)\b"      , RegexOptions.Compiled | RegexOptions.IgnoreCase)},
            { RTextTokenTypes.QuotedString,      new Regex(@"\A(""|')(?:\\\1|.)*?\1"            , RegexOptions.Compiled)},
            { RTextTokenTypes.Boolean,           new Regex(@"\A(?:true|false)\b"                , RegexOptions.Compiled)},
            { RTextTokenTypes.Label,             new Regex(@"\A\w+:"                            , RegexOptions.Compiled)},
            { RTextTokenTypes.Identifier ,        new Regex(@"\A[a-z_]\w*(?=\s*[^:]|)"          , RegexOptions.Compiled | RegexOptions.IgnoreCase)},
            { RTextTokenTypes.RightBrakcet,      new Regex(@"\A]"                               , RegexOptions.Compiled)},
            { RTextTokenTypes.LeftBracket,       new Regex(@"\A\["                              , RegexOptions.Compiled)},
            { RTextTokenTypes.RightAngleBracket, new Regex(@"\A}"                               , RegexOptions.Compiled)},
            { RTextTokenTypes.LeftAngleBrakcet,  new Regex(@"\A{"                               , RegexOptions.Compiled)},
            { RTextTokenTypes.Comma,             new Regex(@"\A,"                               , RegexOptions.Compiled)},
            { RTextTokenTypes.Template,          new Regex(@"\A(?:<%((?:(?!%>).)*)%>|<([^>]*)>)", RegexOptions.Compiled)},
            { RTextTokenTypes.Error,             new Regex(@"\A[\S+]"                           , RegexOptions.Compiled)},
            { RTextTokenTypes.NewLine,           new Regex(@"\r|\n|\r\n"                        , RegexOptions.Compiled)}
        };
    }
}