using System.Collections.Generic;

namespace RTextNppPlugin.RText.Protocol
{
    class AutoCompleteRequest : RequestBase
    {
        public List<string> context { get; set; }
        public int column { get; set; }
    }
}
