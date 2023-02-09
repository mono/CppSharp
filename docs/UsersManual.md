CppSharp Users Manual

# 1. Introduction

## What does it do?

This tool allows you to generate .NET bindings that wrap C/C++ code allowing
interoperability with managed languages. This can be useful if you have an
existing native codebase and want to add scripting support, or want to consume
an existing native library in your managed code.

## Why reinvent the wheel?

There are not many automated binding tools around, the only real alternative is
SWIG. So how is it different from SWIG?

 * Cleaner bindings
 * No need to generate a C layer to interop with C++.
 * Based on an actual C++ parser (Clang) so very accurate.
 * Understands C++ at the ABI (application binary interface) level
 * Supports virtual method overriding
 * Easily extensible semantics via user passes
 * Strongly-typed customization APIs
 * Can be used as a library

# 2. Generator Backends

The backend of the bindings generator is abstracted and can support several different targets.

It can be changed by using the `Options.GeneratorKind` option.

For .NET we support these bindings technologies:

- C# (P/Invoke)
- C++/CLI

There is also experimental support for these JavaScript-related targets:

- N-API (Node.js)
- QuickJS
- TypeScript
- Emscripten

# 3. Native Targets

The parser supports several different targets and needs to be correctly configured to match the compiled native code.

This can be done by using the `ParserOptions.TargetTriple` option.

