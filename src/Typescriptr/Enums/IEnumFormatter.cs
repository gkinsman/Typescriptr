using System;

namespace Typescriptr.Enums
{
    public interface IEnumFormatter
    {
        string Start();
        string FormatType(Type enumType, QuoteStyle quoteStyle);
        string FormatProperty(Type enumType, QuoteStyle quoteStyle);
        string End();
    }
}