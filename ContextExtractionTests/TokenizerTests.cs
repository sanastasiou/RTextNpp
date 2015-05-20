using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AutoCompletionTests
{
    using Moq;
    using NUnit.Framework;
    using RTextNppPlugin.Parsing;
    using RTextNppPlugin.Utilities;

    [ExcludeFromCodeCoverage]
    [TestFixture]
    class TokenizerTests
    {
        const string ContextExtractionSampleInput = @"
AUTOSAR {
  ARPackage Coding {
    ARPackage Interfaces {
      CalprmInterface ICafCalprm {
        CalprmElementPrototype cpCahEnableTagePassenger, type: /AUTOSAR/DataTypes/Boolean {";

        System.Collections.Generic.List<string> ContextLinesSample = new System.Collections.Generic.List<string> 
        { 
            "AUTOSAR {",
            "ARPackage Coding {",
            "ARPackage Interfaces {",
            "CalprmInterface ICafCalprm {",
            "        CalprmElementPrototype cpCahEnableTagePassenger, type: /AUTOSAR/DataTypes/Boolean {"};

        const int LastLineLength = 91;

        [Test, Combinatorial]
        public void SelectiveTokenizing([Values(ContextExtractionSampleInput)] string input,
                                        [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                        [Range(0, 8)] int caretPosition)
        {
            ContextExtractor c = new ContextExtractor(input, lengthToEndOfCurrentLine);
            //adjust column for backend
            Assert.AreEqual((LastLineLength - lengthToEndOfCurrentLine) + 1, c.ContextColumn);
            Assert.AreEqual(ContextLinesSample.Count(), c.ContextList.Count());
            Assert.IsTrue(c.ContextList.SequenceEqual(ContextLinesSample));

            var nppMock = new Mock<INpp>();

            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");

            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);

            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 0);
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.IsTrue(aTokenizer.TriggerToken.Value.StartColumn == 0);
            Assert.IsTrue(aTokenizer.TriggerToken.Value.EndColumn == 8);
            Assert.IsTrue(aTokenizer.TriggerToken.Value.Context == "        ");
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 0);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Space);
        }
    }
}