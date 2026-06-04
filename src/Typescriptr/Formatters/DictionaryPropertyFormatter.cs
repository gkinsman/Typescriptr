using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Typescriptr.Exceptions;

namespace Typescriptr.Formatters
{
    public static class DictionaryPropertyFormatter
    {
        public static string KeyValueFormatter(Type type, NullabilityInfo nullability, Func<Type, NullabilityInfo, string> typeNameRenderer)
        {
            var dictType = IsGenericDictionary(type)
                ? type
                : type.GetInterface(typeof(IDictionary<,>).Name)
                  ?? type.GetInterface(typeof(IReadOnlyDictionary<,>).Name);
            var typeArguments = dictType.GenericTypeArguments;
            var keyType = typeArguments[0];
            var valueType = typeArguments[1];

            // The key/value NullabilityInfo is only available when the declared member type is itself
            // the generic dictionary (so the generic args line up); otherwise we have no NRT metadata.
            NullabilityInfo keyNullability = null;
            NullabilityInfo valueNullability = null;
            if (nullability != null && nullability.GenericTypeArguments.Length == 2)
            {
                keyNullability = nullability.GenericTypeArguments[0];
                valueNullability = nullability.GenericTypeArguments[1];
            }

            var keyTypeName = typeNameRenderer(keyType, keyNullability);
            var valueTypeName = typeNameRenderer(valueType, valueNullability);

            var propString = $"{{ [key: {keyTypeName}]: {valueTypeName} }}";
            return propString;
        }

        static bool IsGenericDictionary(Type type) =>
            type.IsGenericType &&
            (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ||
             type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>));
    }
}