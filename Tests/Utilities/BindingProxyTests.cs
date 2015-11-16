using System;
namespace Tests.Utilities
{
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    [TestFixture]
    class BindingProxyTests
    {
        [Test]
        public void BindingProxyTest()
        {
            int Data = 0;
            BindingProxy a = new BindingProxy();
            a.Data = Data;
            var k = a.Data;
            Assert.AreEqual(k, Data);
        }
    }
}
