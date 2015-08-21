CppSharp is a collection of libraries for working with C++ code from .NET.

It has multiple use cases, from parsing C++ code to automatically generating
.NET bindings for wrapping C/C++ native code allowing interoperability with
managed languages like C#.

This can be used to consume an existing native library in your managed code
or add scripting support to a native codebase.

## Build Status

| windows-vs-x86            | linux-gcc-x86_64            | osx-clang-x86               |
|---------------------------|-----------------------------|-----------------------------|
| [![windows-vs-x86][1]][2] | [![linux-gcc-x86_64][3]][4] | [![osx-clang-x86][3]][4]

[1]: https://ci.appveyor.com/api/projects/status/5o9gxjcttuaup671/branch/master?svg=true
[2]: https://ci.appveyor.com/project/tritao/CppSharp/branch/master
[3]: https://travis-ci.org/mono/CppSharp.svg?branch=master
[4]: https://travis-ci.org/mono/CppSharp

## Libraries

### Generator
 
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
 
## Documentation

Please see the following resources for more information:

[Getting Started](docs/GettingStarted.md)

[User's Manual](docs/UsersManual.md)

[Developer's Manual](docs/DevManual.md)

## Community

Mailing list: [Google group](https://groups.google.com/forum/#!forum/cppsharp-list)

## News

2nd, June 2015: We're having three students contributing to the project this summer,
as part of Google Summer of Code.

* Alexandre Esteves (@afesteves)
* Dimitar Dobrev (@ddobrev)
* Abhinav Tripathi (@genuinelucifer)

## Support

For professional services related to building custom wrappers and consulting please contact @ddobrev (dpldobrev at yahoo dot com).
