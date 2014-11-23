using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace ESRLabs.RTextEditor.Protocol
{  
    [DataContract]
    public class FindElementRequest : RequestBase
    {
        [DataMember]
        public string search_pattern { get; set; }
    }
}
