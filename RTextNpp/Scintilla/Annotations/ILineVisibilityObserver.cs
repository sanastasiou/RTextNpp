using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTextNppPlugin.Scintilla.Annotations
{
    internal class VisibilityInfo : IEquatable<VisibilityInfo>
    {
        public string File { get; set; }
        public int FirstLine { get; set; }
        public int LastLine { get; set; }
        public IntPtr ScintillaHandle { get; set; }

        public override int GetHashCode()
        {
            return File.GetHashCode() ^ FirstLine ^ LastLine ^ ScintillaHandle.ToInt32();
        }

        public bool Equals(VisibilityInfo other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }
            if (ReferenceEquals(this, other))
            {
                return true;
            }
            return (File == other.File) && (ScintillaHandle == other.ScintillaHandle) && (FirstLine == other.FirstLine) && (LastLine == other.LastLine);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return obj.GetType() == GetType() && Equals((VisibilityInfo)obj);
        }

        public static bool operator ==(VisibilityInfo obj1, VisibilityInfo obj2)
        {
            if (ReferenceEquals(obj1, null))
            {
                return false;
            }
            if (ReferenceEquals(obj2, null))
            {
                return false;
            }

            return (obj1.File == obj2.File) && (obj1.ScintillaHandle == obj2.ScintillaHandle) && (obj1.FirstLine == obj2.FirstLine) && (obj1.LastLine == obj2.LastLine);
        }

        // this is second one '!='
        public static bool operator !=(VisibilityInfo obj1, VisibilityInfo obj2)
        {
            return !(obj1 == obj2);
        }

        public override string ToString()
        {
            return String.Format("Visibility info : \nScintilla : {0}\nFile : {1}\nFirst visible line : {2}\nLast visible line : {3}\n", ScintillaHandle, File, FirstLine, LastLine);
        }
    }

    internal delegate void VisibilityInfoUpdated(VisibilityInfo info);

    interface ILineVisibilityObserver
    {
        VisibilityInfo MainVisibilityInfo { get; }

        VisibilityInfo SubVisibilityInfo { get; }

        event VisibilityInfoUpdated OnVisibilityInfoUpdated;
    }
}
