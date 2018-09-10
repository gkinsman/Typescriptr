using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Typescriptr
{
    public static class StringExtensions
    {
        public static string ToCamelCase(this string str)
        {
            return $"{str.Substring(0, 1).ToLower()}{str.Substring(1)}";
        }

        public static string IndentEachLine(this string str, string indent)
        {
            str = str.Trim();
            var lines = str.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            var builder = new StringBuilder();
            foreach (var line in lines)
            {
                if (!string.IsNullOrEmpty(line)) builder.AppendLine($"{indent}{line}");
                else builder.AppendLine(line);
            }

            return builder.ToString();
        }

        public static void PrependLine(this StringBuilder builder, string str)
        {
            builder.Insert(0, $"{str}{Environment.NewLine}");
        }
    }
}