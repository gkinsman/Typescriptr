using System;
using System.Linq;
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
            var stringToIndent = string.Join(Environment.NewLine, "ABCD".ToCharArray());
            
            var indented = stringToIndent.IndentEachLine("  ");

            this.Assent(indented);
        }

        [Fact]
        public void IndentEachLine_EmptyLines_ShouldntIndentEmptyLines()
        {
            var stringToIndent = "AB" + Environment.NewLine + Environment.NewLine + "CD";

            var indented = stringToIndent.IndentEachLine("  ");

            this.Assent(indented);
        }
    }
}