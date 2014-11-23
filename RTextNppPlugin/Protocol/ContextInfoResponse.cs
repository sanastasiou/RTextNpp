using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ESRLabs.RTextEditor.Protocol
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
