using System;
namespace Tests.Utilities
{
    using System.Diagnostics;
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    [TestFixture]
    class StringExtensionsTests
    {
        [Test]
        public void ContainsTest()
        {
            string a = "abbcc";
            Assert.True(a.Contains("CC", StringComparison.InvariantCultureIgnoreCase));
        }
        [Test]
        public void RemoveNewLineTest()
        {
            string a = "abbcc\naakk\r\n";
            Assert.AreEqual(a.RemoveNewLine(), "abbccaakk");
        }
        [Test]
        public void GetByteCountTest()
        {
            string a = "a";
            Assert.AreEqual(1, a.GetByteCount());
        }
    }
}
