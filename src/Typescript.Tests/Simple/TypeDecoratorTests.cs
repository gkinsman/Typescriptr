using Assent;
using Typescriptr;
using Typescriptr.Formatters;
using Xunit;

namespace Typescript.Tests.Simple
{
    public class TypeDecoratorTests
    {
        class SimpleTypesOnly
        {
            public int IntType { get; set; }
            public long LongType { get; set; }
            public string StringType { get; set; }
            public decimal DecimalType { get; set; }
        }
        class TypeWithNestedType
        {
            public SimpleTypesOnly SimpleType { get; set; }
        }
        class TypeWithEnum
        {
            public EnumType AnEnum { get; set; }
        }
        public enum EnumType
        {
            FirstEnum,
            SecondEnum,
            ThirdEnum
        }

        [Fact]
        public void Generator_CommentDecoration_GenerateSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault().WithTypeDecorator(CommentBlockDecorator.Decorate);
            var generated = generator.Generate(new[]
            {
                typeof(SimpleTypesOnly),
                typeof(TypeWithNestedType),
                typeof(TypeWithEnum)
            });
            
            this.Assent(generated.JoinTypesAndEnums());
        }
    }
}