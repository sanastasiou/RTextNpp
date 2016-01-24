namespace RTextNppPlugin.RText.Protocol
{
    public class RequestBase
    {
        virtual public string type { get { return Constants.Commands.REQUEST; } }
        virtual public string command { get; set; }
        virtual public int invocation_id { get; set; }
    }

    public class ResponseBase : IResponseBase
    {
        public string type { get; set; }
        public int invocation_id { get; set; }
    }

    public interface IResponseBase
    {
        string type { get; set; }
        int invocation_id { get; set; }
    }
}