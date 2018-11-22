using System;
using System.Linq;

namespace Typescriptr.Enums
{
    public static class EnumPropertyFormatters
    {
        public static string UnionStringEnumPropertyTypeFormatter(Type enumType, QuoteStyle quoteStyle) {
            var quote = quoteStyle == QuoteStyle.Double ? "\"" : "'";
            var names = enumType.GetEnumNames();
            return string.Join(" | ", names.Select(n => $"{quote}{n}{quote}"));
        }
    }
}