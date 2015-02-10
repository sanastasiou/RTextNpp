﻿using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace RTextNppPlugin.Automate.Protocol
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
