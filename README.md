CppSharp is a tool and set of libraries which allows programmers to use
C/C++ libraries with high-level programming languages (such as C#).

It is a tool that takes C/C++ header and library files and generates the 
necessary glue to surface the native API as a managed API. Such an API can be
used to consume an existing native library in your high-level code or add
scripting support to a native codebase.

The supported target languages at present are C# and C++/CLI.

It can also be used as a library to parse native code into a syntax tree with a
rich declaration and type information model.

## Releases/Build Status

|Windows 64-bit|Windows 32-bit| windows-vs-x86            | linux-gcc-x86_64            | osx-clang-x86               |
|---------------------------|---------------------------|---------------------------|-----------------------------|-----------------------------|
| [![NuGet](https://img.shields.io/nuget/v/CppSharp.svg)](https://www.nuget.org/packages/CppSharp/) | [![GitHub release](https://img.shields.io/github/release/mono/CppSharp.svg)](https://github.com/mono/CppSharp/releases) | [![windows-vs-x86](https://ci.appveyor.com/api/projects/status/5o9gxjcttuaup671/branch/master?svg=true)](https://ci.appveyor.com/project/tritao/CppSharp/branch/master) | [![linux-gcc-x86_64](https://travis-ci.org/mono/CppSharp.svg?branch=master)](https://travis-ci.org/mono/CppSharp) | [![osx-clang-x86](https://travis-ci.org/mono/CppSharp.svg?branch=master)](https://travis-ci.org/mono/CppSharp)

1. [Libraries](#libraries)
2. [Documentation](#documentation)
3. [Community](#community)
4. [Support](#support)
5. [Users](#users)

## Libraries

### AST 

Mirrors the Clang's C/C++ AST and type system classes in C# APIs.

Check out [_Clang's AST introduction docs_](http://clang.llvm.org/docs/IntroductionToTheClangAST.html) for more details about its architecture.
 
 * C++ declarations
 * C++ statements / expressions
 * C++ types
 * Class object layout
 * Declaration visitors
 * Type visitors

### Parser

Provides APIs for parsing of C/C++ source code into a syntax tree.

* Parsing of C/C++ source code
* Parsing of libraries archives symbols
* Parsing of shared libraries symbols 
* Based on the very accurate Clang C++ parser.

### Generator

Generates the glue binding code from a syntax tree of the native code.
 
 * Multiple backends: C++/CLI and C# (P/Invoke)
 * Multiple ABIs: Itanium, MS, ARM, iOS
 * Multiple platforms: Windows, OS X and Linux
 * Multiple runtimes: .NET and Mono
 * C++ virtual methods overriding from managed code
 * C++ multiple inheritance by translating to C# interfaces
 * C++ std::string
 * C++ default parameter values
 * C/C++ semantic comments (Doxygen) to C# comments
 * Extensible bindings semantics via user passes and type mapping 

## Documentation

Please see the following resources for more information:

[Getting Started](docs/GettingStarted.md)

[User's Manual](docs/UsersManual.md)

[Developer's Manual](docs/DevManual.md)

## Community

Feel free to open up issues on Github for any problems you find.

You can also join us at our [#managed-interop](https://gitter.im/managed-interop) Gitter discussion channel.

## Support

For building wrappers and priority support please write to &#99;&#112;&#112;&#115;&#104;&#97;&#114;&#112;&#64;&#112;&#114;&#111;&#116;&#111;&#110;&#109;&#97;&#105;&#108;&#46;&#99;&#111;&#109;.
Alternatively, you may post bounties at https://www.bountysource.com/.

## Users

CppSharp is used by the following projects:

[QtSharp](https://gitlab.com/ddobrev/QtSharp)

[MonoGame](https://github.com/mono/MonoGame)

[LLDBSharp](https://github.com/tritao/LLDBSharp)

[Xamarin](http://xamarin.com/)

[FFMPEG.net](https://github.com/crazyender/FFMPEG.net)

[FFmpeg bindings](https://github.com/InitialForce/FFmpeg_bindings)

[Tizen bindings](https://github.com/kitsilanosoftware/CppSharpTizen)

[libgd bindings](https://github.com/imazen/deprecated-gd-bindings-generator-old)

[ChakraSharp](https://github.com/baristalabs/ChakraSharp)

[FFmpeg.AutoGen](https://github.com/Ruslan-B/FFmpeg.AutoGen)

[GLFW3.NET](https://github.com/realvictorprm/GLFW3.NET)

Please feel free to send us a pull request adding your own projects to the list above.
