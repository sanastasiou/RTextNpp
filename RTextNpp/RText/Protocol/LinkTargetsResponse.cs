using System.Collections.Generic;

namespace RTextNppPlugin.RText.Protocol
{
    public class LinkTargetsResponse : ProgressResponse, IResponseBase
    {
        public string begin_column { get; set; }
        public string end_column { get; set; }
        public List<Target> targets { get; set; }

        #region IResponseBase Members
        public string type { get; set; }
        public int invocation_id { get; set; }

        #endregion
    }
    
    public class Target
    {
        public string display { get; set; }
        public string file { get; set; }
        public string line { get; set; }
        public string desc { get; set; }
    }
}
