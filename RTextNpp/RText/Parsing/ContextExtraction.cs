using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AJ.Common;
using RTextNppPlugin.Logging;

namespace RTextNppPlugin.RText.Parsing
{
    public class ContextExtractor : IContextExtractor
    {
        #region [Interface]
        /**
         * \brief   Constructor.
         *
         * \param   contextBlock    The context block of text.
         * \param   lengthToEnd     The length to end.
         */
        public ContextExtractor(string contextBlock, int lengthToEnd)
        {
            if (contextBlock == null || lengthToEnd < 0)
            {
                _contextLines = new Stack<string>();
                ContextColumn = 0;
            }
            else
            {
                Analyze(JoinLines(contextBlock.SplitString(new string[] { "\r\n", "\n" }, StringSplitOptions.None)));
                //handle extreme case where no context lines could be found
                if (_contextLines.Count != 0)
                {
                    if (lengthToEnd <= _contextLines.Last().Length)
                    {
                        //adjust for backend
                        ContextColumn = (_contextLines.Last().Length - lengthToEnd) + Constants.Scintilla.BACKEND_COLUMN_OFFSET;
                    }
                    else
                    {
                        ContextColumn = 0;
                        _contextLines = new Stack<string>();
                    }
                }
            }
        }
        /**
         * Gets or sets a list of context lines.
         */
        public IEnumerable<string> ContextList
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
        /**
         * \brief   Analyzes the given joined lines.
         *
         * \param   joinedLines The joined lines.
         * \todo    Figured out exactly when a line can be broken and adjust code accordingly
         */
        private void Analyze(List<StringBuilder> joinedLines)
        {
            _contextLines = new Stack<string>(joinedLines.Count());
            //handle case with empty string
            if(joinedLines.Count == 0)
            {
                return;
            }
            int non_ignored_lines = 0;
            int array_nesting     = 0;
            int block_nesting     = 0;
            int last_element_line = 0;
            if (joinedLines[_currentIndex] != null)
            {
                //last line is always a context line
                _contextLines.Push(joinedLines[_currentIndex].ToString());
            }
            //start from second to last line and go up
            for (int i = _currentIndex - 1; i >= 0; --i)
            {
                string aStrippedLine = String.Empty;
                if ((joinedLines[i] == null) || String.IsNullOrEmpty((aStrippedLine = joinedLines[i].ToString().Trim()))) continue;
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
                                _contextLines.Push(aStrippedLine);
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
                                _contextLines.Push(aStrippedLine);
                            }
                            break;
                        case ']':
                            ++array_nesting;
                            break;
                        case ':':
                            //label directly above element
                            if (non_ignored_lines == last_element_line + 1)
                            {
                                _contextLines.Push(aStrippedLine);
                            }
                            break;
                    }
                }
            }
        }
        private List<StringBuilder> JoinLines(IEnumerable<string> it)
        {
            List<StringBuilder> aJoinedLines = new List<StringBuilder>(new StringBuilder[it.Count()]);
            using (var enumerator = it.GetEnumerator())
            {
                bool aIsBroken        = false;
                _currentIndex         = 0;
                int count             = it.Count();
                while (enumerator.MoveNext())
                {
                    --count;
                    var trimmed = enumerator.Current.Trim();
                    bool aWasBroken = aIsBroken;
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        if (trimmed[0] == '@' || trimmed[0] == '#')
                        {
                            continue;
                        }
                        //need to check if "previous joined line" is also a label...just check last line - it's already joined
                        bool isPreviousLineLabel = (_currentIndex >= 0) && (aJoinedLines[_currentIndex] != null) && COMMAND_LABEL_REGEX.Match((aJoinedLines[_currentIndex].ToString())).Success;
                        aIsBroken = !isPreviousLineLabel && ((trimmed.Last() == '[' && !COMMAND_LABEL_REGEX.Match(trimmed).Success) || trimmed.Last() == ',' || trimmed.Last() == '\\');
                        //handle closing bracket after last element
                        if (trimmed.First() == ']' && _currentIndex > 0 && (aJoinedLines[_currentIndex] != null && aJoinedLines[_currentIndex].ToString().Contains('[')))
                        {
                            aWasBroken = true;
                            --_currentIndex;
                        }
                    }
                    if (aIsBroken)
                    {
                        if (!string.IsNullOrEmpty(trimmed) && trimmed.Last() == '\\')
                        {
                            //remove seperator
                            Append(ref aJoinedLines, aWasBroken, enumerator.Current.Substring(0, enumerator.Current.LastIndexOf('\\')));
                        }
                        else
                        {
                            Append(ref aJoinedLines, aWasBroken, enumerator.Current);
                        }
                    }
                    else
                    {
                        if (count == 0 && string.IsNullOrEmpty(enumerator.Current))
                        {
                            if (_currentIndex > 0)
                            {
                              //handle special case of empty context
                              --_currentIndex;
                            }
                            if(aJoinedLines.Count == 1)
                            {
                                //add empty line
                                Append(ref aJoinedLines, false, enumerator.Current);
                            }
                            break;
                        }
                        Append(ref aJoinedLines, aWasBroken, enumerator.Current);
                    }
                    if (!aIsBroken && (count > 0))
                    {
                        ++_currentIndex;
                    }
                }
            }
            foreach(var line in aJoinedLines)
            {
                if(line != null)Logging.Logger.Instance.Append(line.ToString());
            }
            return aJoinedLines;
        }
        private void Append(ref List<StringBuilder> joinedLines, bool wasBroken, string text )
        {
            if (wasBroken)
            {
                joinedLines[_currentIndex].Append(text);
            }
            else
            {
                joinedLines[_currentIndex] = new StringBuilder(100).Append(text);
            }
        }
        #endregion
        
        #region [Data Members]
        private Stack<string> _contextLines;   //!< The analyzed context lines.
        private int _currentIndex;             //!< The maximum index of currently joined lines.
        private static Regex COMMAND_LABEL_REGEX = new Regex(@"^\s*\w+:", RegexOptions.Compiled);
        #endregion
    }
}