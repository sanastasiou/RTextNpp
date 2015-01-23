using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RTextNppPlugin.Utilities.Protocol
{
    [Serializable]
    [DataContract]
    public class Option
    {
        [DataMember]
        public string display { get; set; }
        [DataMember]
        public string insert { get; set; }
        [DataMember]
        public string desc { get; set; }
    }

    [DataContract]
    public class AutoCompleteResponse : ProgressResponse, IResponseBase
    {
        [DataMember]
        public List<Option> options { get; set; }
        
        #region IResponseBase Members
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public int invocation_id { get; set; }
        #endregion
    }
}
