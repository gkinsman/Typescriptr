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

    public delegate string FormatDictionaryProperty(Type t, NullabilityInfo nullability, Func<Type, NullabilityInfo, string> typeNameRenderer);

    public delegate string FormatCollectionProperty(Type t, NullabilityInfo nullability, Func<Type, NullabilityInfo, string> typeNameRenderer);

    public delegate bool MemberFilter(MemberInfo memberInfo);

    public class TypeScriptGenerator
    {
        public static readonly string TabString = "  ";

        private DecorateType _typeDecorator;
        private FormatEnum _enumFormatter;
        private FormatEnumProperty _enumPropertyFormatter;
        private FormatDictionaryProperty _dictionaryPropertyFormatter;
        private FormatCollectionProperty _collectionPropertyFormatter;
        private MemberFilter _memberFilter;
        
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
            {typeof(DateOnly), "string"},
            {typeof(TimeOnly), "string"},
            {typeof(DayOfWeek), "string"},
            {typeof(TimeSpan), "string"},
            {typeof(Guid), "string"},
            {typeof(object), "unknown"}
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

        public TypeScriptGenerator WithModule(string module)
        {
            _module = module;
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
        private readonly NullabilityInfoContext _nullabilityContext = new NullabilityInfoContext();
        private string _module;
        

        public GenerationResult Generate(IEnumerable<Type> types)
        {
            var typeBuilder = new StringBuilder();
            var enumBuilder = new StringBuilder();

            var typeSegments = new List<RenderedType>();
            var enumSegments = new List<RenderedEnum>();

            foreach (var t in types) _typeStack.Push(t);

            // Discovery: the stack walk finds referenced and base types. Each type/enum is
            // rendered into its own buffer; output order is decided afterwards so that it is
            // deterministic rather than dependent on reflection/stack order.
            while (_typeStack.Any())
            {
                var type = _typeStack.Pop();
                if (_typesGenerated.Contains(type)) continue;

                if (type.IsEnum)
                {
                    var renderedEnum = RenderEnum(type);
                    if (renderedEnum != null) enumSegments.Add(renderedEnum);
                }
                else
                {
                    typeSegments.Add(RenderType(type));
                }
            }

            foreach (var segment in OrderTypes(typeSegments))
                typeBuilder.Append(segment.Text);

            foreach (var segment in OrderEnums(enumSegments))
                enumBuilder.Append(segment.Text);

            if (!string.IsNullOrEmpty(_module))
            {
                typeBuilder = new StringBuilder(typeBuilder.ToString().IndentEachLine(TabString));
                typeBuilder.PrependLine($"declare module '{_module}' {{");
                typeBuilder.AppendLine("}");
            }
            else
            {
                typeBuilder.AppendLine();
            }

            return new GenerationResult(typeBuilder.ToString(), enumBuilder.ToString());
        }

        private RenderedEnum RenderEnum(Type enumType)
        {
            if (_enumNames.Contains(enumType.FullName)) return null;
            var enumString = _enumFormatter(enumType, _quoteStyle);

            var builder = new StringBuilder();
            if (_typeDecorator != null)
                builder.AppendLine(_typeDecorator(enumType));

            builder.Append(enumString);
            _enumNames.Add(enumType.FullName);

            return new RenderedEnum(enumType.Namespace ?? string.Empty, enumType.Name, builder.ToString());
        }

        private RenderedType RenderType(Type type)
        {
            var builder = new StringBuilder();

            var memberTypesToInclude = _memberTypes == MemberType.PropertiesOnly
                ? MemberTypes.Property
                : MemberTypes.Property | MemberTypes.Field;

            // Reflection does not guarantee member order, so sort ordinally for deterministic output.
            var members = type.GetMembers()
                .Where(m => memberTypesToInclude.HasFlag(m.MemberType))
                .OrderBy(m => m.Name, StringComparer.Ordinal);

            bool ShouldExport(Type t)
            {
                return t != typeof(Object)
                    && t != typeof(ValueType)
                    && t != null;
            }
            var baseType = type.BaseType;
            var hasBaseType = ShouldExport(baseType);
            
            if (_typeDecorator != null)
                builder.AppendLine(_typeDecorator(type));

            builder.Append($"export interface ");
            RenderTypeName(builder, type);
            if (hasBaseType) {
                builder.Append($" extends ");
                RenderTypeName(builder, baseType);
            }

            builder.AppendLine(" {");

            foreach (var memberInfo in members)
            {
                Type memberType = null;
                NullabilityInfo memberNullability = null;
                if (memberInfo is PropertyInfo p)
                {
                    memberType = p.PropertyType;
                    memberNullability = _nullabilityContext.Create(p);
                }
                else if (memberInfo is FieldInfo f)
                {
                    memberType = f.FieldType;
                    memberNullability = _nullabilityContext.Create(f);
                }
                if (memberType == null) throw new InvalidOperationException();

                var memberName = memberInfo.Name;
                if (_useCamelCasePropertyNames)
                    memberName = memberName.ToCamelCase();

                if (memberInfo.DeclaringType == type) {

                    if (_memberFilter == null || _memberFilter(memberInfo))
                    {
                        RenderProperty(builder, memberType, memberNullability, memberName);
                    }
                }
            }

            builder.AppendLine("}");
            _typesGenerated.Add(type);

            Type normalizedBaseType = null;
            if (hasBaseType) {
                normalizedBaseType = baseType.IsGenericType ? baseType.GetGenericTypeDefinition() : baseType;
                if (!_typesGenerated.Contains(normalizedBaseType))
                    _typeStack.Push(normalizedBaseType);
            }

            return new RenderedType(type.Namespace ?? string.Empty, type.Name, type, normalizedBaseType, builder.ToString());
        }

        private static IEnumerable<RenderedType> OrderTypes(List<RenderedType> segments)
        {
            return segments
                .GroupBy(s => s.Namespace, StringComparer.Ordinal)
                .OrderBy(g => g.Key, StringComparer.Ordinal)
                .SelectMany(g => OrderWithinNamespace(g.ToList()));
        }

        // Within a namespace, render base types before derived types; otherwise alphabetical.
        // A topological pass over base->derived edges (both ends in this group) with an
        // alphabetical tiebreak: at each step emit the alphabetically-first type whose
        // in-group base has already been emitted.
        private static IEnumerable<RenderedType> OrderWithinNamespace(List<RenderedType> group)
        {
            var inGroup = new HashSet<Type>(group.Select(s => s.Type));
            var remaining = group.OrderBy(s => s.Name, StringComparer.Ordinal).ToList();
            var emitted = new HashSet<Type>();
            var result = new List<RenderedType>();

            bool BaseSatisfied(RenderedType s) =>
                s.BaseType == null
                || !inGroup.Contains(s.BaseType)
                || emitted.Contains(s.BaseType);

            while (remaining.Count > 0)
            {
                var next = remaining.FirstOrDefault(BaseSatisfied);
                if (next == null)
                {
                    // No satisfiable type (would only happen on a cycle); emit the rest alphabetically.
                    result.AddRange(remaining);
                    break;
                }

                result.Add(next);
                emitted.Add(next.Type);
                remaining.Remove(next);
            }

            return result;
        }

        private static IEnumerable<RenderedEnum> OrderEnums(List<RenderedEnum> segments)
        {
            return segments
                .OrderBy(s => s.Namespace, StringComparer.Ordinal)
                .ThenBy(s => s.Name, StringComparer.Ordinal);
        }

        private class RenderedType
        {
            public RenderedType(string ns, string name, Type type, Type baseType, string text)
            {
                Namespace = ns;
                Name = name;
                Type = type;
                BaseType = baseType;
                Text = text;
            }

            public string Namespace { get; }
            public string Name { get; }
            public Type Type { get; }
            public Type BaseType { get; }
            public string Text { get; }
        }

        private class RenderedEnum
        {
            public RenderedEnum(string ns, string name, string text)
            {
                Namespace = ns;
                Name = name;
                Text = text;
            }

            public string Namespace { get; }
            public string Name { get; }
            public string Text { get; }
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

        private string TypeNameRenderer(Type type, NullabilityInfo nullability)
        {
            // Nullable value types (int?) are detected from the runtime type; nullable reference
            // types (string?) only exist in metadata, so we read them from the member's NullabilityInfo.
            var underlyingValueType = Nullable.GetUnderlyingType(type);
            var isNullable = underlyingValueType != null
                || (nullability != null
                    && !type.IsValueType
                    && nullability.ReadState == NullabilityState.Nullable);

            if (underlyingValueType != null)
                type = underlyingValueType;

            Func<string, string> decorate = isNullable ? str => str + " | null" : str => str;

            if (_propTypeMap.ContainsKey(type))
                return decorate(_propTypeMap[type]);

            if (type.IsClosedTypeOf(typeof(IDictionary<,>)) || type.IsClosedTypeOf(typeof(IReadOnlyDictionary<,>)))
                return decorate(_dictionaryPropertyFormatter(type, nullability, TypeNameRenderer));

            if (typeof(IEnumerable).IsAssignableFrom(type))
                return decorate(_collectionPropertyFormatter(type, nullability, TypeNameRenderer));

            var typeName = type.Name;
            if (typeof(Enum).IsAssignableFrom(type))
                typeName = _enumPropertyFormatter(type, _quoteStyle);

            if (!_typesGenerated.Contains(type)) _typeStack.Push(type);

            return decorate(typeName);
        }

        private void RenderProperty(StringBuilder builder, Type propType, NullabilityInfo nullability, string propName)
        {
            var propTypeName = TypeNameRenderer(propType, nullability);
            builder.AppendLine($"{TabString}{propName}: {propTypeName};");
        }

        public TypeScriptGenerator WithMemberFilter(MemberFilter memberFilter)
        {
            _memberFilter = memberFilter;
            return this;
        }
    }
}