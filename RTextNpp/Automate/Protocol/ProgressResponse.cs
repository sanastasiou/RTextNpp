﻿using System;
using System.Runtime.Serialization;

namespace RTextNppPlugin.Automate.Protocol
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
