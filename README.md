# Typescriptr
A C# to TypeScript converter that focuses on ease of use and client side awesomeness.

## Yet another C# to TypeScript converter?!
There are a few options out there for generating TypeScript currently, and they might work for you! This library focuses on 'fitting in' with TypeScript to make the developer experience of using server types in TS projects _awesome_. 

Specifically, it does these things:
- It renders IDictionary<TKey,TValue> to `{ [key: TKey]: TValue }` out of the box
- It renders IEnumerable<T> to `T[]` out of the box 
- Enum values are rendered as strings instead of numbers, and are generated separately from regular types.
- Enum properties are rendered as string unions `propName: 'EnumVal1' | 'EnumVal2' | 'EnumVal3';`.
- It is small and flexible: you can easily override the defaults below.

## Scope
This project is specifically designed to render TypeScript from C# for the purpose of providing type safety over DTOs. It is explicitly not designed to be a general C#-to-TypeScript converter. Decisions on feature additions to this library will take that guiding purpose into account.


## Getting Started

[`Install-Package Typescriptr`](https://www.nuget.org/packages/Typescriptr/)

1) Create a TypeScript generator with the default config:
```csharp
var generator = TypeScriptGenerator.CreateDefault();
```

2) Extract the types from your assemblies that you'd like to compile to TypeScript, and then pass them through the generator:
```csharp
var typesToGenerate = 
  typeof(MyApiClient).Assembly.ExportedTypes
    .Where(type => typeof(ApiDto).IsAssignableFrom(type));

var generationResult = generator.Generate(typesToGenerate);
```

3) Do whatever you like with the output Types and Enums.
For example, write Types to a type definition file, and Enums to a regular TypeScript file:

```csharp
using(var fs = File.Create("types.d.ts"))
using(var tw = new StreamWriter(fs)) {
	tw.Write(result.Types);
}

using(var fs = File.Create("enums.ts"))
using(var tw = new StreamWriter(fs)) {
	tw.Write(result.Enums);
}
```


## Defaults

By default, Typescriptr will map the following types when created with the `CreateDefault` method:
```csharp
public static TypeScriptGenerator CreateDefault() => new TypeScriptGenerator()
            .WithPropertyTypeFormatter<DateTime>(t => "string")
            .WithPropertyTypeFormatter<DayOfWeek>(t => "string")
            .WithPropertyTypeFormatter<TimeSpan>(t => "string")
            .WithPropertyTypeFormatter<Guid>(t => "string")
            .WithPropertyTypeFormatter<DateTimeOffset>(t => "string")
            .WithEnumFormatter(EnumFormatter.ValueNamedEnumFormatter, EnumFormatter.UnionStringEnumPropertyTypeFormatter)
            .WithQuoteStyle(QuoteStyle.Single)
            .WithDictionaryPropertyFormatter(DictionaryPropertyFormatter.KeyValueFormatter)
            .WithCollectionPropertyFormatter(CollectionPropertyFormatter.Format)
            .WithNamespace("Api")
            .WithCamelCasedPropertyNames();
```

## Enums

I'm a firm believer in using strings for Enums in APIs, as it makes them usable without needing to refer to documentation or code to understand the meaning of an enum's value. In .NET with JSON.NET, that means using the `StringEnumConverter` to convert Enums to strings.

In TypeScript, enums can be a bit of a pain to work when rendering .NET types into TS interfaces, as TS interfaces have no code output: they are just a way to provide intellisense and compilation errors over results returned from a server API. When writing code against those interfaces however, we also want to be able to use enums as values.

To that end, Typescriptr by default will render enum values as strings, and enum property types as string unions, and will provide them as separately rendered output in the generator result. This achieves the best of both worlds, as everything is just strings, yet still statically typed.

```csharp
class TypeWithEnum
{
    public enum EnumType
    {
        FirstEnum,
        SecondEnum,
        ThirdEnum
    }
    public EnumType AnEnum { get; set; }
}
```

produces `result.Types`:
```TypeScript
declare namespace Api {
  interface TypeWithEnum {
    anEnum: 'FirstEnum' | 'SecondEnum' | 'ThirdEnum';
  }
}
```
and `result.Enums`:
```Typescript
enum EnumType {
  FirstEnum = 'FirstEnum',
  SecondEnum = 'SecondEnum',
  ThirdEnum = 'ThirdEnum',
}
```

#### Notes
- It is possible to override this behaviour, and to provide custom formatters for enum properties and values. A numeric value formatter is [built in](https://github.com/gkinsman/Typescriptr/blob/master/src/Typescriptr/Formatters/EnumFormatter.cs), however there is no support for how to handle numeric values on properties, as referencing enums from a type definition file is error-prone.

## Dictionaries

Any object properties assignable to IDictionary will be converted into TypeScript object syntax. 

#### Notes
- Keys must be resolvable to either TypeScript `number` or `string` (as those are the only allowed types for TS indexes). Custom type resolvers can be added using `WithPropertyTypeFormatter`.
- Any type passed to the generator that is assignable to IDictionary will be rendered poorly, as it wouldn't make much sense. If you must, you can provide a custom type formatter using [`WithPropertyTypeFormatter`].(https://github.com/gkinsman/Typescriptr/blob/master/src/Typescriptr/TypeScriptGenerator.cs#L56)
- Non-generic IDictionary isn't supported

```csharp
class TypeWithDictionaryProp
{
    public Dictionary<string, int> DictProp { get; set; }
}
```
produces
```TypeScript
declare namespace Api {
  interface TypeWithDictionaryProp {
    dictProp: { [key: string]: number };
  }
}
```

## Collections

Any object properties assignable to IEnumerable will be converted into TypeScript arrays.

```csharp
class TypeWithArrayProp
{
    public string[] ArrayProp { get; set; }
}
```
produces:
```TypeScript
declare namespace Api {
  interface TypeWithArrayProp {
    arrayProp: string[];
  }
}
```

#### Notes
- As with IDictionary types, it is recommended to not have API types inherit directly from the interface, but to have properties of the type, else the expected output doesn't make much sense.


## Nullable Value Types

Nullable value types are rendered to optional properties in TypeScript:

```csharp
class TypeWithNullable
{
    public int? NullableInt { get; set; }
    public Guid? NullableGuid { get; set; }
}
```

```TypeScript
declare namespace Api {
  interface TypeWithNullable {
    nullableInt?: number;
    nullableGuid?: string;
  }
}
```

## Limitations

### Inheritance

Inheritance isn't directly supported by Typescriptr, in that types passed into the generator that have base clases will not, in the output, also derive from that base class. Instead, base types that aren't `object` or `ValueType` will be rendered with all of their respective publicly available properties. The resulting effect is the same as with inheritance, but with slightly larger code output, and simpler generator code.

### Generics

Top level types with type parameters are not supported at all. Properties with generic type parameters may be mapped with custom property type formatters, but a non-generic base type is required to match the type on. This scenario is fairly obscure and untested, as generics are rarely a good idea for DTO's.
