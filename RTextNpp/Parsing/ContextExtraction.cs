using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTextNppPlugin.Parsing
{
    class ContextExtraction
    {
        #region [Interface]
        public ContextExtraction(string [] contextLines, int caretPosition )
        {
            _reversedLines  = new List<string>(contextLines.Reverse());
            _contextLines   = new List<string>(_reversedLines.Count);
            _originalColumn = caretPosition;
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
        private void Analyze()
        {

        }


        #endregion

        #region [Data Members]

        List<string> _contextLines;  //! The analyzed context lines.
        List<string> _reversedLines; //!< Original context lines in reversed order.
        int _originalColumn;         //!< Origianl column position.

        #endregion       

        /**
         * 
         * @brief   Extracts the context lines described for the RText backend service.
         * 
         * @param   unfilteredLines The unfiltered lines.
         * 
         * \param [in,out]  unfilteredLines The unfiltered lines.
         * \param [in,out]  aResultLines    The result lines (context).
         */
        public void extractContextLines(ref List<string> unfilteredLines, ref string[] aResultLines)
        {
            int non_ignored_lines = 0;
            int array_nesting = 0;
            int block_nesting = 0;
            int last_element_line = 0;
            int aAddedLines = 0;
            int aCount = unfilteredLines.Count;
            //for (int i = 1; i < aCount; ++i)
            //{
            //    if (i == 0)
            //    {
            //        aResultLinesTemp[aCount - (++aAddedLines)] = (unfilteredLines[i] + Environment.NewLine);
            //    }
            //    else
            //    {
            //        string aStrippedLine = unfilteredLines[i].Trim();
            //        if (String.IsNullOrEmpty(aStrippedLine) || aStrippedLine.StartsWith("#") || String.IsNullOrWhiteSpace(aStrippedLine)) continue;
            //        else
            //        {
            //            ++non_ignored_lines;
            //            switch (aStrippedLine.Last())
            //            {
            //                case '{':
            //                    if (block_nesting > 0)
            //                    {
            //                        --block_nesting;
            //                    }
            //                    else if (block_nesting == 0)
            //                    {
            //                        aResultLinesTemp[aCount - (++aAddedLines)] = aStrippedLine;
            //                        last_element_line = non_ignored_lines;
            //                    }
            //                    break;
            //                case '}':
            //                    ++block_nesting;
            //                    break;
            //                case '[':
            //                    if (array_nesting > 0)
            //                    {
            //                        --array_nesting;
            //                    }
            //                    else if (array_nesting == 0)
            //                    {
            //                        aResultLinesTemp[aCount - (++aAddedLines)] = aStrippedLine;
            //                    }
            //                    break;
            //                case ']':
            //                    ++array_nesting;
            //                    break;
            //                case ':':
            //                    //label directly above element
            //                    if (non_ignored_lines == last_element_line + 1)
            //                    {
            //                        aResultLinesTemp[aCount - (++aAddedLines)] = aStrippedLine;
            //                    }
            //                    break;
            //            }
            //        }
            //    }
            //}
            //aResultLines = new string[aAddedLines];
            //Array.Copy(aResultLinesTemp, aCount - aAddedLines, aResultLines, 0, aAddedLines);
        }
    }
}
