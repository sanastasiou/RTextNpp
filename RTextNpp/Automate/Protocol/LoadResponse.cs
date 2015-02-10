using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RTextNppPlugin.Automate.Protocol
{   
    [Serializable]
    [DataContract]
    public class SpecificProblems
    {
        [DataMember]
        public string message { get; set; }
        [DataMember]
        public string severity { get; set; }
        [DataMember]
        public int line { get; set; }
    }

    [Serializable]
    [DataContract]
    public class Problem
    {
        [DataMember]
        public string file { get; set; }
        [DataMember]
        public List<SpecificProblems> problems { get; set; }
    }

    [Serializable]
    [DataContract]
    public class LoadResponse : ProgressResponse, IResponseBase
    {
        [DataMember]
        public List<Problem> problems { get; set; }
        [DataMember]
        public int total_problems { get; set; }

        #region IResponseBase Members
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public int invocation_id { get; set; }

        #endregion
    }
}
