using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Typescriptr.Formatters
{
    public class CollectionPropertyFormatter
    {
        public static string Format(Type type, NullabilityInfo nullability, Func<Type, NullabilityInfo, string> typeNameRenderer)
        {
            var typeArgument =
                type.GetElementType() ??
                type.GenericTypeArguments.FirstOrDefault() ??
                type.GetInterface(typeof(IEnumerable<>).Name).GenericTypeArguments.FirstOrDefault();

            // Element nullability lives in ElementType for arrays, GenericTypeArguments[0] for List<T> etc.
            NullabilityInfo elementNullability = null;
            if (nullability != null)
                elementNullability = nullability.ElementType
                    ?? (nullability.GenericTypeArguments.Length > 0 ? nullability.GenericTypeArguments[0] : null);

            var renderedTypeName = typeNameRenderer(typeArgument, elementNullability);

            // Parenthesise unions so the array marker binds correctly: (string | null)[], not string | null[].
            return renderedTypeName.Contains(" | ")
                ? $"({renderedTypeName})[]"
                : $"{renderedTypeName}[]";
        }
    }
}