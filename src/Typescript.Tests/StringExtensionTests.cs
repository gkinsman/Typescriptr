using System.Text;
using Assent;
using Typescriptr;
using Xunit;

namespace Typescript.Tests
{
    public class StringExtensionTests
    {
        [Fact]
        public void IndentEachLine_NoEmptyLines_ShouldIndentAll()
        {
            var stringToIndent = @"
A
B
C
D";
            var indented = stringToIndent.IndentEachLine("  ");

            this.Assent(indented);
        }

        [Fact]
        public void IndentEachLine_EmptyLines_ShouldntIndentEmptyLines()
        {
            var stringToIndent = @"
A

B
C

D";

            var indented = stringToIndent.IndentEachLine("  ");

            this.Assent(indented);
        }
    }
}