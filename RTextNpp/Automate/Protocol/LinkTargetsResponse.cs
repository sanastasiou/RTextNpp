using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RTextNppPlugin.Utilities.Protocol
{
    [Serializable]
    [DataContract]
    public class LinkTargetsResponse : ProgressResponse, IResponseBase
    {
        [DataMember]
        public string begin_column { get; set; }
        [DataMember]
        public string end_column { get; set; }
        [DataMember]
        public List<Target> targets { get; set; }

        #region IResponseBase Members
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public int invocation_id { get; set; }

        #endregion
    }
    
    [Serializable]
    [DataContract]
    public class Target
    {
        [DataMember]
        public string display { get; set; }
        [DataMember]
        public string file { get; set; }
        [DataMember]
        public string line { get; set; }
        [DataMember]
        public string desc { get; set; }
    }
}
