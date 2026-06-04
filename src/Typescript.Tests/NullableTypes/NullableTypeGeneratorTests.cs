#nullable enable
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

            public NestedType NestedThing { get; set; } = null!;
        }

        [Fact]
        public void Generator_TypeWithNestedNullable_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithNestedNullable)});

            this.Assent(generated.Types);
        }

        class ReferenceType {}

        class TypeWithNullableReferenceType
        {
            public ReferenceType? ReferenceType { get; set; }
        }

        [Fact]
        public void Generator_TypeWithNullableReferenceType_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithNullableReferenceType)});

            this.Assent(generated.Types);
        }

        class TypeWithNullableInCollection
        {
            public System.Collections.Generic.List<string?> Tags { get; set; } = null!;
        }

        [Fact]
        public void Generator_TypeWithNullableInCollection_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithNullableInCollection)});

            this.Assent(generated.Types);
        }

        class TypeWithNullableDictionaryValue
        {
            public System.Collections.Generic.IDictionary<string, ReferenceType?> Map { get; set; } = null!;
        }

        [Fact]
        public void Generator_TypeWithNullableDictionaryValue_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithNullableDictionaryValue)});

            this.Assent(generated.Types);
        }
    }
}