CppSharp is a collection of libraries for working with C++ code from .NET.

### Generator

Generates .NET bindings that wrap C/C++ code allowing interoperability with
managed languages. This is useful if you want to consume an existing native
library in your managed code or add scripting support to a native codebase.
 
 * Multiple backends: C++/CLI and C# P/Invoke
 * Multiple ABIs: Itanium (GCC, Clang), MS and MinGW
 * Virtual table overriding support
 * Multiple inheritance support
 * Easily extensible semantics via user passes 
 * Work-in-progress support for STL
 * Strongly-typed customization APIs and type maps

### AST 

Mirrors the Clang's C++ AST and type system classes in C# APIs.

Check out [_Clang's AST introduction docs_](http://clang.llvm.org/docs/IntroductionToTheClangAST.html) if you're not familiar with the architecture. 
 
 * C++ declarations
 * C++ types
 * Class object layout
 * Declaration visitors
 * Type visitors

### Parser

Provides APIs for parsing C++ source code.

* Parsing of C++ source code
* Parsing of libraries archives symbols
* Parsing of shared libraries symbols 
* Based on the very accurate Clang C++ parser.
 
## Community

Mailing list: [Google group](https://groups.google.com/forum/#!forum/cppsharp-list)

Build bot (CI): [builds.tritao.eu](builds.tritao.eu)

## Documentation

The documentation is still a work-in-progress, please see the following resources
for more information:

[Getting Started](docs/GettingStarted.md)

[User's Manual](docs/UsersManual.md)

[Developer's Manual](docs/DevManual.md)

## Releases

VS2012 32-bit:

- [CppSharp_VS2012_423_artifacts.zip](https://dl.dropboxusercontent.com/u/194502/CppSharp/CppSharp_VS2012_423_artifacts.zip) (_November 13th 2013_)

- [CppSharp_VS2012_329_artifacts.zip](https://dl.dropboxusercontent.com/u/194502/CppSharp/CppSharp_VS2012_329_artifacts.zip) (_November 5th 2013_) 

- [CppSharp_VS2012_306_artifacts.zip](https://dl.dropboxusercontent.com/u/194502/CppSharp/CppSharp_VS2012_306_artifacts.zip) (_October 25th 2013_) 

- [CppSharp_VS2012_183_artifacts.zip](https://dl.dropboxusercontent.com/u/194502/CppSharp/CppSharp_VS2012_183_artifacts.zip) (_September 17th 2013_)

## News

* November 11th 2013 - Released a new version (423) with bug fixes for type maps (when used with template specializations), constructors renaming and better documentation generation.
* November 5th 2013 - Released a new version (329) with a lot of bug fixes for operators and vtables.
* September 22nd 2013 - Conversion (type cast) operators wrapped (thanks to <a href="https://github.com/ddobrev">@ddobrev</a>)
* September 21st 2013 - Multiple inheritance now supported (thanks to <a href="https://github.com/ddobrev">@ddobrev</a>)

* September 11th 2013 - Added wrapping of inlined functions (thanks to <a href="https://github.com/ddobrev">@ddobrev</a>)
* September 11th 2013 - New binaries available for Windows (VS2012)
