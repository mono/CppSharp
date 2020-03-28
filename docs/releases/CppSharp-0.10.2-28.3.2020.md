# CppSharp 0.10.2 28.3.2019

* Associate getter/setter methods with their associated property in GetterSetterToProperty pass

* Added optional getter/setter pair creation for fields in FieldToProperty pass

* Refactor CLI handling of enums to be based on generic C generator

* Default to .h file extension in C code generator

* Add helper methods to ignore and generate specific translation units

* Guard a few more language-specific passes in the driver

* Fix generation of multiple interfaces in C# for some edge cases

* Fix templates to be abstract if any specialization is abstract

* Fix TranslationUnit.FileRelativePath for invalid units

* Re-use common C++ declaration type printing code in CLITypePrinter

* Allow changing the default TypePrinterContextKind when creating a type printer

* Remove needless pointer offsets from generated C#

* Fix a crash for secondary bases with secondary bases

* Fix bug related to processing of potential property methods

* Ensure generated symbols can use placement new

* Fix Mono not being found on the PATH on recent macOS versions

* Instantiate exception specifications before reading

* Update LLVM to the latest version

* Fix a syntax error in CMake listing Clang modules

* Enable building lld as part of LLVM

* Restore modules needed for compiling with Clang

* Support Visual Studio 2019 for building Clang

* Update Travis to Ubuntu Xenial 18.04

* Simplify and optimize the printing of pointers in C++

* Fix printing of function pointers in C++

* Don't export symbols for explicit specializations

* Avoid invalid template specializations in tests

* Update the printing of comments to the new Clang

* Work around MSVC 32 crashing reading of exported symbols in Mach-O

* Improve Xcode toolchain lookup to be more robust

* Implement logic for -fgnuc-version= argument required by LLVM

* Update LLVM to the latest version

* Refactor Clang builtins directory logic and move it to the managed side

* Escape C# strings correctly

* Improve CS_INTERNAL so it now applies to all declarations

* Print the parser target triple in verbose mode

* Always re-create the Clang resource directory when building

* Cleanup Clang resource directory lookup logic

* Remove old workaround for testing issue that does not seem necessary anymore

* Git ignore .vscode directory

* Workaround Premake issue when copying the resource include directories at build time

* Fix warning about #pragma once in source file

* Update bootstrap tool to run against latest LLVM

* Update bootstrap tool to find LLVM build directory

* Add options to disable tests and examples from the build

* Improve the ignoring of dependent name types

* Implement UnresolvedUsingType and UnresolvedUsingTypename

* Fix the tests for exception types

* Switch to Xcode 11 in Travis CI

* Extend printing and reading of exception types

* Fix the check to ignore dependent name types

* Ignore unused destructors when generating symbols

* Fix the printing of "noexcept" in C++

* Make destructors virtual in abstract classes for tests

* Avoid generating abstract implementations for template classes

* Fix template type checking in CovariantTypeComparer

* Git ignore nested temporary obj folders

* Workaround System.TypeLoad exception when running test-suite on macOS

* Fix enum with zeros for hex literals

* Fix the moving of free functions to classes to match by module too

* Generate valid C# when an external module has an unsupported operator

* Fix a possible overflown stack when ignoring

* Force compilation of all functions of specializations

* Fill in missed values when cloning functions

* Optimize the moving of functions to classes

* Delete a custom pass added as standard

* Fix the C++ printing of function pointers in parameters

* Eliminate generated symbols for ignored functions

* Fix printing of type defs in C++

* Remove the internal C# functions for virtual destructors

* Give unique names to exported inlined functions

* Generate symbols for methods of specializations

* Optimize all passes which visited useless items

* Make the pass for properties more extendable

* Simplify overrides of overrides of secondary bases

* Optimize calls to base getters in properties

* Fix comparison of char and const char* in overloading

* Optimize the pass for properties

* Clarify limitations around exceptions and RTTI

* Destroy returned by value std::strings

* Upgrade ANSI marshalling to UTF-8 marshalling

* Generate valid C# when a renamed override causes conflicts

* Ensure protected nested types are accessible with multiple inheritance

* Fix the regressed indentation of printed comments

* Generate projects for .NET 4.7 to use new features

* Simplify the generated C# for marshalling strings