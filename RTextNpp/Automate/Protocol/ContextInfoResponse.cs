
namespace RTextNppPlugin.Automate.Protocol
{
    class ContextInfoResponse : ProgressResponse, IResponseBase
    {
        public string desc { get; set; }

        #region IResponseBase Members
        public string type { get; set; }
        public int invocation_id { get; set; }

        #endregion
    }
}
