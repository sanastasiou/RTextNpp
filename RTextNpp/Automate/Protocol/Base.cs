using System;
using System.Runtime.Serialization;

namespace RTextNppPlugin.RTextEditor.Protocol
{
    [Serializable]
    [DataContract]
    public class RequestBase
    {
        [DataMember]
        virtual public string type { get; set; }
        [DataMember]
        virtual public string command { get; set; }
        [DataMember]
        virtual public int invocation_id { get; set; }
    }

    public interface IResponseBase
    {
        string type { get; set; }
        int invocation_id { get; set; }
    }
}
