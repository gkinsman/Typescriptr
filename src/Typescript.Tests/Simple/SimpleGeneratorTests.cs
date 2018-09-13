using System;
using System.Collections.Generic;
using Assent;
using Typescriptr;
using Xunit;

namespace Typescript.Tests.Simple
{
    public class SimpleGeneratorTests
    {
        class SimpleTypesOnly
        {
            public int IntType { get; set; }
            public long LongType { get; set; }
            public string StringType { get; set; }
            public decimal DecimalType { get; set; }
        }

        [Fact]
        public void Generator_TypeWithBuiltInPropsOnly_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(SimpleTypesOnly)});

            this.Assent(generated.Types);
        }

        class TypeWithNestedType
        {
            public SimpleTypesOnly SimpleType { get; set; }
        }

        [Fact]
        public void Generator_TypeWithNestedSimpleTypes_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithNestedType)});

            this.Assent(generated.Types);
        }

        class TypeWithPrivateProperties
        {
            private class HiddenInnerType
            {
            }

            private string PrivateStringProp { get; set; }
            private int PrivateIntProp { get; set; }
            private HiddenInnerType PrivateInnerProp { get; set; }
            public Guid PublicGuidProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithHiddenInnerType_OnlyPublicPropertiesShouldBeRendered()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithPrivateProperties)});

            this.Assent(generated.Types);
        }

        struct TestStruct
        {
            public int IntProp { get; }
        }
        
        class TypeWithCustomValueTypeProp
        {
            public TestStruct TestStructProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithCustomValueTypeProp_ShouldRenderCustomTypeSeparately()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithCustomValueTypeProp)});

            this.Assent(generated.Types);
        }
    }
}