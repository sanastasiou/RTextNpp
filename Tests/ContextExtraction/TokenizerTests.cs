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
            "\\ ",
            " ",
            "ID",
            "AUTOSAR {",
            "ARPackage Coding {",
            "ARPackage Interfaces {",
            "      CalprmInterface         cpCahEnableTagePassenger,        label: \"string\",        type: [        1,        2]"};

        const int LastLineLength = 114;

        [Test]
        public void SelectiveTokenizing([Values(ContextExtractionSampleInput)] string input)
        {
            ContextExtractor c = new ContextExtractor(input, 0);
            //adjust column for backend
            Assert.AreEqual((LastLineLength) + 1, c.ContextColumn);
            //first 3 lines are not considered since they are empty
            Assert.AreEqual(ContextLinesSample.Count() - 3, c.ContextList.Count());
            Assert.IsTrue(c.ContextList.SequenceEqual(ContextLinesSample.Skip(Math.Max(0, ContextLinesSample.Count() - 4))));

            var nppMock = new Mock<INpp>();

            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns<int>( x=> ContextLinesSample[x] + "\n");

            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 3))).Returns(0);

            Tokenizer aTokenizer = new Tokenizer(6, nppMock.Object);
            var aTokenList = new List<Tokenizer.TokenTag>();

            foreach (var t in aTokenizer.Tokenize(new RTextTokenTypes[] { RTextTokenTypes.Label }))
            {
                aTokenList.Add(t);
            }

            Tokenizer.TokenTag aFirstLabel = new Tokenizer.TokenTag { BufferPosition = 63, Context = "label:", EndColumn = 69, Line = 3, StartColumn = 63, Type = RTextTokenTypes.Label };

            Assert.AreEqual(aTokenList[0].Context, "label:");
            Assert.AreEqual(aTokenList[0].Line, 6);
            Assert.AreEqual(aTokenList[0].Type, RTextTokenTypes.Label);
            Assert.AreEqual(aTokenList[0].StartColumn, 63);
            Assert.AreEqual(aTokenList[0].EndColumn, 69);
            Assert.AreEqual(aTokenList[0].BufferPosition, 63);

            Assert.AreEqual(aTokenList[1].Context, "type:");
            Assert.AreEqual(aTokenList[1].Line, 6);
            Assert.AreEqual(aTokenList[1].Type, RTextTokenTypes.Label);
            Assert.AreEqual(aTokenList[1].StartColumn, 87);
            Assert.AreEqual(aTokenList[1].EndColumn, 92);
            Assert.AreEqual(aTokenList[1].BufferPosition, 87);

            Assert.True(aFirstLabel.ToString() == "Token : label:\nLine : 3\nStart column : 63\nEnd column : 69\nCaret position at start : 63\nType : Label");

            aTokenizer = new Tokenizer(3, nppMock.Object);

            Assert.AreEqual(aTokenizer.Tokenize().Count(), 4);

            aTokenizer = new Tokenizer(2, nppMock.Object);
            aTokenList.Clear();
            foreach(var t in aTokenizer.Tokenize())
            {
                aTokenList.Add(t);
            }

            Assert.AreEqual(aTokenList[0].Context, "ID");
            Assert.AreEqual(aTokenList[0].Line, 2);
            Assert.AreEqual(aTokenList[0].Type, RTextTokenTypes.RTextName);
            Assert.AreEqual(aTokenList[0].StartColumn, 0);
            Assert.AreEqual(aTokenList[0].EndColumn, 2);
            Assert.AreEqual(aTokenList[0].BufferPosition, 0);

            Assert.AreEqual(aTokenList[1].Context, "\n");
            Assert.AreEqual(aTokenList[1].Line, 2);
            Assert.AreEqual(aTokenList[1].Type, RTextTokenTypes.NewLine);
            Assert.AreEqual(aTokenList[1].StartColumn, 2);
            Assert.AreEqual(aTokenList[1].EndColumn, 3);
            Assert.AreEqual(aTokenList[1].BufferPosition, 2);

            aTokenizer = new Tokenizer(0, nppMock.Object);
            aTokenList.Clear();
            foreach (var t in aTokenizer.Tokenize())
            {
                aTokenList.Add(t);
            }

            Assert.AreEqual(aTokenList[0].Context, "\\");
            Assert.AreEqual(aTokenList[0].Line, 0);
            Assert.AreEqual(aTokenList[0].Type, RTextTokenTypes.Error);
            Assert.AreEqual(aTokenList[0].StartColumn, 0);
            Assert.AreEqual(aTokenList[0].EndColumn, 1);
            Assert.AreEqual(aTokenList[0].BufferPosition, 0);

            Assert.AreEqual(aTokenList[1].Context, " ");
            Assert.AreEqual(aTokenList[1].Line, 0);
            Assert.AreEqual(aTokenList[1].Type, RTextTokenTypes.Space);
            Assert.AreEqual(aTokenList[1].StartColumn, 1);
            Assert.AreEqual(aTokenList[1].EndColumn, 2);
            Assert.AreEqual(aTokenList[1].BufferPosition, 1);

            Assert.AreEqual(aTokenList[2].Context, "\n");
            Assert.AreEqual(aTokenList[2].Line, 0);
            Assert.AreEqual(aTokenList[2].Type, RTextTokenTypes.NewLine);
            Assert.AreEqual(aTokenList[2].StartColumn, 2);
            Assert.AreEqual(aTokenList[2].EndColumn, 3);
            Assert.AreEqual(aTokenList[2].BufferPosition, 2);
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
            Assert.AreEqual(aTokenList[0].Type, RTextTokenTypes.RTextName);
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
    }
}