using System;
using System.Text;

namespace RTextNppPlugin.Utilities
{
    public static class StringExtensions
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        public static string RemoveNewLine(this string input)
        {
            return input.Replace("\r", "").Replace("\n", "");
        }

        public static int GetByteCount(this string text)
        {
            return Encoding.Default.GetByteCount(text);
        }
    }

}
