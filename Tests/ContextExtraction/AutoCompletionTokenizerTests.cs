using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ContextExtraction.AutoCompletion
{
    using Moq;
    using NUnit.Framework;
    using RTextNppPlugin.Parsing;
    using RTextNppPlugin.Utilities;

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
        
        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerSpace([Values(ContextExtractionSampleInput)] string input,
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

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerCommand([Values(ContextExtractionSampleInput)] string input,
                                                          [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                          [Range(9, 30)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 1);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string>{"        " }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 8);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 30);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, "CalprmElementPrototype");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 8);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Command);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerSpaceAfterCommand([Values(ContextExtractionSampleInput)] string input,
                                                                    [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                    [Range(31, 31)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 2);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype" }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 30);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 31);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, " ");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 30);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Space);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerIdentifier([Values(ContextExtractionSampleInput)] string input,
                                                                    [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                    [Range(32, 55)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 3);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " " }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 31);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 55);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, "cpCahEnableTagePassenger");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 31);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.RTextName);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerComma([Values(ContextExtractionSampleInput)] string input,
                                                                    [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                    [Range(56, 56)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 4);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger" }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 55);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 56);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, ",");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 55);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Comma);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerSpaceAfterComma([Values(ContextExtractionSampleInput)] string input,
                                                                  [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                  [Range(57, 57)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 5);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger", "," }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 56);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 57);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, " ");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 56);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Space);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerLabel([Values(ContextExtractionSampleInput)] string input,
                                                        [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                        [Range(58, 62)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 6);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger", ",", " " }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 57);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 62);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, "type:");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 57);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Label);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerSpaceAfterLabel(  [Values(ContextExtractionSampleInput)] string input,
                                                                    [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                    [Range(63, 63)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 7);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger", ",", " ", "type:" }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 62);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 63);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, " ");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 62);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Space);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerReference([Values(ContextExtractionSampleInput)] string input,
                                                                  [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                  [Range(64, 89)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 8);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger", ",", " ", "type:", " " }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 63);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 89);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, "/AUTOSAR/DataTypes/Boolean");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 63);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Reference);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerSpaceAfterReference([Values(ContextExtractionSampleInput)] string input,
                                                                      [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                      [Range(90, 90)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 9);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger", ",", " ", "type:", " ", "/AUTOSAR/DataTypes/Boolean" }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.StartColumn, 89);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.EndColumn, 90);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Context, " ");
            //buffer position is with start column because offset is 0
            Assert.AreEqual(aTokenizer.TriggerToken.Value.BufferPosition, 89);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Line, 4);
            Assert.AreEqual(aTokenizer.TriggerToken.Value.Type, RTextTokenTypes.Space);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerLeftAngleBracket([Values(ContextExtractionSampleInput)] string input,
                                                                   [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                   [Range(91, 91)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 10);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger", ",", " ", "type:", " ", "/AUTOSAR/DataTypes/Boolean", " " }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue == false);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerNewLine([Values(ContextExtractionSampleInput)] string input,
                                                          [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                          [Range(92, 92)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 11);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger", ",", " ", "type:", " ", "/AUTOSAR/DataTypes/Boolean", " ", "{" }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue == false);
        }

        [Test, Combinatorial]
        public void AutoCompletionTokenizerTriggerBufferPositionOutOfScope( [Values(ContextExtractionSampleInput)] string input,
                                                                            [Values(LastLineLength)] int lengthToEndOfCurrentLine,
                                                                            [Values(-1, 95)] int caretPosition)
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

            Assert.IsTrue(aTokenizer.LineTokens.Count() == 12);
            Assert.IsTrue(aTokenizer.LineTokens.SequenceEqual(new List<string> { "        ", "CalprmElementPrototype", " ", "cpCahEnableTagePassenger", ",", " ", "type:", " ", "/AUTOSAR/DataTypes/Boolean", " ", "{", "\n" }));
            Assert.IsTrue(aTokenizer.TriggerToken.HasValue == false);
        }
    }
}
