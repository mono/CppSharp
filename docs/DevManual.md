CppSharp Developers Manual

# Architecture

## Driver

The driver is responsible for setting up the needed context for the rest of
the tool and for implementing the main logic for parsing the user-provided headers,
processing the declarations and then calling the language-specific generator to
generate the bindings.

## Parser

Since writing bindings by hand is tedious and error-prone, an automated
approach was preferred. The open-source Clang parser is used for the task,
providing an AST (Abstract Syntax Tree) of the code, ready to be consumed
by the generator.

This is done in Parser.cpp, we walk the AST provided by Clang and mirror
it in a .NET-friendly way. Most of this code is pretty straightforward if
you are familiar with how Clang represents C++ code in AST nodes.

Recommended Clang documentation: [Clang Internals manual](http://clang.llvm.org/docs/InternalsManual.html)

## Generator

After parsing is done, some language-specific binding code needs to be generated.

Different target languages provide different features, so each generator needs to
process the declarations in certain ways to provide a good mapping to the target
language.

Additionally some of it can be provided directly in the native source
code, by annotating the declarations with custom attributes.
 
## Runtime

This implements the C++ implementation-specific behaviours that allow
the target language to communicate with the native code. It will usually
use an existing FFI that provides support for interacting with C code.

It needs to know about the object layout, virtual tables, RTTI and
exception low level details, so it can interoperate with the C++ code.

# ABI Internals

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

The target runtime needs to support calling native methods and this is usually
implemented with an FFI (foreign function interface) in the target language VM
virtual machine). In .NET this is done via the P/Invoke system.

# Similar Tools

- SWIG
[http://www.swig.org/](http://www.swig.org/)

This is the grand-daddy of binding tools and supports a lot of different languages and environments.
It uses a unique approach of mixing the customization of the binding in interface files that it also
uses to specify the native declarations. This is possible because SWIG contains its own parser for
native code.

- Cxxi
[https://github.com/mono/cxxi/](https://github.com/mono/cxxi/)

This can be seen as the precedecessor to CppSharp and relied on a different binding approach,
generating IL code at runtime for each C++ ABI and using the GCC-XML tool for parsing.

In comparison, CppSharp generates different code at compile-time for each ABI and relies on Clang for parsing.

- Sharppy - .NET bindings generator for unmanaged C++
[https://code.google.com/p/sharppy/](https://code.google.com/p/sharppy/)

Lokos abandoned at this point.

- XInterop
[http://xinterop.com/](http://xinterop.com/)

Commercial tool.