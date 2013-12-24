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
 * Easily extensible semantics via user passes
 * Strongly-typed customization APIs
 * Can be used as a library
 * Based on an actual C++ parser (Clang) so very accurate.
 * Understands C++ at the ABI (application binary interface) level
 * Supports virtual method overriding

# 2. Targets

The backend of the generator is abstracted and it can target different .NET
binding technologies:

- C# (P/Invoke)
- C++/CLI

# 3. C/C++ language features

In this section we will go through how the generator deals C/C++ types.

## Fundamental types

### Integral types

  - char      **→** System::Byte
  - bool      **→** System::Boolean
  - short     **→** System::Int16
  - int, long **→** System::Int32
  - long long **→** System::Int64
  
  Note: Signedness is also preserved in the conversions.

### Floating-point types

  - float     **→** System::Single
  - double    **→** System::Double

### Other types

  - wchar_t   **→** System::Char
  - void      **→** System::Void

## Derived types

### Arrays

 These are mapped to .NET CLR arrays.

### Pointers

#### Function Pointers / Pointers to Members

These are mapped to .NET CLR delegates.

#### Primitive types

 These are mapped to .NET CLR references unless:

  - void*        **→** System::IntPtr
  - const char*  **→** System::String

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
guess, we apply the [Flags] .NET attribute to the wrapper enum.

## Functions

Since global scope functions are not supported in C# (though they are available
in the CLR) they are mapped as a static function in a class, to be consumable by
any CLS-compliant language.

By default all globals functions of a translation unit are mapped to a static
class with the name of of the unit prefixed by the namespace.

Special cases to be aware of:

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

Default arguments values are not supported yet since potentially all C++ constant
expressions can be used as default arguments.

## Classes / Structs

In C++, both classes and structs are identical and can be used in both heap
(malloc/new) and automatic (stack) allocations.

This is unlike .NET, in which there is an explicit differentiation of the
allocation semantics of the type in the form of classes (reference types)
and structs (value types), 

By default, classes and structs are wrapped as .NET reference types. You can
provide an explicit mapping to wrap any type as a value type.

### POD (Plain Old Data) types

TODO: If the native type is a POD type, that means we can safely convert it to
a value type. This would make the generator do the right thing by default and is
pretty easy to implement.

### Constructors

Constructors are mapped to .NET class constructors.

Note: An extra constructor is generated that takes a native pointer to the class.
This allows construction of managed instances from native instances.

### Destructors

Destructors are mapped to the Dispose() pattern of .NET.

### Overloaded Operators

Most of the regular C++ operators can be mapped to .NET operator overloads.

In case an operator has no match in C# then its added as a named method with
the same parameters.

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

Support for overriding virtual methods is being worked on and it will provide
a way for managed code to override virtual methods in bound classes.

Instances of these types can be passed to native code and and whenever the
native code calls one of those functions there will be a transition to the
C# code. 

## Templates

Template parsing is supported and you can type map them to other types.

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

## Comments

There is full support for parsing of Doxygen-style C++ comments syntax.

They are translated to .NET XML-style comments.

## Limitations

Support for these features is limited or only partial: 

- Exceptions
- RTTI

## Standard library support

  The generator provides some built-in type maps for the most common C/C++
standard library types:

### Strings

 - std::string
 - std::wstring
  
 These are mapped automatically to .NET strings. 

### Containers

- std::vector
- std::map
- std::set

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