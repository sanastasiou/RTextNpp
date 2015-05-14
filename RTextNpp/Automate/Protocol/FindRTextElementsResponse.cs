using System.Collections.Generic;

namespace RTextNppPlugin.Automate.Protocol
{
    public class FindRTextElementsResponse : ProgressResponse, IResponseBase
    {
        public string total_elements { get;set;}
        public List<Element> elements { get; set; }

        #region IResponseBase Members
        public string type { get; set; }
        public int invocation_id { get; set; }

        #endregion
    }

    public class Element
    {
        public string display { get; set; }
        public string file { get; set; }
        public int line { get; set; }
        public string desc { get; set; }
    }
}
