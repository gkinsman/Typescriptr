using System;
using Assent;
using Typescriptr;
using Xunit;

namespace Typescript.Tests.Enums
{
    public class EnumGeneratorTests
    {
        class TypeWithEnum
        {
            public enum EnumType
            {
                FirstEnum,
                SecondEnum,
                ThirdEnum
            }

            public EnumType AnEnum { get; set; }
        }

        [Fact]
        public void Generator_TypeWithEnum_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithEnum)});

            var result = string.Join($"{Environment.NewLine}---{Environment.NewLine}", generated.Types,
                generated.Enums);

            this.Assent(result);
        }
        
        class TypeWithNullableEnum
        {
            public enum EnumType
            {
                FirstEnum,
                SecondEnum,
                ThirdEnum,
            }

            public EnumType? NullableEnumProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithNullableEnum_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithNullableEnum)});

            this.Assent(generated.JoinTypesAndEnums());
        }


        class TypeWithValuedEnum
        {
            public enum EnumType
            {
                First = 1,
                Second = 2,
                Third = 3,
            }
            
            public EnumType EnumWithValueProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithEnumAndEnumValueFormatter_RendersValues()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithValuedEnum)});

            this.Assent(generated.JoinTypesAndEnums());
        }

        
        class TypeOneWithEnum
        {
            public SharedEnumType AnEnum { get; set; }
        }
        class TypeTwoWithEnum
        {
            public SharedEnumType AnEnum { get; set; }
        }

        public enum SharedEnumType
        {
            FirstEnum,
            SecondEnum,
            ThirdEnum
        }
        
        [Fact]
        public void Generator_TypesWithSharedEnum_ShouldOnlyGenerateTheEnumOnce()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeOneWithEnum), typeof(TypeTwoWithEnum)});

            this.Assent(generated.JoinTypesAndEnums());
        }
    }
}