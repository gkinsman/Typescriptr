using System;

namespace Typescriptr.Formatters
{
    public class CommentBlockDecorator
    {
        public static string Decorate(Type type)
        {
            return $@"/*
 * NB: Do not edit this class manually
 * Source: {type.FullName}
 * Generated using Typescriptr https://github.com/gkinsman/Typescriptr
 */";
        }
    }
}