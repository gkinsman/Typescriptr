using System;
using System.Collections.Generic;
using System.Linq;

namespace Typescriptr.Formatters
{
    public class CollectionPropertyFormatter
    {
        public static string Format(Type type, Func<Type, string> typeNameRenderer)
        {
            var typeArgument =
                type.GetElementType() ??
                type.GenericTypeArguments.FirstOrDefault() ??
                type.GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.FirstOrDefault();

            var renderedTypeName = typeNameRenderer(typeArgument);
            return $"{renderedTypeName}[]";
        }
    }
}