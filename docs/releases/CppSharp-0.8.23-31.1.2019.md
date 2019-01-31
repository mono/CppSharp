# CppSharp 0.8.23 31.1.2019

* Keep Clang libTooling libs when packaging LLVM.

* Improve error handling in build scripts `UseClang()`.

* Added .NET Core build folder to Git Ignore .

* Initial integration of Clang AST viewer GUI tool.

* Made an exception serializable and removed another.

* Fixed the warnings in the test C++ for C# only.

* Fixed a crash when parsing libraries on macOS.

* Fixed error handling when parsing non-existent libraries.

* Added support for building with Clang and LLD.

* Switched to use csc.exe Roslyn compiler under Mono

* Disable most of the support for explicit pre-C++11 ABI since we do not need it anymore

* Fixed warnings in native test code.

* Fixed the generation of dependent virtual methods.

* Fixed overloading of operators with parameters mapped to the same type.

* Extended the type maps for primitive strings to C++/CLI.

* Handled int and long in maps to help resolve ambiguity.

* Simplified type maps by unlinking them from declarations.

* Properly hashed types to optimize their storage in maps.

* Fixed right-value references creating ambiguous overloads.

* Fixed the generated code in a case of ambiguous overloads.

* Added type maps for primitive strings (pointers to char).

* Added an option for skipping private declarations.

* Tested indirect calls from native code of overrides in the target language.

* Initial support for building under .NET Core.

* Updated the CI on Linux to use GCC 7.

* Exported all additional symbols on macOS.

* Fixed error handling and message when parsing non-existent files.

* Added a test for passing an std::string by value.

* Fixed the marshalling of std::string with GCC 6+ on Linux.

* Added a type map for char.

* Make Windows10SDK detection more robust

* Cached found type maps for faster look-ups.

* Deleted unused and slow code.

* Fixed the parsing of functions with integral template args.

* Decreased the build time on the Linux CI.

* Fixed a crash when parsing type aliases.

* Fixed the build of the parser when its path has spaces.

* Changed type maps to only return types - no strings.

* Simplified type maps by using static objects to disable as needed.

* Optimized the walking of the managed AST.

* Optimized the generation of C# by not splitting any strings.

* Optimized the walking of the AST by simplifying its search.

* Fixed the late parsing of templates.

* Fixed LLVM/Clang tar.gz archive extraction using 7-Zip on Windows.

* Fixed Windows SDK version detection in build scripts.

* Resolved ambiguity between char-like types in the generated C#.

* Fixed the generated C# for templates with > 1 ctor taking a pointer to a class.

* Fixed the generated C# for pure virtual functions with default arguments.

* Fixed the generated C# for default arguments of type pointer to a function.

* Fixed the generated C# for a certain case of two default parameters.

* Fixed the generated C# for arguments with default values of "nullptr".

* Fixed the generated C# for setters with default parameters.

* Fixed the generated C# for public fields with types mapped to primitive.

* Fixed the generated C# for constant references to primitives.

* Upgraded the CI script to use Ubuntu 16.04.

* Fixed ambiguity when the type of a parameter is mapped to a type in an overload.