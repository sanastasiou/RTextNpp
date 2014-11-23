using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace ESRLabs.RTextEditor.Protocol
{
    [Serializable]
    [DataContract]
    public class ProgressResponse
    {
        [DataMember]
        virtual public int percentage { get; set; }
        [DataMember]
        virtual public string message { get; set; }
    }
}
