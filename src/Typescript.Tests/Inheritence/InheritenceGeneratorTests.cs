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

            var result = generated.Types;

            this.Assent(result);
        }

    }
}