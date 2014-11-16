CppSharp is a collection of libraries for working with C++ code from .NET.

### Generator

Generates .NET bindings that wrap C/C++ code allowing interoperability with
managed languages. This is useful if you want to consume an existing native
library in your managed code or add scripting support to a native codebase.
 
 * Multiple backends: C++/CLI and C# P/Invoke
 * Multiple ABIs: Itanium, MS, ARM, iOS and iOS64
 * Multiple platforms: Windows, OS X and Linux
 * Virtual table overriding support
 * Multiple inheritance support
 * Easily extensible semantics via user passes 
 * Work-in-progress support for STL (C++/CLI only)
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

## Building custom wrappers and consulting

Please contact @ddobrev.