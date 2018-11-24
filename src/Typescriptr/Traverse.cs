using System;
using System.Collections.Generic;

namespace Typescriptr
{
    internal static class Traverse
    {
        public static IEnumerable<T> Across<T>(T first, Func<T, T> next) where T : class
        {
            for (var item = first; item != null; item = next(item))
                yield return item;
        }
    }
}