using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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
                        if(typesToKeep.Count() == 0 || typesToKeep.Contains(type))
                        {
                            TokenTag aCurrentTag = new TokenTag
                            {
                                Line           = _lineNumber,
                                Context        = aMatch.Value,
                                StartColumn    = aColumn + aMatch.Index,
                                EndColumn      = aColumn + aMatch.Length,
                                BufferPosition = aOffset + aColumn,
                                Type           = type
                            };
                            //special case for identifier
                            if (type == RTextTokenTypes.Label)
                            {
                                aFirstToken = false;
                            }
                            else if(type == RTextTokenTypes.RTextName)
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
        string        _lineText;    //!< Line to tokenize.
        int           _lineNumber;  //!< Line number.
        #endregion
    }
}
