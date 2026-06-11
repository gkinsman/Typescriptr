using Typescriptr;
using Xunit;

namespace Typescript.Tests.Ordering.Alpha
{
    public class Zebra
    {
        public string Zulu { get; set; }
    }

    public class Apple : Zebra
    {
        public string Charlie { get; set; }
        public string Bravo { get; set; }
        public string Delta { get; set; }
    }

    public enum Echo
    {
        One,
        Two
    }
}

namespace Typescript.Tests.Ordering.Beta
{
    public class Gamma
    {
        public string Foo { get; set; }
    }

    public enum Foxtrot
    {
        One,
        Two
    }
}

namespace Typescript.Tests.Ordering
{
    public class OutputOrderingTests
    {
        [Fact]
        public void Generator_OrdersTypesByNamespaceThenName_WithBaseBeforeDerived()
        {
            var generator = TypeScriptGenerator.CreateDefault();

            // Deliberately unsorted input: stack order would otherwise render these
            // in the reverse of the desired order.
            var generated = generator.Generate(new[]
            {
                typeof(Alpha.Zebra),
                typeof(Alpha.Apple),
                typeof(Beta.Gamma),
            });

            var types = generated.Types;

            // Namespace grouping: Alpha-namespace types precede Beta-namespace types,
            // even though "Zebra" sorts after "Gamma" alphabetically.
            Assert.True(
                types.IndexOf("interface Zebra") < types.IndexOf("interface Gamma"),
                "Alpha-namespace types should precede Beta-namespace types");

            // Base before derived within a namespace, overriding alphabetical (Apple < Zebra).
            Assert.True(
                types.IndexOf("interface Zebra") < types.IndexOf("interface Apple"),
                "Base type Zebra should precede derived type Apple");

            // Members within an interface sorted ordinally (Bravo, Charlie, Delta).
            var bravo = types.IndexOf("bravo:");
            var charlie = types.IndexOf("charlie:");
            var delta = types.IndexOf("delta:");
            Assert.True(
                bravo < charlie && charlie < delta,
                "Members should be ordered alphabetically");
        }

        [Fact]
        public void Generator_OrdersEnumsByNamespaceThenName()
        {
            var generator = TypeScriptGenerator.CreateDefault();

            var generated = generator.Generate(new[]
            {
                typeof(Alpha.Echo),
                typeof(Beta.Foxtrot),
            });

            var enums = generated.Enums;

            // Alpha-namespace enum precedes Beta-namespace enum.
            Assert.True(
                enums.IndexOf("Echo") < enums.IndexOf("Foxtrot"),
                "Alpha-namespace enum should precede Beta-namespace enum");
        }
    }
}
