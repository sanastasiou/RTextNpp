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
        [Test, Combinatorial]
        public void InvalidArguments([Values(null, "")] string input,
                                     [Values(null, 0, -1)] int lengthToEndOfCurrentLine)
        {
            ContextExtractor c = new ContextExtractor(input, lengthToEndOfCurrentLine);

            Assert.AreEqual(0, c.ContextColumn);
            Assert.AreEqual(0, c.ContextList.Count());
        }

        const string SingleLineContext = "      PPortPrototype control, providedInterface: /actuator/IActuatorHornControl {";        

        [Test, Combinatorial]
        public void ValidArgument_OneLineInput( [Values(SingleLineContext)] string input,
                                                [Range(0, 81)] int lengthToEndOfCurrentLine)
        {
            ContextExtractor c = new ContextExtractor(input, lengthToEndOfCurrentLine);

            //adjust column for backend
            Assert.AreEqual((input.Length - lengthToEndOfCurrentLine) + 1, c.ContextColumn);
            Assert.AreEqual(1, c.ContextList.Count());
            Assert.AreEqual(input, c.ContextList.Last());
        }

        [Test]
        public void Invalid_Length_Till_EOF([Values(SingleLineContext)] string input,
                                            [Values(82)] int lengthToEndOfCurrentLine)
        {
            ContextExtractor c = new ContextExtractor(input, lengthToEndOfCurrentLine);

            //adjust column for backend
            Assert.AreEqual(0, c.ContextColumn);
            Assert.AreEqual(0, c.ContextList.Count());
        }

        const string MultipleLineContext = @"      #some commment
                                                   @some notation
                                                   PPortPrototype control,\ 
                                                   checksum: ""bla"",

                                                   providedInterface: /actuator/IActuatorHornControl {";

        const string ExpectedMultilineContext = "                                                   PPortPrototype control,                                                   checksum: \"bla\",                                                   providedInterface: /actuator/IActuatorHornControl {";

        /// <summary>
        /// Check if the context column is correctly reported for various cursor position in the string
        //  Length of string without comment and notation is 243, backend columns start from 1.
        //  So if the cursor is at the start of the string before PPort the column is 1,
        //  if the cursor is at the very last position the cursor is 243 + 1
        /// </summary>
        /// <param name="input"></param>
        /// <param name="lengthToEndOfCurrentLine"></param>
        [Test, Sequential]
        public void ValidArguments_MultilineInput([Values(33, 51, 58, 0, 243)] int lengthToEndOfCurrentLine,
                                                  [Values(211, 193, 186, 244, 1)] int expectedColumn      )
        {            
            ContextExtractor c = new ContextExtractor(MultipleLineContext, lengthToEndOfCurrentLine);

            //adjust column for backend
            Assert.AreEqual(expectedColumn, c.ContextColumn);
            Assert.AreEqual(1, c.ContextList.Count());
            Assert.AreEqual(ExpectedMultilineContext, c.ContextList.Last());
        }

        const string MultipleLineContextArray = @" A { 
                                                    LOL type : 3,
                                                    b: [ 
                                                        c1,c2,
                                                        c3
                                                       ]";
        const string ExpectedMultilineArrayString = "                                                    LOL type : 3,                                                    b: [                                                         c1,c2,                                                        c3                                                       ]";

        [Test, Sequential]
        public void ValidArguments_BreakAfterLastEleemnt([Values(0, 10, 298)] int lengthToEndOfCurrentLine,
                                                         [Values(299, 289, 1)] int expectedColumn)
        {
            ContextExtractor c = new ContextExtractor(MultipleLineContextArray, lengthToEndOfCurrentLine);

            //adjust column for backend
            Assert.AreEqual(expectedColumn, c.ContextColumn);
            Assert.AreEqual(2, c.ContextList.Count());
            Assert.AreEqual("A {", c.ContextList.ElementAt(0));
            Assert.AreEqual(ExpectedMultilineArrayString, c.ContextList.ElementAt(1));
        }

        const string ComplexAnalysisText = @"#Some comment...
@file-extension: ecuextract
#some another comment...
AUTOSAR {
  ARPackage Coding {
                                      
    ARPackage Interfaces {
      CalprmInterface ICafCalprm {
        label:
            bla
        label:
            foo
        CalprmElementPrototype cpCahEnableTagePassenger, type: /AUTOSAR/DataTypes/Boolean {
          SwDataDefProps swCalibrationAccess: readOnly, swImplPolicy: standard, swVariableAccessImplPolicy: optimized, compuMethod: /Coding/DataTypes/cpCahEnableTagePassenger_Semantic
        }
		CalprmElementPrototype bla {
			desc: [2,k [
                label:
                    [2,3
                  1], label: 23]
		}
        foo:
		CalprmElementPrototype ";

        System.Collections.Generic.List<string> ContextLines = new System.Collections.Generic.List<string> { "AUTOSAR {", "ARPackage Coding {", "ARPackage Interfaces {", "CalprmInterface ICafCalprm {", "foo:", "\t\tCalprmElementPrototype " };

        [Test]
        public void ValidArguments_ComplexAnalysis()
        {
            ContextExtractor c = new ContextExtractor(ComplexAnalysisText, 0);

            Assert.AreEqual(true, c.ContextList.SequenceEqual(ContextLines));
        }


        [Test, Sequential]
        public void SingleSeparatorContext([Values("\\", ",", "[", "]", "")] string input,
                                           [Values(1,2,2,2, 0)] int column,
                                           [Values(1, 1, 1, 1, 0)] int contextLines)
        {
            ContextExtractor c = new ContextExtractor(input, 0);
            Assert.AreEqual(column, c.ContextColumn);
            Assert.AreEqual(contextLines, c.ContextList.Count());
        }
    }
}
