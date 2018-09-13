using System;
using Typescriptr;

namespace Typescript.Tests
{
    public static class TestHelpers
    {
        public static string JoinTypesAndEnums(this GenerationResult result)
        {
            return string.Join($"{Environment.NewLine}---{Environment.NewLine}", result.Types,
                result.Enums);
        }
    }
}