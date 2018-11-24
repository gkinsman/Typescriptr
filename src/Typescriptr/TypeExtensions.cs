using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Typescriptr
{
    internal static class TypeExtensions
    {
        public static bool IsClosedTypeOf(this Type @this, Type openGeneric)
        {
            return TypesAssignableFrom(@this).Any(t =>
            {
                if (!t.GetTypeInfo().IsGenericType || @this.GetTypeInfo().ContainsGenericParameters) return false;
                return t.GetGenericTypeDefinition() == openGeneric;
            });
        }

        private static IEnumerable<Type> TypesAssignableFrom(Type candidateType)
        {
            return candidateType.GetTypeInfo().ImplementedInterfaces
                .Concat(Traverse.Across(candidateType, t => t.GetTypeInfo().BaseType));
        }
    }
}