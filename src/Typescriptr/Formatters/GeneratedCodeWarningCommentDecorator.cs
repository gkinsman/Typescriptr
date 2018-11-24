using System;

namespace Typescriptr.Formatters
{
    public class GeneratedCodeWarningCommentDecorator
    {
        public static string Decorate(Type type)
        {
            return $"// Source: {type.FullName}";
        }
    }
}