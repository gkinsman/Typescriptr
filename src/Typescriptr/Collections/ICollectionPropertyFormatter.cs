using System;

namespace Typescriptr.Collections
{
    public interface ICollectionPropertyFormatter
    {
        string Format(Type type, Func<Type, string> typeNameRenderer);
    }
}