
namespace RTextNppPlugin.RText.Protocol
{
    class ErrorResponse : ProgressResponse, IResponseBase
    {
        public string command { get; set; }
        #region IResponseBase Members
        public string type { get; set; }
        public int invocation_id { get; set; }
        #endregion
    }
}