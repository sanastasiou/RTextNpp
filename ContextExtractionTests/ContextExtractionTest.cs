using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ContextExtractionTests
{
    using NUnit.Framework;
    using RTextNppPlugin.Parsing;

    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class Initialization
    {
        [Test]
        public void InvalidArguments()
        {
            ContextExtractor c = new ContextExtractor(null, 0);

            Assert.AreEqual(c.ContextColumn, 0);
            Assert.AreEqual(c.ContextList.Count(), 0);
        }
    }
}
