# Typescriptr
A C# to TypeScript converter that focuses on ease of use and assimilation with TypeScript idioms

## Yet another C# to TypeScript converter?!
There are a few options out there for generating TypeScript currently, and they might work for you! This library focuses on 'fitting in' with TypeScript to make the developer experience of using types in TS projects _awesome_. 

Specifically, it does these things:
- Enum values are rendered as strings, instead of numbers. I'm a firm believer that using strings in API's is much more user friendly than numbers. You can switch to numbers if you want, as there's built in support for it.
  - While enum properties are string unions, enums types are also provided for outputting to a `.ts` file which will be compiled to JS, leaving the interfaces in your `.d.ts` file alone.
- Enum properties are rendered as string unions `propName: 'EnumVal1' | 'EnumVal2' | 'EnumVal3';`. Again, overridable.
- It is small. The generator is basically one class that only handles primitives, maps (IDictionary), collections (IEnumerable), and custom types.
- It's hopefully easy to customise and change. Going forward I'll add to it as required, but for now it has a simple core.


## TODO
- Docs
- Build/Nuget
