using System.Runtime.Serialization;

namespace RTextNppPlugin.Utilities.Protocol
{  
    [DataContract]
    public class FindElementRequest : RequestBase
    {
        [DataMember]
        public string search_pattern { get; set; }
    }
}
