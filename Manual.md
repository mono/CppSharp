C++ C# interop bridge tool Developer's Manual


1. Overview
-----------

This tool allows you to generate bindinds that wrap C/C++ code allowing
interoperation in another language. This can be useful if you have an
existing native codebase and want to add scripting support, or want to
consume an existing native library in your managed code.

It differs from most existing C++ binding tools like SWIG in that it
does not generate a C layer to interop with C++, it actually understands
enough of the C++ ABI (application binary interface) to get the job done
without generating extra native code.


2. Architecture
---------------

The tool is architected in the following layers:

 1. Parser

Since writing bindings by hand is tedious and error-prone, an automated
approach is preferred. To do this, we use the open-source Clang parser
that provides us with an AST (Abstract Syntax Tree) of the code, ready
to be consumed by the generator.

By using a real compiler for parsing the code, we get mature support for
C and C++ features, like a fully-compliant preprocessor and support for
attributes and pragma directives that can affect the behaviour of the
code (ex. custom packing / alignment options).

 2. Generator (language-specific)

After parsing is done, a language-specific layer is generated.
Since different target languages provide different features, some C++
language features are mapped in different ways by different languages.

Since scripting languages have their own way of expressing certain
patterns, like properties (instead of getters/setters pairs), or
delegates (instead of function pointers) there is support to process
the generated bindings, to rename symbols, create additional helper
methods, map parameters.

Aditionally some of it can be provided directly in the native source
code, by annotating the declarations with custom attributes.
 
 3. Runtime (language-specific)

This implements the C++ implementation-specific behaviours that allow
the target language to communicate with the native code. It will usually
use an existing FFI that provides support for interacting with C code.

It needs to know about the object layout, virtual tables, RTTI and
exception low level details, so it can interoperate with the C++ code.


3. C/C++ features
-------------------

In this section we will go through each relevant C / C++ feature and
go over how it is usually implemented and discuss different binding
strategies.

 * Comments
 
 \brief Doxygen-style C++ comments are currently translated to .NET
 XML-style comments.
 
 * Defines
 
 Defines can be translated to proper enumerations in C#. This needs
 to be a manual operation for now. Other type of defines like strings
 are currently not supported.

 * Enumerations
 
C/C++ enums are translated to proper enumerations in C#.

Beware that in the case of anonymous enums inside classes or namespaces,
they need to hoisted to an outer enclosing namespace. In some cases manual
rules need to be written for a good mapping.

 Flags / Bitfields
 
Some enums represent bitfields. The generator will try to apply an heuristic
to native enums to check if they represent flags. Most of the time it will
get it right and correctly apply the [Flags] .NET attribute to the wrapper
enum. If it guesses wrong (not enough enum values to make a good guess)
then you will need to make a rule to correct it.

 * Types

  Built in
   (Wide) Chars (Unicode?)
   Signed / Unsigned Int (8, 16, 32, 64)
   Float / Doubles (32, 64)
  
  Modifiers
   Value
   Pointers
   [C++] References
  
 * Functions

Since global scope functions are not supported in C# (though they are
available in CIL) they must be mapped as a static function in a class,
to be consumed by C# code.

 * Bitfields

Not supported yet. Needs some research to check how to map to C#.

 * Unions
 
Not supported yet. Needs some research to check how to map to C#.

 * Type Definitions
 
Not supported yet. Needs some research to check how to map to C#.

 * Classes / Structs
 
Classes are wrapped in a native .NET class. Support for classes is currently
untested and still experimental.

	 POD
	 Constructors
	 Destructors
	 Overloaded Operators
	 Conversion Operators
	 Methods
	 Static Members
	 Pointers to members
 
	 Inheritance:
	 
	 1. No inheritance
	 2. Single inheritance
	 3. Multiple inheritance
	 4. Virtual inheritance

* Templates

Not supported yet. Needs some research to check how to map to C#.

4. Customization
------------------

The generator is extensible and some support for specific C++ features
will appear in the next versions:

	* STL
	* Smart Pointers

5. Target Languages
-------------------

At the moment the project is C#-specific.

6. ABI Internals
----------------

Each ABI specifies the internal implementation-specific details of how
C++ code works at the machine level, involving things like:

 1. Class Layout
 2. Symbol Naming
 3. Virtual tables
 4. Exceptions
 5. RTTI (Run-time Type Information)

There are two major C++ ABIs currently in use:

 1. Microsoft (VC++ / Clang)
 2. Itanium (GCC / Clang)
 
Each implementation differs in a lot of low level details, so we have to
implement specific code for each one.

The target runtime needs to support calling native methods.
This is usually implemented with an FFI (foreign function interface) in
the target language VM (virtual machine).

In most cases MSVC lays out classes in the following order:

1. Pointer to virtual functions table (_vtable_ or _vftable_), added only
when the class has virtual methods and no suitable table from a base class
can be reused.

2. Base classes

3. Class members


7. Similiar Tools and Inspiration
---------------------------------

https://github.com/mono/cxxi
http://code.google.com/p/bridj/
http://code.google.com/p/jnaerator/
http://dyncall.org/
