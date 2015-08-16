using System.Collections.Generic;

namespace RTextNppPlugin.RText.Protocol
{   
    internal class SpecificError
    {
        public string message { get; set; }
        public string severity { get; set; }
        public int line { get; set; }
    }

    internal class Error
    {
        public string file { get; set; }
        public List<SpecificError> problems { get; set; }
    }

    internal class LoadResponse : ProgressResponse, IResponseBase
    {
        public List<Error> problems { get; set; }
        public int total_problems { get; set; }

        #region IResponseBase Members
        public string type { get; set; }
        public int invocation_id { get; set; }

        #endregion
    }
}
