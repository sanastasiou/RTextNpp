using System;
using System.Runtime.Serialization;

namespace RTextNppPlugin.RTextEditor.Protocol
{
    [Serializable]
    [DataContract]
    class ContextInfoResponse : ProgressResponse, IResponseBase
    {
        [DataMember]
        public string desc { get; set; }

        #region IResponseBase Members
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public int invocation_id { get; set; }

        #endregion
    }
}
