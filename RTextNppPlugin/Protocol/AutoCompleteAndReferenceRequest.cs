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
    class AutoCompleteAndReferenceRequest : RequestBase
    {
        [DataMember]
        public List<string> context { get; set; }
        [DataMember]
        public int column { get; set; }
    }
}
