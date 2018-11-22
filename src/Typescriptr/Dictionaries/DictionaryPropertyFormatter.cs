using System;
using System.Collections.Generic;

namespace Typescriptr.Dictionaries
{
    public interface IDictionaryPropertyFormatter
    {
        string Format(Type type, Func<Type, string> typeNameRenderer);
    }

    public class KeyValueDictionaryPropertyFormatter : IDictionaryPropertyFormatter
    {
        public string Format(Type type, Func<Type, string> typeNameRenderer) {
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