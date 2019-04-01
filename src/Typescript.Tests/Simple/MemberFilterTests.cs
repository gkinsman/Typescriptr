using System;
using System.Reflection;
using Assent;
using Typescriptr;
using Xunit;

namespace Typescript.Tests.Simple
{
    public class MemberFilterTests
    {
        private class IgnoreAttribute : Attribute {}
        
        private class SimpleClass
        {
            public int SerializedType { get; set; }
            [Ignore] public int IgnoredType { get; set; }
        }

        [Fact]
        public void WhenNoFilterSpecified_TypeIsEmitted()
        {
            var generator = TypeScriptGenerator.CreateDefault();
            var generated = generator.Generate(new[] {typeof(SimpleClass)});
            
            this.Assent(generated.JoinTypesAndEnums());
        }

        [Fact]
        public void WhenFilterSpecified_PropertyIsIgnored()
        {
            var generator = TypeScriptGenerator
                    .CreateDefault()
                    .WithMemberFilter(memberInfo => memberInfo.GetCustomAttribute<IgnoreAttribute>() == null)
                ;
            var generated = generator.Generate(new[] {typeof(SimpleClass)});
            
            this.Assent(generated.JoinTypesAndEnums());
        }
    }
}