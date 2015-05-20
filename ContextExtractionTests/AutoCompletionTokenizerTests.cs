using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace AutoCompletionTests
{
    using NUnit.Framework;
    using RTextNppPlugin.Parsing;
    using RTextNppPlugin.Utilities;
    using Moq;

    [ExcludeFromCodeCoverage]
    [TestFixture]
    class AutoCompletionTokenizerTests
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

        //91 is the lenght of the last context block line
        [Test, Combinatorial, RequiresSTA]
        public void AutoCompletionTokenizerTriggerToken([Values(ContextExtractionSampleInput)] string input,
                                                        [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                        [Range(0, LastLineLength)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens != null);
            
            
        }
    }
}
