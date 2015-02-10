using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RTextNppPlugin.Automate.Protocol
{
    [DataContract]
    class AutoCompleteRequest : RequestBase
    {
        [DataMember]
        public List<string> context { get; set; }
        [DataMember]
        public int column { get; set; }
    }
}
