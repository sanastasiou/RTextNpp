using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
namespace Tests.Utilities
{
    using NUnit.Framework;
    using RTextNppPlugin.RText.Parsing;
    using RTextNppPlugin.Utilities;
    using Moq;
    using MoqExtensions;
    using RTextNppPlugin.RText;
    using RTextNppPlugin.Scintilla;
    [TestFixture]
    class TokenEqualityComparerTests
    {
        List<string> ContextLinesSample = new List<string>
        {
            "AUTOSAR {",
            "ARPackage Coding {",
            "ARPackage Interfaces {",
            "CalprmInterface ICafCalprm {",
            "        CalprmElementPrototype cpCahEnableTagePassenger, type: /AUTOSAR/DataTypes/Boolean {"};
        [Test]
        public void InitializationTest()
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            Assert.IsNotNull(aComparer);
        }
        List<Tokenizer.TokenTag> expectedTokensSpace = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space }
        };
        const string FILE_DUMMY = "dummy.atm";
        [Test]
        public void TestSpace([Range(0, 8)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensSpace, 0, FILE_DUMMY));
            Assert.IsTrue(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
        }
        [Test]
        public void TestTokenAfterSpace([Range(9, 30)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensSpace, 0, FILE_DUMMY));
            Assert.IsTrue(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
        }
        [Test]
        public void TestSecondTokenAfterSpace([Range(31, 31)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensSpace, 0, FILE_DUMMY));
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
        }
        List<Tokenizer.TokenTag> expectedTokensTriggerCommand = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command }
        };
        [Test]
        public void TestSecondTokenAfterSpaceFirstTokenParsed([Range(31, 55)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerCommand, 30, FILE_DUMMY));
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
        }
        [Test]
        public void TestThirdTokenAfterSpaceFirstTokenParsed([Range(56, 56)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerCommand, 0, FILE_DUMMY));
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
        }
        List<Tokenizer.TokenTag> expectedTokensTriggerIdentifier = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command },
            new Tokenizer.TokenTag { BufferPosition = 30, Context = " ", EndColumn = 31, Line = 4, StartColumn = 30, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 31, Context = "cpCahEnableTagePassenger", EndColumn = 55, Line = 4, StartColumn = 31, Type = RTextTokenTypes.Identifier }
        };
        [Test]
        public void TestThirdTokenAfterSpaceFirstTwoTokensParsed([Range(56, 56)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerIdentifier, 55, FILE_DUMMY));
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
        }
        List<Tokenizer.TokenTag> expectedTokensTriggerComma = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command },
            new Tokenizer.TokenTag { BufferPosition = 30, Context = " ", EndColumn = 31, Line = 4, StartColumn = 30, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 31, Context = "cpCahEnableTagePassenger", EndColumn = 55, Line = 4, StartColumn = 31, Type = RTextTokenTypes.Identifier },
            new Tokenizer.TokenTag { BufferPosition = 55, Context = ",", EndColumn = 56, Line = 4, StartColumn = 55, Type = RTextTokenTypes.Comma }
        };
        [Test]
        public void TestThirdTokenAfterSpaceFirstThreeTokensParsed([Range(57, 61)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, 57, FILE_DUMMY));
            Assert.IsTrue(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
        }
        [Test]
        public void TestThirdTokenAfterSpaceFirstThreeTokensParsedLabelEdge([Range(61, 63)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerLabel, 62, FILE_DUMMY));
            if(caretPosition == 62)
            {
                Assert.IsTrue(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
            }
            else
            {
                Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
            }
        }
        List<Tokenizer.TokenTag> expectedTokensTriggerLabel = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command },
            new Tokenizer.TokenTag { BufferPosition = 30, Context = " ", EndColumn = 31, Line = 4, StartColumn = 30, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 31, Context = "cpCahEnableTagePassenger", EndColumn = 55, Line = 4, StartColumn = 31, Type = RTextTokenTypes.Identifier },
            new Tokenizer.TokenTag { BufferPosition = 55, Context = ",", EndColumn = 56, Line = 4, StartColumn = 55, Type = RTextTokenTypes.Comma },
            new Tokenizer.TokenTag { BufferPosition = 56, Context = " ", EndColumn = 57, Line = 4, StartColumn = 56, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 57, Context = "type:", EndColumn = 62, Line = 4, StartColumn = 57, Type = RTextTokenTypes.Label }
        };
        [Test]
        public void TestThirdTokenAfterSpaceFirstThreeTokensParsedAfterLabel([Range(64, 89)] int caretPosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerLabel, 62, FILE_DUMMY));
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
        }
        List<Tokenizer.TokenTag> expectedTokensTriggerReference = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command },
            new Tokenizer.TokenTag { BufferPosition = 30, Context = " ", EndColumn = 31, Line = 4, StartColumn = 30, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 31, Context = "cpCahEnableTagePassenger", EndColumn = 55, Line = 4, StartColumn = 31, Type = RTextTokenTypes.Identifier },
            new Tokenizer.TokenTag { BufferPosition = 55, Context = ",", EndColumn = 56, Line = 4, StartColumn = 55, Type = RTextTokenTypes.Comma },
            new Tokenizer.TokenTag { BufferPosition = 56, Context = " ", EndColumn = 57, Line = 4, StartColumn = 56, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 57, Context = "type:", EndColumn = 62, Line = 4, StartColumn = 57, Type = RTextTokenTypes.Label },
            new Tokenizer.TokenTag { BufferPosition = 62, Context = " ", EndColumn = 63, Line = 4, StartColumn = 62, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 63, Context = "/AUTOSAR/DataTypes/Boolean", EndColumn = 89, Line = 4, StartColumn = 63, Type = RTextTokenTypes.Reference }
        };
        List<Tokenizer.TokenTag> expectedTokensTriggerReferenceReferenceIncomplete = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command },
            new Tokenizer.TokenTag { BufferPosition = 30, Context = " ", EndColumn = 31, Line = 4, StartColumn = 30, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 31, Context = "cpCahEnableTagePassenger", EndColumn = 55, Line = 4, StartColumn = 31, Type = RTextTokenTypes.Identifier },
            new Tokenizer.TokenTag { BufferPosition = 55, Context = ",", EndColumn = 56, Line = 4, StartColumn = 55, Type = RTextTokenTypes.Comma },
            new Tokenizer.TokenTag { BufferPosition = 56, Context = " ", EndColumn = 57, Line = 4, StartColumn = 56, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 57, Context = "type:", EndColumn = 62, Line = 4, StartColumn = 57, Type = RTextTokenTypes.Label },
            new Tokenizer.TokenTag { BufferPosition = 62, Context = " ", EndColumn = 63, Line = 4, StartColumn = 62, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 63, Context = "/AUTOSAR/DataTypes/Boo", EndColumn = 89, Line = 4, StartColumn = 63, Type = RTextTokenTypes.Reference }
        };
        List<Tokenizer.TokenTag> expectedTokensTriggerReferenceReferenceAugmented = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command },
            new Tokenizer.TokenTag { BufferPosition = 30, Context = " ", EndColumn = 31, Line = 4, StartColumn = 30, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 31, Context = "cpCahEnableTagePassenger", EndColumn = 55, Line = 4, StartColumn = 31, Type = RTextTokenTypes.Identifier },
            new Tokenizer.TokenTag { BufferPosition = 55, Context = ",", EndColumn = 56, Line = 4, StartColumn = 55, Type = RTextTokenTypes.Comma },
            new Tokenizer.TokenTag { BufferPosition = 56, Context = " ", EndColumn = 57, Line = 4, StartColumn = 56, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 57, Context = "type:", EndColumn = 62, Line = 4, StartColumn = 57, Type = RTextTokenTypes.Label },
            new Tokenizer.TokenTag { BufferPosition = 62, Context = " ", EndColumn = 63, Line = 4, StartColumn = 62, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 63, Context = "/AUTOSAR/DataTypes/BooleanValue", EndColumn = 89, Line = 4, StartColumn = 63, Type = RTextTokenTypes.Reference }
        };
        List<Tokenizer.TokenTag> expectedTokensTriggerReferenceReferenceFalseType = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command },
            new Tokenizer.TokenTag { BufferPosition = 30, Context = " ", EndColumn = 31, Line = 4, StartColumn = 30, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 31, Context = "cpCahEnableTagePassenger", EndColumn = 55, Line = 4, StartColumn = 31, Type = RTextTokenTypes.Identifier },
            new Tokenizer.TokenTag { BufferPosition = 55, Context = ",", EndColumn = 56, Line = 4, StartColumn = 55, Type = RTextTokenTypes.Comma },
            new Tokenizer.TokenTag { BufferPosition = 56, Context = " ", EndColumn = 57, Line = 4, StartColumn = 56, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 57, Context = "type:", EndColumn = 62, Line = 4, StartColumn = 57, Type = RTextTokenTypes.Label },
            new Tokenizer.TokenTag { BufferPosition = 62, Context = " ", EndColumn = 63, Line = 4, StartColumn = 62, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 63, Context = "/AUTOSAR/DataTypes/BooleanValue", EndColumn = 89, Line = 4, StartColumn = 63, Type = RTextTokenTypes.Command }
        };
        List<Tokenizer.TokenTag> expectedTokensTriggerReferenceReferenceMinimumContextNotEqual = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command },
            new Tokenizer.TokenTag { BufferPosition = 30, Context = " ", EndColumn = 31, Line = 4, StartColumn = 30, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 31, Context = "cpCahEnableTagePassenger", EndColumn = 55, Line = 4, StartColumn = 31, Type = RTextTokenTypes.Identifier },
            new Tokenizer.TokenTag { BufferPosition = 55, Context = ",", EndColumn = 56, Line = 4, StartColumn = 55, Type = RTextTokenTypes.Comma },
            new Tokenizer.TokenTag { BufferPosition = 56, Context = " ", EndColumn = 57, Line = 4, StartColumn = 56, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 57, Context = "type:", EndColumn = 62, Line = 4, StartColumn = 57, Type = RTextTokenTypes.Error },
            new Tokenizer.TokenTag { BufferPosition = 62, Context = " ", EndColumn = 63, Line = 4, StartColumn = 62, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 63, Context = "/AUTOSAR/DataTypes/BooleanValue", EndColumn = 89, Line = 4, StartColumn = 63, Type = RTextTokenTypes.Reference }
        };
        List<Tokenizer.TokenTag> expectedTokensTriggerReferenceReferenceSingleToken = new List<Tokenizer.TokenTag>
        {
            new Tokenizer.TokenTag { BufferPosition = 0, Context = "        ", EndColumn = 8, Line = 4, StartColumn = 0, Type = RTextTokenTypes.Space },
            new Tokenizer.TokenTag { BufferPosition = 8, Context = "CalprmElementPrototype", EndColumn = 30, Line = 4, StartColumn = 8, Type = RTextTokenTypes.Command }
        };
        [Test, Sequential]
        public void TestNewRequestIfTokensChange([Values(65, 58)] int caretPosition, [Values(58, 65)] int primePosition)
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            var nppMock = new Mock<INpp>();
            //mock last line
            nppMock.Setup(m => m.GetLine(It.IsAny<int>())).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i == 4))).Returns(ContextLinesSample.Last() + "\n");
            nppMock.Setup(m => m.GetLine(It.Is<int>(i => i != 4))).Returns(ContextLinesSample[3] + "\n");
            //for the sake of simplicity we assume that offset is 0
            nppMock.Setup(m => m.GetLineStart(It.Is<int>(i => i == 4))).Returns(0);
            AutoCompletionTokenizer aTokenizer = new AutoCompletionTokenizer(4, caretPosition, nppMock.Object);
            //prime the comparer - token is the reference
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, caretPosition, FILE_DUMMY));
            //caret inside label
            aTokenizer = new AutoCompletionTokenizer(4, primePosition, nppMock.Object);
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, primePosition, FILE_DUMMY));
            //caret inside label
            Assert.IsTrue(aComparer.AreTokenStreamsEqual(aTokenizer.LineTokens, primePosition, FILE_DUMMY));
        }
        [Test]
        public void TestNewRequestIfTokensIsExtendedOrDeleted()
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            //prime the comparer - token is the reference
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerReference, 80, FILE_DUMMY));
            Assert.IsTrue(aComparer.AreTokenStreamsEqual(expectedTokensTriggerReferenceReferenceIncomplete, 80, FILE_DUMMY));
            //caret inside label
            Assert.IsTrue(aComparer.AreTokenStreamsEqual(expectedTokensTriggerReferenceReferenceAugmented, 80, FILE_DUMMY));
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerReferenceReferenceFalseType, 80, FILE_DUMMY));
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerReferenceReferenceMinimumContextNotEqual, 80, FILE_DUMMY));
        }
        [Test]
        public void TestNewRequestIfTokensIsExtendedOrDeletedSingleToken()
        {
            TokenEqualityComparer aComparer = new TokenEqualityComparer();
            //prime the comparer - token is the reference
            Assert.IsFalse(aComparer.AreTokenStreamsEqual(expectedTokensTriggerReferenceReferenceSingleToken, 80, FILE_DUMMY));
            Assert.IsTrue(aComparer.AreTokenStreamsEqual(expectedTokensTriggerReferenceReferenceSingleToken, 80, FILE_DUMMY));
        }
    }
}
