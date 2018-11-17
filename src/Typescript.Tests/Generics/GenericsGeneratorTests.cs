using System;
using Assent;
using Typescriptr;
using Typescriptr.Formatters;
using Xunit;
using Xunit.Abstractions;

namespace Typescript.Tests.Generics
{
    public class GenericsGeneratorTests
    {
        class Alpha { }
        class Beta { }

        class BaseType<T1, T2>
        {
        }

        class TypeWithGenericArguments : BaseType<Alpha, Beta>
        {
        }

        [Fact]
        public void Generator_TypeWithGenericArguments_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(TypeWithGenericArguments) });

            var result = generated.Types;

            this.Assent(result);
        }

        class TypeWithOpenGenericArguments<T>
        {
        }

        [Fact]
        public void Generator_TypeWithOpenGenericArguments_GeneratesSuccessfully()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] { typeof(TypeWithOpenGenericArguments<>) });

            var result = generated.Types;

            this.Assent(result);
        }

    }
}