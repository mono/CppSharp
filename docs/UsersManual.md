CppSharp Users Manual

# 1. Introduction

## What does it do?

This tool allows you to generate .NET bindings that wrap C/C++ code allowing interoperability with managed languages. This can be useful if you have an existing native codebase and want to add scripting support, or want to consume an existing native library in your managed code.

## Why reinvent the wheel?

There are not many automated binding tools around, the only real alternative is SWIG. So how is it different from SWIG?

 * No need to generate a C layer to interop with C++.
 * Based on an actual C++ parser (Clang) so very accurate.
 * Understands C++ at the ABI (application binary interface) level
 * Easily extensible semantics via user passes
 * Strongly-typed customization APIs
 * Can be used as a library

# 2. C/C++ language features

In this section we will go through how the generator deals with each C / C++ feature.

## C/C++ Types

### Fundamental types

These are mapped to .NET types as follows:

1. **Integral types**

  char      **→** System::Byte
  bool      **→** System::Boolean
  short     **→** System::Int16
  int, long **→** System::Int32
  long long **→** System::Int64
  
  Note: Signedness is also preserved in the conversions.

2. **Floating-point types**

  float     **→** System::Single
  double    **→** System::Double

3. **Other types**

  wchar_t   **→** System::Char
  void      **→** System::Void

### Derived types

1. **Arrays**

 These are mapped to .NET CLR arrays.

2. **Function Pointers / Pointers to Members**

 These are mapped to .NET CLR delegates.

3. **Pointers**

 These are mapped to .NET CLR references unless:

  void*        **→** System::IntPtr
  const char*  **→** System::String

4. **References**

 References are mapped to .NET CLR references just like pointers.

## Typedefs

We do not preserve type definitions since .NET and its main language C# do not have the concept of type aliases like C/C++. There is an exception in the case of a typedef'd function (pointer) declaration. In this case generate a .NET delegate with the name of the typedef.

## Enums

C/C++ enums are translated automatically to .NET enumerations.

Special cases to be aware of:

1. Anonymous enums

 C and C++ enums (this does not apply to the new C++11 strongly typed enums) do not introduce their own scope. This means the enumerated values will leak into an outer context, like a class or a namespace. When this is detected, the generator tries to map to an outer enclosing context and generate a new name.

2. Flags / Bitfields
 
 Some enumerations represent bitfield patterns. The generator tries to check for this with some heuristics. If there are enough values in the enum to make a good guess, we apply the [Flags] .NET attribute to the wrapper enum.

## Functions

Since global scope functions are not supported in C# (though they are available in the CLR) they are mapped as a static function in a class, to be consumable by any CLS-compliant language.

By default all globals functions of a translation unit are mapped to a static class with the name of of the unit prefixed by the namespace.

Special cases to be aware of:

1. Variadic arguments (TODO)

  C/C++ variadic arguments need careful handling because they are not contrained to be of the same type.

  .NET provides two types of variadic arguments support:

  * C# params-style

  This is the preferred and idiomatic method but can only be used when we know the variadic arguments will all be of the same type. Since we have no way to derive this fact from the information in C/C++ function signatures, you will need set this explicitly.

  * Argslist

  This is a lesser known method for variadic arguments in .NET and was added by Microsoft for better C++ compatibility in the runtime. As you can guess, this does support different types per variable argument but is more verbose and less idiomatic to use. By default we use this to wrap variadic functions.
  
2. Default arguments

  We do not try to wrap arguments default values yet. This is desired but needs more research since potentially all C++ constant expressions can be used as default arguments, though it would be pretty simple to add this for the common case of null constants.

## Classes / Structs

Unlike .NET, in which there is an explicit differentiation of the allocation semantics of the type in the form of classes (reference types) and structs (value types), in C++ both classes and structs are identical and can be used in both heap (malloc/new) and automatic (stack) allocations.

By default, classes and structs are wrapped as .NET reference types. If your type is supposed to be a value type, then you can instruct the generator to issue a .NET value type. You should use value types if the types are cheap to construct and/or if you creating a lot of instances.

### POD (Plain Old Data)

TODO: If the native type is a POD type, that means we can safely convert it to a value type. This would make the generator do the right thing by default and is pretty easy to implement.

### Constructors

Constructors are mapped to .NET class constructors.

Note: An extra constructor is generated that takes a native pointer to the class. This allows construction of managed instances from native instances.

