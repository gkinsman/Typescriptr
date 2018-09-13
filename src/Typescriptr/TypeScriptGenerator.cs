using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Typescriptr.Exceptions;
using Typescriptr.Formatters;

namespace Typescriptr
{
    public delegate string FormatEnum(Type t, QuoteStyle quoteStyle);
    public delegate string FormatEnumProperty(Type t, QuoteStyle quoteStyle);
    public delegate string FormatDictionaryProperty(Type t, Func<Type, string> typeNameRenderer);
    public delegate string FormatCollectionProperty(Type t, Func<Type, string> typeNameRenderer);

    public class TypeScriptGenerator
    {
        public static readonly string TabString = "  ";

        private FormatEnum _enumFormatter;
        private FormatEnumProperty _enumPropertyFormatter;
        private FormatDictionaryProperty _dictionaryPropertyFormatter;
        private FormatCollectionProperty _collectionPropertyFormatter;

        private bool _useCamelCasePropertyNames;

        private readonly Dictionary<Type, string> _propTypeMap = new Dictionary<Type, string>()
        {
            {typeof(int), "number"},
            {typeof(long), "number"},
            {typeof(ulong), "number"},
            {typeof(short), "number"},
            {typeof(ushort), "number"},
            {typeof(double), "number"},
            {typeof(float), "number"},
            {typeof(byte), "number"},
            {typeof(decimal), "number"},
            {typeof(sbyte), "number"},
            {typeof(uint), "number"},

            {typeof(char), "string"},
            {typeof(string), "string"},

            {typeof(bool), "boolean"},
        };

        private TypeScriptGenerator()
        {
        }

        public static TypeScriptGenerator CreateDefault() => new TypeScriptGenerator()
            .WithPropertyTypeFormatter<DateTime>(t => "string")
            .WithPropertyTypeFormatter<DayOfWeek>(t => "string")
            .WithPropertyTypeFormatter<TimeSpan>(t => "string")
            .WithPropertyTypeFormatter<Guid>(t => "string")
            .WithPropertyTypeFormatter<DateTimeOffset>(t => "string")
            .WithEnumFormatter(EnumFormatter.ValueNamedEnumFormatter, EnumFormatter.UnionStringEnumPropertyTypeFormatter)
            .WithQuoteStyle(QuoteStyle.Single)
            .WithDictionaryPropertyFormatter(DictionaryPropertyFormatter.KeyValueFormatter)
            .WithCollectionPropertyFormatter(CollectionPropertyFormatter.Format)
            .WithNamespace("Api")
            .WithCamelCasedPropertyNames();

        public TypeScriptGenerator WithCamelCasedPropertyNames(bool useCamelCasedPropertyNames = true)
        {
            _useCamelCasePropertyNames = useCamelCasedPropertyNames;
            return this;
        }

        public TypeScriptGenerator WithQuoteStyle(QuoteStyle style)
        {
            _quoteStyle = style;
            return this;
        }
        
        public TypeScriptGenerator WithNamespace(string @namespace)
        {
            _namespace = @namespace;
            return this;
        }
        
        public TypeScriptGenerator WithDictionaryPropertyFormatter(FormatDictionaryProperty formatter)
        {
            _dictionaryPropertyFormatter = formatter;
            return this;
        }

        public TypeScriptGenerator WithCollectionPropertyFormatter(FormatCollectionProperty formatter)
        {
            _collectionPropertyFormatter = formatter;
            return this;
        }

        public TypeScriptGenerator WithEnumFormatter(FormatEnum typeFormatter, FormatEnumProperty propertyFormatter)
        {
            _enumFormatter = typeFormatter;
            _enumPropertyFormatter = propertyFormatter;
            return this;
        }

        public TypeScriptGenerator WithPropertyTypeFormatter<TType>(Func<Type, string> clientType)
        {
            var type = typeof(TType);
            if (_propTypeMap.ContainsKey(typeof(TType)))
                _propTypeMap[typeof(TType)] = clientType(type);
            else
                _propTypeMap.Add(typeof(TType), clientType(type));

            return this;
        }

        private readonly HashSet<Type> _typesGenerated = new HashSet<Type>();
        private readonly HashSet<string> _enumNames = new HashSet<string>();
        private readonly Stack<Type> _typeStack = new Stack<Type>();
        private string _namespace;
        private QuoteStyle _quoteStyle;

        public GenerationResult Generate(IEnumerable<Type> types)
        {
            var typeBuilder = new StringBuilder();
            var enumBuilder = new StringBuilder();

            foreach (var t in types) _typeStack.Push(t);

            while (_typeStack.Any())
            {
                var type = _typeStack.Pop();
                if (_typesGenerated.Contains(type)) continue;

                if (type.IsEnum) RenderEnum(enumBuilder, type);
                else RenderType(typeBuilder, type);
            }

            if (!string.IsNullOrEmpty(_namespace))
            {
                typeBuilder = new StringBuilder(typeBuilder.ToString().IndentEachLine(TabString));
                typeBuilder.PrependLine($"declare namespace {_namespace} {{");
                typeBuilder.AppendLine("}");
            }
            else
            {
                typeBuilder.AppendLine();
            }
            
            return new GenerationResult(typeBuilder.ToString(), enumBuilder.ToString());
        }

        private void RenderEnum(StringBuilder builder, Type enumType)
        {
            var enumString = _enumFormatter(enumType, _quoteStyle);
            builder.Append(enumString);
            _enumNames.Add(enumType.Name);
        }

        private void RenderType(StringBuilder builder, Type type)
        {
            var properties = type.GetProperties();
            builder.AppendLine($"interface {type.Name} {{");

            foreach (var prop in properties)
            {
                var propType = prop.PropertyType;
                var propName = prop.Name;

                if (_useCamelCasePropertyNames)
                    propName = propName.ToCamelCase();
                if (Nullable.GetUnderlyingType(propType) != null)
                    propName = $"{propName}?";

                RenderProperty(builder, propType, propName);
            }

            builder.AppendLine("}");
            _typesGenerated.Add(type);

            var baseType = type.BaseType;
            if (baseType != typeof(Object) && baseType != typeof(ValueType) && baseType != null)
                if (!_typesGenerated.Contains(baseType))
                    _typeStack.Push(baseType);
        }

        private string TypeNameRenderer(Type type)
        {
            if (Nullable.GetUnderlyingType(type) != null)
                type = Nullable.GetUnderlyingType(type);

            if (_propTypeMap.ContainsKey(type))
                return _propTypeMap[type];
            
            if (typeof(IDictionary).IsAssignableFrom(type))
                return _dictionaryPropertyFormatter(type, TypeNameRenderer);
    
            if (typeof(IEnumerable).IsAssignableFrom(type))
                return _collectionPropertyFormatter(type, TypeNameRenderer);

            var typeName = type.Name;
            if (typeof(Enum).IsAssignableFrom(type))
                typeName = _enumPropertyFormatter(type, _quoteStyle);

            if (!_typesGenerated.Contains(type)) _typeStack.Push(type);
            
            return typeName;
        }

        private void RenderProperty(StringBuilder builder, Type propType, string propName)
        {
            var propTypeName = TypeNameRenderer(propType);
            builder.AppendLine($"{TabString}{propName}: {propTypeName};");
        }
    }
}