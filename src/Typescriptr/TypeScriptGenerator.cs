using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Typescriptr.Formatters;

namespace Typescriptr
{
    public delegate string DecorateType(Type t);

    public delegate string FormatEnum(Type t, QuoteStyle quoteStyle);

    public delegate string FormatEnumProperty(Type t, QuoteStyle quoteStyle);

    public delegate string FormatDictionaryProperty(Type t, Func<Type, string> typeNameRenderer);

    public delegate string FormatCollectionProperty(Type t, Func<Type, string> typeNameRenderer);


    public class TypeScriptGenerator
    {
        public static readonly string TabString = "  ";

        private DecorateType _typeDecorator;
        private FormatEnum _enumFormatter;
        private FormatEnumProperty _enumPropertyFormatter;
        private FormatDictionaryProperty _dictionaryPropertyFormatter;
        private FormatCollectionProperty _collectionPropertyFormatter;
        
        private QuoteStyle _quoteStyle;
        private MemberType _memberTypes;
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
            {typeof(DateTime), "string"},
            {typeof(DayOfWeek), "string"},
            {typeof(TimeSpan), "string"},
            {typeof(Guid), "string"},
        };

        private TypeScriptGenerator()
        {
        }

        public static TypeScriptGenerator CreateDefault() => new TypeScriptGenerator()
            .WithPropertyTypeFormatter<DateTimeOffset>(t => "string")
            .WithEnumFormatter(EnumFormatter.ValueNamedEnumFormatter,
                EnumFormatter.UnionStringEnumPropertyTypeFormatter)
            .WithQuoteStyle(QuoteStyle.Single)
            .WithTypeMembers(MemberType.PropertiesOnly)
            .WithDictionaryPropertyFormatter(DictionaryPropertyFormatter.KeyValueFormatter)
            .WithCollectionPropertyFormatter(CollectionPropertyFormatter.Format)
            .WithNamespace("Api")
            .WithCamelCasedPropertyNames();

        public TypeScriptGenerator WithTypeMembers(MemberType memberTypes)
        {
            _memberTypes = memberTypes;
            return this;
        }
        
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

        public TypeScriptGenerator WithTypeDecorator(DecorateType decorator)
        {
            _typeDecorator = decorator;
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
            if (_typeDecorator != null)
            {
                builder.AppendLine(_typeDecorator(enumType));
            }
            builder.Append(enumString);
            _enumNames.Add(enumType.Name);
        }

        private void RenderType(StringBuilder builder, Type type)
        {
            var memberTypesToInclude = _memberTypes == MemberType.PropertiesOnly
                ? MemberTypes.Property
                : MemberTypes.Property | MemberTypes.Field;

            var members = type.GetMembers().Where(m => memberTypesToInclude.HasFlag(m.MemberType));

            bool ShouldExport(Type t)
            {
                return t != typeof(Object)
                    && t != typeof(ValueType)
                    && t != null;
            }
            var baseType = type.BaseType;
            var hasBaseType = ShouldExport(baseType);

            if (_typeDecorator != null)
            {
                builder.AppendLine(_typeDecorator(type));
            }

            builder.Append($"interface ");
            RenderTypeName(builder, type);
            if (hasBaseType) {
                builder.Append($" extends ");
                RenderTypeName(builder, baseType);
            }

            builder.AppendLine(" {");

            foreach (var memberInfo in members)
            {
                Type memberType = null;
                if (memberInfo is PropertyInfo p)
                    memberType = p.PropertyType;
                else if (memberInfo is FieldInfo f)
                    memberType = f.FieldType;
                if (memberType == null) throw new InvalidOperationException();
                
                var memberName = memberInfo.Name;
                if (_useCamelCasePropertyNames)
                    memberName = memberName.ToCamelCase();

                if (memberInfo.DeclaringType == type) {
                    RenderProperty(builder, memberType, memberName);
                }
            }

            builder.AppendLine("}");
            _typesGenerated.Add(type);

            if (hasBaseType) {
                var addedType = baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType;
                if (!_typesGenerated.Contains(addedType))
                    _typeStack.Push(addedType);
            }
        }

        private void RenderTypeName(StringBuilder builder, Type type)
        {
            var friendlyName = type.Name;
            if (type.IsGenericType) {
                var backtickIndex = friendlyName.IndexOf('`');
                if (backtickIndex > 0) {
                    builder.Append(friendlyName.Remove(backtickIndex));
                }
                builder.Append("<");
                var typeParameters = type.GetGenericArguments();
                for (var i = 0; i < typeParameters.Length; ++i) {
                    if (i > 0) { builder.Append(", "); }
                    RenderTypeName(builder, typeParameters[i]);
                }
                builder.Append(">");
            } else {
                builder.Append(friendlyName);
            }
        }

        private string TypeNameRenderer(Type type)
        {
            Func<string, string> decorate = str => str;

            if (Nullable.GetUnderlyingType(type) != null)
            {
                type = Nullable.GetUnderlyingType(type);
                decorate = str => str + " | null";
            }

            if (_propTypeMap.ContainsKey(type))
                return decorate(_propTypeMap[type]);

            if (typeof(IDictionary).IsAssignableFrom(type))
                return decorate(_dictionaryPropertyFormatter(type, TypeNameRenderer));

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return decorate(_collectionPropertyFormatter(type, TypeNameRenderer));

            var typeName = type.Name;
            if (typeof(Enum).IsAssignableFrom(type))
                typeName = _enumPropertyFormatter(type, _quoteStyle);

            if (!_typesGenerated.Contains(type)) _typeStack.Push(type);

            return decorate(typeName);
        }

        private void RenderProperty(StringBuilder builder, Type propType, string propName)
        {
            var propTypeName = TypeNameRenderer(propType);
            builder.AppendLine($"{TabString}{propName}: {propTypeName};");
        }
    }
}