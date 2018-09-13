using System;
using System.Linq;
using System.Text;

namespace Typescriptr.Formatters
{
    public static class EnumFormatter
    {
        public static string UnionStringEnumPropertyTypeFormatter(Type enumType, QuoteStyle quoteStyle)
        {
            var quote = quoteStyle == QuoteStyle.Double ? "\"" : "'";
            var names = enumType.GetEnumNames();
            return string.Join(" | ", names.Select(n => $"{quote}{n}{quote}"));
        }
         
        public static string ValueNamedEnumFormatter(Type enumType, QuoteStyle quoteStyle)
        {
            var builder = new StringBuilder();
            
            builder.AppendLine($"enum {enumType.Name} {{");

            foreach (var enumName in enumType.GetEnumNames())
            {
                var value = quoteStyle == QuoteStyle.Single ? $"'{enumName}'" : $"\"{enumName}\"";
                builder.AppendLine($"{TypeScriptGenerator.TabString}{enumName} = {value},");
            }

            builder.AppendLine("}");

            return builder.ToString();
        }

        public static string ValueNumberEnumFormatter(Type enumType, QuoteStyle quoteStyle)
        {
            var builder = new StringBuilder();

            builder.AppendLine($"enum {enumType.Name} {{");

            foreach (var enumName in enumType.GetEnumNames())
            {
                var value = (int)Enum.Parse(enumType, enumName);
                builder.AppendLine($"{TypeScriptGenerator.TabString}{enumName} = {value},");
            }

            builder.AppendLine("}");

            return builder.ToString();
        }
    }
}