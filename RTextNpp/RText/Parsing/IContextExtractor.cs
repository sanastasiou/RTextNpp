using System;
namespace RTextNppPlugin.RText.Parsing
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