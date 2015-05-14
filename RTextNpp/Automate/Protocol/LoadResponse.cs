using System.Collections.Generic;

namespace RTextNppPlugin.Automate.Protocol
{   
    public class SpecificProblems
    {
        public string message { get; set; }
        public string severity { get; set; }
        public int line { get; set; }
    }

    public class Problem
    {
        public string file { get; set; }
        public List<SpecificProblems> problems { get; set; }
    }

    public class LoadResponse : ProgressResponse, IResponseBase
    {
        public List<Problem> problems { get; set; }
        public int total_problems { get; set; }

        #region IResponseBase Members
        public string type { get; set; }
        public int invocation_id { get; set; }

        #endregion
    }
}
