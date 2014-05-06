CppSharp Developers Manual

1. Introduction
===============

How to compile
--------------

Check out the Getting started guide for build instructions.

2. Architecture
===============


## Driver

The driver is responsible for setting up the needed context for the rest of
the tool and for implementing the main logic for parsing the user-provided headers,
processing the declarations adn then calling the language-specific generator to
generate the bindings.


## Parser

Since writing bindings by hand is tedious and error-prone, an automated
approach was preferred. The open-source Clang parser is used for the task,
providing an AST (Abstract Syntax Tree) of the code, ready to be consumed
by the generator.

This is done in Parser.cpp, we walk the AST provided by Clang and mirror
it in a .NET-friendly way. Most of this code is pretty straightforward if
you are familiar how Clang represents C++ code in AST nodes.

Recommended Clang documentation: [http://clang.llvm.org/docs/InternalsManual.html](http://clang.llvm.org/docs/InternalsManual.html "Clang Internals")


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

3. ABI Internals
===============

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


4. Similar Tools
=================

* Sharppy - .NET bindings generator for unmanaged C++
[https://code.google.com/p/sharppy/](https://code.google.com/p/sharppy/)

* XInterop
[http://xinterop.com/](http://xinterop.com/)

* SWIG
[http://www.swig.org/](http://www.swig.org/)

* Cxxi
[https://github.com/mono/cxxi/](https://github.com/mono/cxxi/)

