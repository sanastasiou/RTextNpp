using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace RTextNppPlugin.Parsing
{
    class ContextExtractor
    {
        #region [Interface]

        /**
         * \brief   Constructor.
         *
         * \param   contextLines    The context lines.
         * \param   lengthToEnd     The length to end of line from current column.
         */
        public ContextExtractor(string[] contextLines, int lengthToEnd)
        {
            _reversedLines = new List<string>(JoinLines(ref contextLines).Reverse());
            _contextLines  = new List<string>(_reversedLines.Count);
            Analyze();
            ContextColumn  = _contextLines.Last().Length - lengthToEnd + 1; //compensate for backend
        }

        /**
         * \brief   Constructor.
         *
         * \param   contextBlock    The context block of text.
         * \param   lengthToEnd     The length to end.
         */
        public ContextExtractor(string contextBlock, int lengthToEnd)
        {
            var aContextLines = contextBlock.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            _reversedLines = new List<string>(JoinLines(ref aContextLines).Reverse());
            _contextLines = new List<string>(_reversedLines.Count);
            Analyze();
            ContextColumn = _contextLines.Last().Length - lengthToEnd + 1; //compensate for backend
        }

        /**
         * Gets or sets a list of context lines.
         */
        public List<string> ContextList
        {
            get
            {
                return _contextLines;
            }
        }

        /**
         * Gets or sets the context column.
         *
         */
        public int ContextColumn { get; private set; }

        #endregion

        #region [Helpers]
        private void Analyze()
        {
            int non_ignored_lines = 0;
            int array_nesting     = 0;
            int block_nesting     = 0;
            int last_element_line = 0;

            _contextLines.Add(_reversedLines[0]);
            for (int i = 1; i < _reversedLines.Count; ++i)
            {
                string aStrippedLine = _reversedLines[i].Trim();
                if (String.IsNullOrEmpty(aStrippedLine)) continue;
                else
                {
                    ++non_ignored_lines;
                    switch (aStrippedLine.Last())
                    {
                        case '{':
                            if (block_nesting > 0)
                            {
                                --block_nesting;
                            }
                            else if (block_nesting == 0)
                            {
                                _contextLines.Add(aStrippedLine);
                                last_element_line = non_ignored_lines;
                            }
                            break;
                        case '}':
                            ++block_nesting;
                            break;
                        case '[':
                            if (array_nesting > 0)
                            {
                                --array_nesting;
                            }
                            else if (array_nesting == 0)
                            {
                                _contextLines.Add(aStrippedLine);
                            }
                            break;
                        case ']':
                            ++array_nesting;
                            break;
                        case ':':
                            //label directly above element
                            if (non_ignored_lines == last_element_line + 1)
                            {
                                _contextLines.Add(aStrippedLine);
                            }
                            break;
                    }
                }
            }
            _contextLines.Reverse();
        }

        /**
         * \brief   Join broken lines while preserving whitespace, commas and opening brackets. Line separators are removed.
         *
         * \param [in,out]  originalLines   The original lines.
         *
         * \return  An enumerator that allows foreach to be used to process the joined lines.
         */
        
        private IEnumerable<string> JoinLines(ref string [] originalLines)
        {
            List<string> aJoinedLines = new List<string>(originalLines.Count());
            for (int i = 0; i < originalLines.Count(); ++i)
            {
                //skip whitespaces etc.
                if(_IGNORE_LINE_REGEX.IsMatch(originalLines[i]))continue;

                string aCurrentLine = null;
                Match aSepMatch = null;
                do
                {
                    //concatenate till no more line breaks or end of lines are found
                    if ((aSepMatch = _LINE_SEP_REGEX.Match(originalLines[i])).Success)
                    {
                        if (aCurrentLine == null)
                        {
                            aCurrentLine = aSepMatch.Groups[1].Value;
                            if(aSepMatch.Groups[2].Value == "," || aSepMatch.Groups[2].Value == "[")
                            {
                                aCurrentLine += aSepMatch.Groups[2].Value;
                            }
                        }
                        else
                        {
                            aCurrentLine += aSepMatch.Groups[1].Value;
                        }
                        ++i;
                    }
                    else
                    {
                        //we have had no match till now - no broken line
                        if (aCurrentLine == null)
                        {
                            aCurrentLine = originalLines[i];
                        }
                        else
                        {
                            //we had some match, just append
                            aCurrentLine += originalLines[i];
                        }
                    }

                } while (aSepMatch.Success && i < originalLines.Count());
                aJoinedLines.Add(aCurrentLine);
            }
            return aJoinedLines;
        }


        #endregion

        #region [Data Members]

        private List<string> _contextLines;                                                              //! The analyzed context lines.
        private List<string> _reversedLines;                                                             //!< Original context lines in reversed order.
        private Regex        _LINE_SEP_REGEX = new Regex(@"(.*?)(\\|,|\[)(\s*)$", RegexOptions.Compiled);//!< Regex to find line breaker.
        private Regex _IGNORE_LINE_REGEX = new Regex(@"^\s*(@|#)", RegexOptions.Compiled);               //!< Regex to completely discard line if it's a comment or a notation.

        #endregion
    }
}
