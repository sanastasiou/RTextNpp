using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ESRLabs.RTextEditor.Protocol
{
    [DataContract]
    public class FindRTextElementsResponse : ProgressResponse, IResponseBase
    {
        [DataMember]
        public string total_elements { get;set;}
        [DataMember]
        public List<Element> elements { get; set; }

        #region IResponseBase Members
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public int invocation_id { get; set; }

        #endregion
    }

    [DataContract]
    public class Element
    {
        [DataMember]
        public string display { get; set; }
        [DataMember]
        public string file { get; set; }
        [DataMember]
        public int line { get; set; }
        [DataMember]
        public string desc { get; set; }
    }
}
