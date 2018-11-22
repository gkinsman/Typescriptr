using System;
using System.Text;

namespace Typescriptr.Enums
{
    public class KeyOfObjectEnumFormatter : IEnumFormatter
    {
        public string Start() {
            return "function mkenum<T extends {[index: string]: U}, U extends string>(x: T) { return x; }"
                   + Environment.NewLine;
        }

        public string FormatType(Type enumType, QuoteStyle quoteStyle) {
            var builder = new StringBuilder();

            var typeName = enumType.Name;

            builder.AppendLine($"const {enumType.Name} = mkenum({{");
            foreach (var enumName in enumType.GetEnumNames()) {
                var value = quoteStyle == QuoteStyle.Single ? $"'{enumName}'" : $"\"{enumName}\"";
                builder.AppendLine($"{TypeScriptGenerator.TabString}{enumName}: {value},");
            }

            builder.AppendLine("});");
            builder.AppendLine($"export type {typeName} = (typeof {typeName})[keyof typeof {typeName}];");

            return builder.ToString();
        }

        public string FormatProperty(Type enumType, QuoteStyle quoteStyle) {
            return EnumPropertyFormatters.UnionStringEnumPropertyTypeFormatter(enumType, quoteStyle);
        }

        public string End() {
            return string.Empty;
        }
    }
}