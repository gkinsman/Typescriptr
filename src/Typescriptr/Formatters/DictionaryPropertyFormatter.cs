using System;
using System.Collections;
using System.Collections.Generic;
using Typescriptr.Exceptions;

namespace Typescriptr.Formatters
{
    public static class DictionaryPropertyFormatter
    {
        public static string KeyValueFormatter(Type type, Func<Type, string> typeNameRenderer)
        {
            var dictType = type.GetInterface(typeof(IDictionary<,>).Name);
            var typeArguments = dictType.GenericTypeArguments;
            var keyType = typeArguments[0];
            var valueType = typeArguments[1];

            var keyTypeName = typeNameRenderer(keyType);
            var valueTypeName = typeNameRenderer(valueType);

            var propString = $"{{ [key: {keyTypeName}]: {valueTypeName} }}";
            return propString;
        }
    }
}