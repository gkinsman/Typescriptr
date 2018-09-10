using System.Collections.Generic;
using Assent;
using Typescriptr;
using Xunit;

namespace Typescript.Tests.CollectionTypes
{
    public class CollectionGeneratorTests
    {
        class TypeWithArrayProp
        {
            public string[] ArrayProp { get; set; }
        }
        
        [Fact]
        public void Generator_TypeWithArrayProperty_ShouldRenderToArray()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithArrayProp)});

            this.Assent(generated.Types);
        }

        class TypeWithEnumerableProp
        {
            public IEnumerable<int> EnumerableProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithEnumerableProp_ShouldRenderToArray()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithEnumerableProp)});

            this.Assent(generated.Types);
        }

        class IsEnumerable : List<int> {}
        
        class TypeWithPropThatInheritsFromIEnumerable
        {
            public IsEnumerable DerivedFromList { get; set; }
        }

        [Fact]
        public void Generator_TypeWithPropThatDerivesFromEnumerable_ShouldRenderToArray()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithPropThatInheritsFromIEnumerable)});

            this.Assent(generated.Types);
        }
    }
}