These triples follow the same format as LLVM/Clang, which is documented [here](https://clang.llvm.org/docs/CrossCompilation.html#target-triple).

Here are a few examples for the most common variants:

- `i686-pc-win32-msvc`
- `x86_64-pc-win32-msvc`
- `i686-linux-gnu`
- `x86_64-linux-gnu`
- `i686-apple-darwin`
- `x86_64-apple-darwin`


# 3. C/C++ language features

In this section we will go through how the generator deals C/C++ types.

## Fundamental types

### Integral types

  - `char`        **→** `byte` / `System::Byte`
  - `bool`        **→** `bool` / `System::Boolean`
  - `short`       **→** `short` / `System::Int16`
  - `int`, `long` **→** `int` / `System::Int32`
  - `long long`   **→** `long` / `System::Int64`
  
  Note: Signedness is also preserved in the conversions.

  These size of these types are dependent on environment and compiler, so the mappings
  above are only representative of some environments (the specific data model is usually
  abbreviated as LP32, ILP32, LLP64, LP64).

  Please check the fundamental types properties table at [cppreference.com](http://en.cppreference.com/w/cpp/language/types)
  for more information about this.


### Floating-point types

  - `float`     **→** `float` / `System::Single`
  - `double`    **→** `double` / `System::Double`

### Other types

  - `wchar_t`   **→** `char` / `System::Char`
  - `void`      **→** `void` / `System::Void`

## Derived types

### Arrays

 These are mapped to .NET CLR arrays.

### Pointers

#### Function Pointers / Pointers to Members

These are mapped to .NET CLR delegates.

This is implemented by the [`DelegatesPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/DelegatesPass.cs) pass.

#### Primitive types

 These are mapped to .NET CLR references unless:

  - `void*`        **→** `System.IntPtr` / `System::IntPtr`
  - `const char*`  **→** `string` / `System::String`

Regular non-wide strings are assumed to be ASCII by default (marshaled with .NET `Marshal.PtrToStringAnsi`).
Wide strings are marshaled either as UTF-16 or UTF-32, depending on the width of `wchar_t` for the target.

This behavior can be overriden by using the `Options.Encoding` setting.

#### References

 References are mapped to .NET CLR references just like pointers.

## Typedefs

We do not preserve type definitions since .NET and its main language C# do not
have the concept of type aliases like C/C++. There is an exception in the case
of a typedef'd function (pointer) declaration. In this case we generate a .NET
delegate with the name of the typedef.

## Enums

Regular C/C++ enums are translated to .NET enumerations.

### Anonymous enums

C and C++ enums do not introduce their own scope (different from C++11 strongly
typed enums). This means the enumerated values will leak into an outer context,
like a class or a namespace. When this is detected, the generator tries to map
to an outer enclosing context and generate a new name.

### Flags / Bitfields
 
Some enumerations represent bitfield patterns. The generator tries to check for
this with some heuristics. If there are enough values in the enum to make a good
guess, we apply the `[Flags]` .NET attribute to the wrapper enum.

This is implemented by the [`CheckFlagEnumsPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/CheckFlagEnumsPass.cs) pass.

## Functions

Since global scope functions are not supported in C# (though they are available
in the CLR) they are mapped as a static function in a class, to be consumable by
any CLS-compliant language.

By default all globals functions of a translation unit are mapped to a static
class with the name of of the unit prefixed by the namespace.

We also provide special passes that try to move these free functions either as
instance or static functions of some class.

See the [`FunctionToInstanceMethodPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/FunctionToInstanceMethodPass.cs) and [`FunctionToStaticMethodPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/FunctionToStaticMethodPass.cs) passes.

### Variadic arguments (TODO)

C/C++ variadic arguments need careful handling because they are not constrained
to be of the same type. .NET provides two types of variadic arguments support:

#### C# params-style

This is the preferred and idiomatic method but can only be used when we know the
variadic arguments will all be of the same type. Since we have no way to derive
this fact from the information in C/C++ function signatures, you will need set
this explicitly.

#### Argslist

This is a lesser known method for variadic arguments in .NET and was added by
Microsoft for better C++ compatibility in the runtime. As you can guess, this
does support different types per variable argument but is more verbose and less
idiomatic to use. By default we use this to wrap variadic functions.
  
### Default arguments

A subset of default arguments values are supported.

This is implemented by the [`HandleDefaultParamValuesPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/HandleDefaultParamValuesPass.cs) pass.

## Classes / Structs

In C++, both classes and structs are identical and can be used in both heap
(`malloc` / `new`) and automatic (stack) allocations.

This is unlike .NET, in which there is an explicit differentiation of the
allocation semantics of the type in the form of classes (reference types)
and structs (value types), 

By default, classes and structs are wrapped as .NET reference types. You can
provide an explicit mapping to wrap any type as a value type.

### POD (Plain Old Data) types

TODO: If the native type is a POD type, that means we can safely convert it to
a value type. This would make the generator do the right thing by default and is
pretty easy to implement.

### Static classes

Classes that respect the following constraints are bound as static managed classes.

- Do not provide any non-private constructors
- Do not provide non-static fields or methods
- Do not provide any static function that return a pointer to the class

This is implemented by the [`CheckStaticClass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/CheckStaticClass.cs) pass.

### Constructors

Constructors are mapped to .NET class constructors.

Note: An extra constructor is generated that takes a native pointer to the class.
This allows construction of managed instances from native instances.

Additionally we will create C# conversion operators out of compatible single-argument
constructors.

This is implemented by the [`ConstructorToConversionOperatorPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/ConstructorToConversionOperatorPass.cs) pass.

### Destructors

Destructors are mapped to the Dispose() pattern of .NET.

### Overloaded Operators

Most of the regular C++ operators can be mapped to .NET operator overloads.

In case an operator has no match in C# then its added as a named method with
the same parameters.

This is implemented by the [`CheckOperatorsOverloads`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/CheckOperatorsOverloads.cs) pass.

### Inheritance

C++ supports implementation inheritance of multiple types. This is incompatible
with .NET which supports only single implementation inheritance (but multiple
interface inheritance).

#### Single inheritance

This is the simplest case and we can map the inheritance directly.

#### Multiple inheritance

In this case we can only map one class directly. The others can be mapped as 
interfaces if they only provide pure virtual methods. Otherwise the best we can
do is provide some conversion operators in .NET to get access to them.

### Virtual methods

Overriding virtual methods from managed classes is supported.

Instances of these types can be passed to native code and and whenever the
native code calls one of those functions there will be a transition to the
C# code.

This is done by mirroring the virtual methods table with our own table at runtime,
and replacing the table entries with unmanaged function pointers that transition to
managed code as needed.

### Fields

Class instance fields are translated to managed properties.

This is implemented by the [`FieldToPropertyPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/FieldToPropertyPass.cs) pass.

## Templates

Template parsing is supported and you can type map them to other types.

Code generation for templates is experimental and can be enabled by the `GenerateClassTemplates` option.

## Preprocessor defines

Since C preprocessor definitions can be used for very different purposes, we can
only do so much when wrapping them to managed code.

### Numeric defines
 
These can be translated to proper .NET enumerations.
 
### String defines
 
These can be translated to .NET static constant definitions.
 
### Generalized expressions
 
This case is not supported and probably never will.

### Function-like macros
 
This case is not supported and probably never will.

### Helper defines

We support a set of helper defines that can be used to annotate the native code with:

- `CS_IGNORE_FILE` (translation units)

   Used to ignore whole translation units.

- `CS_IGNORE` (declarations)

   Used to ignore declarations from being processed.

- `CS_IGNORE_GEN` (declarations)

   Used to ignore declaration from being generated.

- `CS_IGNORE_FILE` (translation units)

   Used to ignore all declarations of one header.

- `CS_VALUE_TYPE` (classes and structs)

   Used to flag that a class or struct is a value type.

- `CS_IN_OUT` / `CS_OUT` (parameters)

   Used in function parameters to specify their usage kind.

- `CS_FLAGS` (enums)

   Used to specify that enumerations represent bitwise flags.

- `CS_READONLY` (fields and properties)

   Used in fields and properties to specify read-only semantics.

- `CS_EQUALS` / `CS_HASHCODE` (methods)

   Used to flag method as representing the .NET Equals or
   Hashcode methods.

- `CS_CONSTRAINT(TYPE [, TYPE]*)` (templates)

   Used to define constraint of generated generic type or generic method.

- `CS_INTERNAL` (methods)

   Used to flag a method as internal to an assembly. So, it is
   not accessible outside that assembly.

These are implemented by the [`CheckMacrosPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/CheckMacrosPass.cs) pass.

## Comments

There is full support for parsing of Doxygen-style C++ comments syntax.

  They are translated to .NET XML-style comments.

We can also figure out the intended semantic usage (`ref` or `out`) for parameters from Doxyxen tags.

Related passes:

- [`CleanCommentsPass`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/CleanCommentsPass.cs) pass
- [`FixParameterUsageFromComments`](https://github.com/mono/CppSharp/blob/master/src/Generator/Passes/FixParameterUsageFromComments.cs) pass

## Limitations

Support for these features is limited: 

- Exceptions
- RTTI

They are supported and taken into account by the C++ parser for bindings generation,
but there is currently no way to catch C++ exceptions from C#.

There is also no way to check RTTI type information for a specific type from C#,
but not an issue in practice since C# itself provides this via `GetType()`.

## Standard library support

  The generator provides some built-in type maps for the most common C/C++
standard library types:

### Strings

 - `std::string`
 - `std::wstring` (C++/CLI only (UTF-16), [pending PR](https://github.com/mono/CppSharp/pull/983) for C#)
  
 These are mapped automatically to .NET strings.

### Containers

- `std::vector`
- `std::map`
- `std::set`

Support for wrapping these is experimental and only currently works on the
CLI backend.

# 4. Customization

The generator provides various ways to customize the generation process.

## Type Maps

If all you need to do is customize what gets generated for a type, then you can
use the type maps feature. This lets you hook into the process for a specific
type pattern.

## Passes

If you need more control then you can write your own pass. Passes have full 
access to the parsed AST (Abstract Syntax Tree) so you can modify the entire 
structure and declaration data of the source code. This is very powerful and
should allow you to pretty much do anything you want.

The generator already provides many ready-to-use passes that can transform
the wrapped code to be more idiomatic:

### Renaming passes

Use these to rename your declarations automatically so they follow .NET 
conventions. When setting up renaming passes, you can declare what kind of
declarations they apply to. There are two different kinds of rename passes:

#### Case renaming pass

This is a very simple to use pass that changes the case of the name of the
declarations it matches.

#### Regex renaming pass

This pass allows you to do powerful regex-based pattern matching renaming of
declaration names.

### Function to instance method

This pass introduces instance methods that call a C/C++ global function. This can
be useful to map "object-oriented" design in C to .NET classes. If your function
takes an instance to a class type as the first argument, then you can use this
pass.

### Function to static method pass

This pass introduces static methods that call a C/C++ global function. This can
be useful to gather related global functions inside the object it belongs to
semantically.

### Getter/setter to property pass

This pass introduces a property that calls the native C/C++ getter and setter
function. This can make the API much more idiomatic and easier to use under .NET
languages.

### Internal passes

Some internal functionalities are also implemented as passes like checking for
invalid declaration names or resolving incomplete declarations. Please check the
developer manual for more information about these.

# 5. Advanced use cases

## Windows

If you're exposing C++ functions on Windows, you'll have to add the `__declspec(dllexport)` directive, otherwise the symbols won't be found when calling them from the managed world. You could also add the directive to a class directly, like this:

```c++
class __declspec(dllexport) ExposedClass
{
  // class definition
}
```
