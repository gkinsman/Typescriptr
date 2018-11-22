using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Typescriptr.Collections;
using Typescriptr.Dictionaries;
using Typescriptr.Enums;
using Typescriptr.Exceptions;

namespace Typescriptr
{
    public class TypeScriptGenerator
    {
        public static readonly string TabString = "  ";

        IEnumFormatter _enumFormatter;
        IDictionaryPropertyFormatter _dictionaryPropertyFormatter;
        ICollectionPropertyFormatter _collectionPropertyFormatter;

        QuoteStyle _quoteStyle;
        MemberType _memberTypes;
        bool _useCamelCasePropertyNames;

        readonly Dictionary<Type, string> _propTypeMap = new Dictionary<Type, string>()
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

        TypeScriptGenerator()
        {
        }

        public static TypeScriptGenerator CreateDefault() => new TypeScriptGenerator()
            .WithPropertyTypeFormatter<DateTimeOffset>(t => "string")
            .WithEnumFormatter(new KeyOfObjectEnumFormatter())
            .WithQuoteStyle(QuoteStyle.Single)
            .WithTypeMembers(MemberType.PropertiesOnly)
            .WithDictionaryPropertyFormatter(new KeyValueDictionaryPropertyFormatter())
            .WithCollectionPropertyFormatter(new GenericTypeCollectionPropertyFormatter())
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

        public TypeScriptGenerator WithNamespace(string @namespace)
        {
            _namespace = @namespace;
            return this;
        }

        public TypeScriptGenerator WithDictionaryPropertyFormatter(IDictionaryPropertyFormatter formatter)
        {
            _dictionaryPropertyFormatter = formatter;
            return this;
        }

        public TypeScriptGenerator WithCollectionPropertyFormatter(ICollectionPropertyFormatter formatter)
        {
            _collectionPropertyFormatter = formatter;
            return this;
        }

        public TypeScriptGenerator WithEnumFormatter(IEnumFormatter enumFormatter)
        {
            _enumFormatter = enumFormatter;
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

        readonly HashSet<Type> _typesGenerated = new HashSet<Type>();
        readonly HashSet<string> _enumNames = new HashSet<string>();
        readonly Stack<Type> _typeStack = new Stack<Type>();
        string _namespace;
        
        public GenerationResult Generate(IEnumerable<Type> types)
        {
            var typeBuilder = new StringBuilder();
            var enumBuilder = new StringBuilder();

            enumBuilder.AppendLine(_enumFormatter.Start());
            
            foreach (var t in types) _typeStack.Push(t);

            while (_typeStack.Any())
            {
                var type = _typeStack.Pop();
                if (_typesGenerated.Contains(type)) continue;

                if (type.IsEnum) RenderEnum(enumBuilder, type);
                else RenderType(typeBuilder, type);
            }

            enumBuilder.Append(_enumFormatter.End());

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
        
        void RenderEnum(StringBuilder builder, Type enumType)
        {
            var enumString = _enumFormatter.FormatType(enumType, _quoteStyle);
            builder.AppendLine(enumString);
            _enumNames.Add(enumType.Name);
        }

        void RenderType(StringBuilder builder, Type type)
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

        void RenderTypeName(StringBuilder builder, Type type)
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

        string TypeNameRenderer(Type type)
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
                return decorate(_dictionaryPropertyFormatter.Format(type, TypeNameRenderer));

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return decorate(_collectionPropertyFormatter.Format(type, TypeNameRenderer));

            var typeName = type.Name;
            if (typeof(Enum).IsAssignableFrom(type))
                typeName = _enumFormatter.FormatProperty(type, _quoteStyle);

            if (!_typesGenerated.Contains(type)) _typeStack.Push(type);

            return decorate(typeName);
        }

        void RenderProperty(StringBuilder builder, Type propType, string propName)
        {
            var propTypeName = TypeNameRenderer(propType);
            builder.AppendLine($"{TabString}{propName}: {propTypeName};");
        }
    }
}