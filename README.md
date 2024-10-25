<img src="docs/logo.svg" width="128">

CppSharp is a tool and set of libraries which facilitates the usage of native C/C++ code
with the .NET ecosystem.

It consumes C/C++ header and library files and generates the 
necessary glue code to surface the native API as a managed API. Such an API can be
used to consume an existing native library in your managed code or add
managed scripting support to a native codebase.

The supported target languages at present are C# and C++/CLI.

It can also be used as a library to parse native code into a syntax tree with a
rich declaration and type information model.

## Releases/Build Status

| NuGet Packages            | Continuous Integration    |
|---------------------------|---------------------------|
| [![NuGet](https://img.shields.io/nuget/v/CppSharp.svg)](https://www.nuget.org/packages/CppSharp/) | [![GitHub-actions](https://github.com/mono/CppSharp/workflows/CI/badge.svg)](https://github.com/mono/CppSharp/actions?query=workflow%3ACI) 

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

Feel free to open up issues on GitHub for any problems you find.

## Support

If you need commercial support feel free to open a discussion or issue for discussion.

## Users

CppSharp is used by the following projects:

[Kythera AI](https://kythera.ai)

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

[DearImguiSharp](https://github.com/Sewer56/DearImguiSharp)

Please feel free to send us a pull request adding your own projects to the list above.
