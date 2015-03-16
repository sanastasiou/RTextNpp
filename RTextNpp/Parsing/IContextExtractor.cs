using System;

namespace RTextNppPlugin.Parsing
{
    /**
     * Interface for context extraction.
     */
    public interface IContextExtractor
    {
        int ContextColumn { get; }
        System.Collections.Generic.IEnumerable<string> ContextList { get; }
    }
}
