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
            var generated = generator.Generate(new[] { typeof(SimpleTypesOnly) });

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
            var generated = generator.Generate(new[] { typeof(TypeWithNestedType) });

            this.Assent(generated.Types);
        }

        class TypeWithPrivateProperties
        {
            private class HiddenInnerType
            { }

            private string PrivateStringProp { get; set; }
            private int PrivateIntProp { get; set; }
            private HiddenInnerType PrivateInnerProp { get; set; }
            public Guid PublicGuidProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithHiddenInnerType_OnlyPublicPropertiesShouldBeRendered()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] { typeof(TypeWithPrivateProperties) });

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
            var generated = generator.Generate(new[] { typeof(TypeWithCustomValueTypeProp) });

            this.Assent(generated.Types);
        }


        class TestTypeWithFields
        {
#pragma warning disable 414
            public string StringField = "123";
            public int IntField = 4;
#pragma warning restore 414
        }

        [Fact]
        public void Generator_TypeWithFields_ShouldRenderFields()
        {
            var generator = TypeScriptGenerator.CreateDefault()
                .WithTypeMembers(MemberType.PropertiesAndFields);
            var generated = generator.Generate(new[] { typeof(TestTypeWithFields) });

            this.Assent(generated.Types);
        }

        class TestTypeWithFieldsAndProps
        {
#pragma warning disable 414
            public string StringField = "123";
            public int IntField = 4;
#pragma warning restore 414
            public string StringProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithFieldsAndPropsButSetToPropsOnly_ShouldOnlyRenderProps()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] { typeof(TestTypeWithFieldsAndProps) });

            this.Assent(generated.Types);
        }

        [Fact]
        public void Generator_UsingModule_ShouldRenderESModule()
        {
            var generator = TypeScriptGenerator.CreateDefault()
                .WithModule("Api");
            var generated = generator.Generate(new []{ typeof(TestTypeWithFields) });

            this.Assent(generated.Types);
        }
    }
}