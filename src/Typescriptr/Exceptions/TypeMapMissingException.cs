using System;

namespace Typescriptr.Exceptions
{
    public class TypeMapMissingException : Exception
    {
        public TypeMapMissingException(Type unmappedType)
        {
            UnmappedType = unmappedType;
        }

        public Type UnmappedType { get; }

        public override string Message => $"Type named {UnmappedType.Name} is missing a mapping to a TypeScript type. Add it using `WithTypeFormatter`";
    }
}