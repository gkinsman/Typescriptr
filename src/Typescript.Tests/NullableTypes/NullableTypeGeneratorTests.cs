using System;
using Assent;
using Typescriptr;
using Xunit;

namespace Typescript.Tests.NullableTypes
{
    public class NullableTypeGeneratorTests
    {
        class TypeWithNullable
        {
            public int? NullableInt { get; set; }
            public Guid? NullableGuid { get; set; }
        }

        [Fact]
        public void Generator_TypeWithNullable_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithNullable)});

            this.Assent(generated.Types);
        }
        
        class TypeWithNestedNullable
        {
            public class NestedType
            {
                public int? NullableInt { get; set; }
                public DateTime NullableDateTime { get; set; }
            }

            public NestedType NestedThing { get; set; }
        }

        [Fact]
        public void Generator_TypeWithNestedNullable_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithNestedNullable)});

            this.Assent(generated.Types);
        }
    }
}