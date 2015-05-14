using System.Collections.Generic;

namespace RTextNppPlugin.Automate.Protocol
{
    public class Option
    {
        public string display { get; set; }
        public string insert { get; set; }
        public string desc { get; set; }
    }

    public class AutoCompleteResponse : ProgressResponse, IResponseBase
    {
        public List<Option> options { get; set; }
        
        #region IResponseBase Members
        public string type { get; set; }
        public int invocation_id { get; set; }
        #endregion
    }
}
