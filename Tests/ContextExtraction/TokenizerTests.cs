using System.Linq;
namespace ContextExtraction.Tokenizer
{
    using System.Collections.Generic;
    using Moq;
    using NUnit.Framework;
    using RTextNppPlugin.RText.Parsing;
    using RTextNppPlugin.Utilities;
    using System;
    [TestFixture]
    class TokenizerTests
    {
        const string ContextExtractionSampleInput = @"
\
ID
AUTOSAR {
  ARPackage Coding {
    ARPackage Interfaces {
      CalprmInterface \
        cpCahEnableTagePassenger,
        label: ""string"",
        type: [
        1,
        2]";
        List<string> ContextLinesSample = new List<string>
        {
            "AUTOSAR {",
            "ARPackage Coding {",
            "ARPackage Interfaces {",
            "CalprmInterface         cpCahEnableTagePassenger,        label: \"string\",        type: [",
            "        1,        2]"};
        
        [Test]
        public void SelectiveTokenizing([Values(ContextExtractionSampleInput)] string input)
        {
            int LastLineLength = ContextLinesSample.Last().Length;

            ContextExtractor c = new ContextExtractor(input, 0);
            //adjust column for backend
            Assert.AreEqual((LastLineLength) + 1, c.ContextColumn);
            Assert.AreEqual(ContextLinesSample.Count(), c.ContextList.Count());
            Assert.IsTrue(c.ContextList.SequenceEqual(ContextLinesSample));
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns<int>( x=> ContextLinesSample[x] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 3))).Returns(0);
            Tokenizer aTokenizer = new Tokenizer(3, nppMock.Object);
            var aTokenList = new List<Tokenizer.TokenTag>();
            foreach (var t in aTokenizer.Tokenize(new RTextTokenTypes[] { RTextTokenTypes.Label }))
            {
                aTokenList.Add(t);
            }
            Tokenizer.TokenTag aFirstLabel = new Tokenizer.TokenTag { BufferPosition = 57, Context = "label:", EndColumn = 63, Line = 3, StartColumn = 57, Type = RTextTokenTypes.Label };
            Assert.AreEqual(aTokenList[0].Context, "label:");
            Assert.AreEqual(aTokenList[0].Line, 3);
            Assert.AreEqual(aTokenList[0].Type, RTextTokenTypes.Label);
            Assert.AreEqual(aTokenList[0].StartColumn, 57);
            Assert.AreEqual(aTokenList[0].EndColumn, 63);
            Assert.AreEqual(aTokenList[0].BufferPosition, 57);
            Assert.AreEqual(aTokenList[1].Context, "type:");
            Assert.AreEqual(aTokenList[1].Line, 3);
            Assert.AreEqual(aTokenList[1].Type, RTextTokenTypes.Label);
            Assert.AreEqual(aTokenList[1].StartColumn, 81);
            Assert.AreEqual(aTokenList[1].EndColumn, 86);
            Assert.AreEqual(aTokenList[1].BufferPosition, 81);
            Assert.True(aFirstLabel.ToString() == "Token : label:\nLine : 3\nStart column : 57\nEnd column : 63\nCaret position at start : 57\nType : Label");
            aTokenizer = new Tokenizer(2, nppMock.Object);
            Assert.AreEqual(aTokenizer.Tokenize().Count(), 6);
        }
        [Test]
        public void TokenizeIDAfterEmptyLine()
        {
            List<string> LinesSample = new List<string>() { "   \n", "ID\n" };
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns<int>(x => LinesSample[x]);
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.IsAny<int>())).Returns(0);
            Tokenizer aTokenizer = new Tokenizer(1, nppMock.Object);
            var aTokenList = new List<Tokenizer.TokenTag>();
            foreach (var t in aTokenizer.Tokenize())
            {
                aTokenList.Add(t);
            }
            Assert.AreEqual(aTokenList.Count(), 2);
            Assert.AreEqual(aTokenList[0].Context, "ID");
            Assert.AreEqual(aTokenList[0].Line, 1);
            Assert.AreEqual(aTokenList[0].Type, RTextTokenTypes.Command);
            Assert.AreEqual(aTokenList[0].StartColumn, 0);
            Assert.AreEqual(aTokenList[0].EndColumn, 2);
            Assert.AreEqual(aTokenList[0].BufferPosition, 0);
            Assert.AreEqual(aTokenList[1].Context, "\n");
            Assert.AreEqual(aTokenList[1].Line, 1);
            Assert.AreEqual(aTokenList[1].Type, RTextTokenTypes.NewLine);
            Assert.AreEqual(aTokenList[1].StartColumn, 2);
            Assert.AreEqual(aTokenList[1].EndColumn, 3);
            Assert.AreEqual(aTokenList[1].BufferPosition, 2);
        }
        [Test, Sequential]
        public void TokenizeIDAfterBrokenLine([Values(",\n", "[\n", "\\\n")] string input)
        {
            List<string> LinesSample = new List<string>() { input, "ID\n" };
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns<int>(x => LinesSample[x]);
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.IsAny<int>())).Returns(0);
            Tokenizer aTokenizer = new Tokenizer(1, nppMock.Object);
            var aTokenList = new List<Tokenizer.TokenTag>();
            foreach (var t in aTokenizer.Tokenize())
            {
                aTokenList.Add(t);
            }
            Assert.AreEqual(aTokenList.Count(), 2);
            Assert.AreEqual(aTokenList[0].Context, "ID");
            Assert.AreEqual(aTokenList[0].Line, 1);
            Assert.AreEqual(aTokenList[0].Type, RTextTokenTypes.Identifier);
            Assert.AreEqual(aTokenList[0].StartColumn, 0);
            Assert.AreEqual(aTokenList[0].EndColumn, 2);
            Assert.AreEqual(aTokenList[0].BufferPosition, 0);
            Assert.AreEqual(aTokenList[1].Context, "\n");
            Assert.AreEqual(aTokenList[1].Line, 1);
            Assert.AreEqual(aTokenList[1].Type, RTextTokenTypes.NewLine);
            Assert.AreEqual(aTokenList[1].StartColumn, 2);
            Assert.AreEqual(aTokenList[1].EndColumn, 3);
            Assert.AreEqual(aTokenList[1].BufferPosition, 2);
        }
        [Test]
        public void CanTokenHaveReference()
        {
            Tokenizer.TokenTag a = new Tokenizer.TokenTag { Type = RTextTokenTypes.Boolean };
            Assert.IsFalse(a.CanTokenHaveReference());
            a.Type = RTextTokenTypes.Reference;
            Assert.IsTrue(a.CanTokenHaveReference());
            a.Type = RTextTokenTypes.Identifier;
            Assert.IsTrue(a.CanTokenHaveReference());
        }
        [Test]
        public void TokenizeUnderCursor()
        {
            List<string> LinesSample = new List<string>() { "   \n", "ID\n" };
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns<int>(x => LinesSample[x]);
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.IsAny<int>())).Returns(0);
            nppMock.Setup(m => m.GetPositionFromMouseLocation()).Returns(-1);
            var aToken = Tokenizer.FindTokenUnderCursor(nppMock.Object);
            Assert.IsNull(aToken.Context);
            nppMock.Setup(m => m.GetPositionFromMouseLocation()).Returns(0);
            nppMock.Setup(m => m.GetLineNumber(It.IsAny<int>())).Returns(1);
            aToken = Tokenizer.FindTokenUnderCursor(nppMock.Object);
            Assert.AreEqual(aToken.Context, "ID");
            nppMock.Setup(m => m.GetPositionFromMouseLocation()).Returns(10);
            aToken = Tokenizer.FindTokenUnderCursor(nppMock.Object);
            Assert.IsNull(aToken.Context);
        }
    }
}