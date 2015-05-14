﻿namespace RTextNppPlugin.Automate.Protocol
{
    public class RequestBase
    {
        virtual public string type { get; set; }
        virtual public string command { get; set; }
        virtual public int invocation_id { get; set; }
    }

    public interface IResponseBase
    {
        string type { get; set; }
        int invocation_id { get; set; }
    }
}
