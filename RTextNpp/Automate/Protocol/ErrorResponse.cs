using System;
using System.Runtime.Serialization;

namespace RTextNppPlugin.RTextEditor.Protocol
{
    [Serializable]
    [DataContract]
    class ErrorResponse : ProgressResponse, IResponseBase
    {
        [DataMember]
        public string command { get; set; }

        #region IResponseBase Members
        [DataMember]
        public string type { get; set; }
        [DataMember]
        public int invocation_id { get; set; }
        #endregion
    }
}
