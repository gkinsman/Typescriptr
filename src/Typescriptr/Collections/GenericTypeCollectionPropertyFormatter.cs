using System;
using System.Collections.Generic;
using System.Linq;

namespace Typescriptr.Collections
{
    public class GenericTypeCollectionPropertyFormatter : ICollectionPropertyFormatter
    {
        public string Format(Type type, Func<Type, string> typeNameRenderer) {
            var typeArgument =
                type.GetElementType() ??
                type.GenericTypeArguments.FirstOrDefault() ??
                type.GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.FirstOrDefault();

            var renderedTypeName = typeNameRenderer(typeArgument);
            return $"{renderedTypeName}[]";
        }
    }
}