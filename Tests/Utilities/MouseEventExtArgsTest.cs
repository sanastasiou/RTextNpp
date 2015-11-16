using System;
using System.Reflection;
namespace Tests.Utilities
{
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    using RTextNppPlugin.Utilities.Settings;
    using RTextNppPlugin;
    using Moq;
    using System.Threading;
    using System.IO;
    using System.Xml;
    using RTextNppPlugin.WpfControls;
    using CSScriptIntellisense;
    using System.Runtime.InteropServices;
    using RTextNppPlugin.DllExport;
    [TestFixture]
    public class MouseEventExtArgsTest
    {
        [Test]
        public void InitializationTest()
        {
            MouseEventExtArgs m = new MouseEventExtArgs(new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 0, 0, 0, 0));
            MouseEventExtArgs p = new MouseEventExtArgs(System.Windows.Forms.MouseButtons.Middle, 0, 0, 0, 0);
        }
        [Test]
        public void HandledTest()
        {
            MouseEventExtArgs m = new MouseEventExtArgs(new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 0, 0, 0, 0));
            m.Handled = true;
            Assert.AreEqual(m.Handled, true);
        }
    }
}
