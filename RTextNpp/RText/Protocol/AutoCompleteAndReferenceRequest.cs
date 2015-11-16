using System.Collections.Generic;
namespace RTextNppPlugin.RText.Protocol
{
    class AutoCompleteAndReferenceRequest : RequestBase
    {
        public IEnumerable<string> context { get; set; }
        public int column { get; set; }
    }
}