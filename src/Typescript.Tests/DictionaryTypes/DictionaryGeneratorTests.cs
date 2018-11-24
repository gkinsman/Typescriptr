using System.Collections.Generic;
using Assent;
using Typescriptr;
using Xunit;

namespace Typescript.Tests.DictionaryTypes
{
    public class DictionaryGeneratorTests
    {
        class TypeWithDictionaryProp
        {
            public Dictionary<string, int> DictProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithDirectDictionaryProp_ShouldRenderToTypescriptMap()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithDictionaryProp)});

            this.Assent(generated.Types);
        }

        class ComplexType
        {
            public string AProp { get; set; }
        }
        
        class TypeWithComplexDictionaryValue
        {
            public Dictionary<int, ComplexType> DictProp { get; set; }
        }

        [Fact]
        public void Generator_TypeWithComplexDictionaryValueType_ShouldRenderToTypescriptMap()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithComplexDictionaryValue)});

            this.Assent(generated.Types);
        }

        class TypeWithCustomDictionaryProp
        {
            public ICustomDictionary DictProp { get; set; }
        }

        interface ICustomDictionary : IDictionary<string, int>
        {
        }
        
        [Fact]
        public void Generator_TypeWithCustomDictionaryValueType_ShouldRenderToTypescriptMap()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithCustomDictionaryProp)});

            this.Assent(generated.JoinTypesAndEnums());
        }

        enum TestEnum
        {
            FirstValue,
            SecondValue,
            ThirdValue,
        }

        class TypeWithDictionaryAndEnumPropAsValue
        {
            public Dictionary<string, TestEnum> EnumDictionary { get; set; }
        }
        
        [Fact]
        public void Generator_TypeWithEnumValueType_ShouldRenderToTypescriptMap()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithDictionaryAndEnumPropAsValue)});

            this.Assent(generated.JoinTypesAndEnums());
        }
    }
}