### Destructors

TODO: Destructors need to be mapped to the Dispose() pattern of .NET.

### Overloaded Operators

Most of the regular C++ operators can be mapped to .NET operator overloads.

TODO: In case we get unsupported C++ operators then we should emit a warning, and introduce a new automatically named method to represent the operator. The user should then explicitly give a name to the operator to get rid of the warning.

### Conversion Operators

TODO: Convert C++ conversion operators to .NET conversion operators.

### Inheritance

C++ supports implementation inheritance of multiple types. This is incompatible with .NET which supports only single implementation inheritance (but multiple interface inheritance).

1. Single inheritance

 This is the simplest case and we can map the inheritance directly.

2. Multiple inheritance

 In this case we can only map one class directly. The others can be mapped as interfaces if they only provide pure virtual methods. Otherwise the best we can do is provide some conversion operators in .NET to get access to them.

3. Virtual inheritance

 This is not supported for now.

## Bitfields

This feature is not supported yet.

## Unions
 
This feature is not supported yet.

## Templates

Template parsing is supported and you can type map them to other types.

TODO: Also export explicit template type specializations.

## Preprocessor defines

Since C preprocessor definitions can be used for very different purposes, we can only do so much when converting them to managed code.

 1. Numeric defines
 
 These can be translated to proper .NET enumerations.
 
 2. String defines
 
 These can be translated to .NET static constant definitions.
 
 3. Generalized expressions
 
 This case is not supported and probably never will.

 4. Function-like macros
 
 This case is not supported and probably never will.

## Comments

Doxygen-style C++ comments are translated to .NET XML-style comments. This feature is experimental and limited to what Doxygen directives the upstream Clang parser supports.

### Exceptions

This feature is not supported yet.

## RTTI

This feature is not supported yet.

# 3. Customization

The generator provides various ways to customize the generation process.

## Type Maps

If all you need to do is customize what gets generated for a type, then you can use the type maps feature. This lets you hook into the process for a specific type pattern.

## Standard library support

The generator provides type maps for the most common C/C++ standard library types:

* String

 The native C++ string type, std::string, is mapped automatically to .NET strings.

* Containers

1. Vector

 TODO: This is not supported yet.

2. Map

 TODO: This is not supported yet.

3. Set

 TODO: This is not supported yet.

## Passes

If you need more control then you can write your own pass. Passes have full access to the parsed AST (Abstract Syntax Tree) so you can modify the entire structure and declaration data of the source code. This is very powerful and should allow you to pretty much do anything you want.

The generator already provides many ready-to-use passes that can transform
the wrapped code to be more idiomatic:

### Renaming passes

Use these to rename your declarations automatically so they follow .NET conventions. When setting up renaming passes, you can declare what kind of declarations they apply to. There are two different kinds of rename passes:

1. Case renaming pass

This is a very simple to use pass that changes the case of the name of the declarations it matches.

2. Regex renaming pass

This pass allows you to do powerful regex-based pattern matching renaming of declaration names.

### Function to instance method

This pass introduces instance methods that call a C/C++ global function. This can be useful to map "object-oriented" design in C to .NET classes. If your function
takes an instance to a class type as the first argument, then you can use this
pass.

### Function to static method pass

This pass introduces static methods that call a C/C++ global function. This can be useful to gather related global functions inside the object it belongs to semantically.

### Getter/setter to property pass

This pass introduces a property that calls the native C/C++ getter and setter function. This can make the API much more idiomatic and easier to use under .NET languages.

### Internal passes

Some internal functionalities are also implemented as passes like checking for invalid declaration names or resolving incomplete declarations. Please check the developer manual for more information about these.

# 4. Targets

The backend of the generator is abstracted and it can target different .NET binding technologies:

## C++/CLI

 This is the most developed target at the moment. It generates C++/CLI source code that should be compiled with a C++/CLI compiler and linked with the original library. Since all the hard logic is in the compiler this generator is relatively simple and easy to debug.

## C# (P/Invoke)

 This was the original backend and generates C# source code that calls back into native by using P/Invoke interop technology. It has bitrotted a bit lately but maybe it should be brought back, at least to the level of binding C and simple C++ libraries. It is much simpler to debug C# managed code than it is to debug the C++/CLI compiler. Even if C++/CLI ends up being the superior technology for C++ interop, this would be very useful for binding C libraries to .NET.
