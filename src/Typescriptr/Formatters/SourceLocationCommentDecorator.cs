using System;

namespace Typescriptr.Formatters
{
    public class SourceLocationCommentDecorator
    {
        public static string Decorate(Type type)
        {
            return $"// Source: {type.FullName}";
        }
    }
}