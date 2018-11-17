using System;
using Assent;
using Typescriptr;
using Typescriptr.Formatters;
using Xunit;
using Xunit.Abstractions;

namespace Typescript.Tests.Inheritence
{
    public class InheritenceGeneratorTests
    {
        class BaseClass
        {
            public string Property { get; set; }
        }

        class TypeWithBaseClass : BaseClass
        {
            
        }

        [Fact]
        public void Generator_TypeWithBaseClass_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithBaseClass) });

            this.Assent(generated.Types);
        }


        class TypeWithGenericParent : GenericParent<TypeWithGenericParent>
        {

        }
        class GenericParent<T>
        {
        }

        [Fact]
        public void Generator_TypeWithGenericParent_ShouldRenderValidTypescript()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] { typeof(TypeWithGenericParent) });

            this.Assent(generated.Types);
        }
    }
}