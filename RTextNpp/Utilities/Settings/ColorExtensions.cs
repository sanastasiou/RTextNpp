using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace RTextNppPlugin.Utilities.Settings
{
    public static class ColorExtensions
    {
        public static int ToScintillaColorFormat(this Color c)
        {
            int rgb = c.B;
            rgb |= (c.G << 8);
            rgb |= (c.R << 16);
            return rgb;
        }
    }
}
