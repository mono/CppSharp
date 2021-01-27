## [0.10.5] - 2020-6-27

- Don't add ABI-specific parameters when wrapping C

- Prioritize public non-field properties when resolving naming conflicts

- Fix patching of virtual tables for MSVC with RTTI

- Free the memory of the patched v-tables

- Fix parsing of member pointers with MSVC

- Generate valid C# for constructors taking const&

- Generate valid C# for returned function pointers

- Expose returned values of non-void setters

- Ensure enumerations lack conflicts when renamed

- Fix generation for fields of type const reference


## [0.10.4] - 2020-5-23

- Simplify the required headers for macOS bindings

- Option to allow caller to specify it does not want unchanged output files to be modified. This supports incremental build in VS. (#1373) - Ali Alamiri <ali.alamiri@sage.com>

- CreateInstance factory overload to pass flag informing wrapper if it should own the native pointer passed to it. - Ali Alamiri <ali.alamiri@sage.com>

- force bash use to avoid `./premake5-linux: 3: ./premake5-linux: Bad substitution` error if other default shell in the system - Lorenzo Delana <lorenzo.delana@gmail.com>

- Made the original virtual tables static too

- Pass native pointers to bases in the generated C#

- Check type maps when printing C++ for pointers

- Do not add type alias templates twice to the AST

- Fix all leaks of memory in the old expressions

- Add template functions to their context

- Fix leaking memory by removing a useless assignment

- Fix leaking the memory of an entire Clang AST

- Ignore type maps when printing C++ for symbols

- Implement more accurate managed type printing in C++ type printer.

- Use a native type printer context when looking for type maps in CSharpSourcesExtensions.DisableTypeMap.

- Use explicit type printer when printing types in C# GenerateMethodSpecifier.

- Refactor CodeGenerator.GenerateMethodSpecifier to allow explicit specifier kind.

- Do not check declaration access for explicitly generated declarations.

- Fix TranslationUnit.FileRelativeDirectory to be more robust against null include paths.

- Fix formatting to of Declaration.GenerationKind to ease debugging.

- Ignore implicitly deleted copy constructor methods.

- Correctly marshal constant arrays in C++/CLI (#1346)

- Marshal pointer to primitive typedefs in C++/CLI (#1355) - Ali Alamiri <ali.alamiri@sage.com>

- Fix a regression when renaming classes

- Fix naming conflicts with nested types and members

- publish the clang lib folder - Ali Alamiri <ali.alamiri@sage.com>

- Implement basic support for parsing function-like macros.

- Implement TranslationUnit.ToString() to help with debugging.

- Add debug option and flags to the Premake build and compile scripts.

- Generate valid C# for parameters typedef-ed to mapped types

- Update the version of Mono used for builds (CI)

- Fix the regressed C# marshalling of char*

- Handle pointer to pointer param (#1343) - Ali Alamiri <ali.alamiri@sage.com>

- Handle returned pointers to std::vector in C++/CLI - Ali Alamiri <ali.alamiri@sage.com>

- Implement abstract templates to call virtuals

- Correctly align printed information for debugging

- Set the render kind of inline command comments

- Fix all memory leaks in tests

- Generate by ref parameters of type a pointer to enum

- Use UnsupportedType description for type name instead of empty string (#1339) - Ali Alamiri <ali.alamiri@sage.com>


## [0.10.3] - 2020-4-9

- Ensure complete template specializations in AST

- Add an option and checking for generation of deprecated declarations

- Implement parsing and AST processing of C++ deprecated attributes

- Make sure we use a native type printer for symbols code generation

- Git ignore new include folders for cross generation

- Fix marshaling for wchar_t in C++ generator mode

- Fix includes setup for parser bindings generation in macOS host platform

- Fix generation of field property setters in C++ generator

- Fix FieldToProperty pass to ignore non-public properties in C++ generator

- Fix declaration access for generated fields flattened from anonymous types

- Change standard type maps to be more specific about its supported generators

- Generate forward ref header for enum defined inside a class in C++/CLI (#1322) - Ali Alamiri

- Move the GenerateInclude logic to GetTypeReference (#1319) - Ali Alamiri

- By reference enum param fixes (#1321) - Ali Alamiri

- Add declaration context as object to function class block - Ali Alamiri

- Add blocks for ctor, dtor, and finalizer bodies. Add constructor that takes a bool from the caller to indicate if the callee should own the pointer passed to it or not - Ali Alamiri

- Add generic type map handling methods for later refactoring of generators

- Fix type printing of typedef qualifiers in C++ type printer

- Fix C++ parser ambiguity in generation of C++ method identifiers by wrapping them in parens

- Fix generation of C++ constructor for abstract classes

- Fix generation of native instance constructors in C++ generator

- Fix marshaling of C++ references in C++ marshaler

- Minor refactoring to allow better IDE inspection and debugging

- Rewrite GenerateEnumFromMacros to search through multiple translation units

- Fix CppTypePrinter to check for typemaps for tag types and keep track of them

- Implement a few overloads in CppTypePrinter that trigger the type maps checking code

- Fix ignore type checking to take type maps into account

- Fix ignored declaration checking to properties desugar field types

- Fix GetterSetterToProperty pass heuristic to also detect “on” as a verb

- CppTypePrinter now takes a BindingContext for further usage in type maps handling

- Only generate “override” in header files for C++ generator

- Guard MoveFunctionToClassPass pass registration against CLI and C# generators

- Ignore generated field method acessors when processing properties in GetterSetterToProperty

- Add cli namespace to header to ensure the array type does not conflict with other types called array - Ali Alamiri

- Marshal non primitive fixed arrays (#1311) - Ali Alamiri

- Ensure typedefs of std::vector are mapped - Ali Alamiri

- Simplify the structure of the LLVM package

- Always keep default constructors in the AST

- Keep copy/move constructors and assignment in AST

- Move the body of a template function to its header

- Implement proper array printing with C array name suffix notation

- Fix CLITypeReferences so it generates regular C++ code in C++ generator mode

- Add CXXOperatorArityZero enum item for further usage in subsequent code

- Initial C++ generator backend


## [0.10.2] - 2020-3-28

- Associate getter/setter methods with their associated property in GetterSetterToProperty pass

- Added optional getter/setter pair creation for fields in FieldToProperty pass

- Refactor CLI handling of enums to be based on generic C generator

- Default to .h file extension in C code generator

- Add helper methods to ignore and generate specific translation units

- Guard a few more language-specific passes in the driver

- Fix generation of multiple interfaces in C# for some edge cases

- Fix templates to be abstract if any specialization is abstract

- Fix TranslationUnit.FileRelativePath for invalid units

- Re-use common C++ declaration type printing code in CLITypePrinter

- Allow changing the default TypePrinterContextKind when creating a type printer

- Remove needless pointer offsets from generated C#

- Fix a crash for secondary bases with secondary bases

- Fix bug related to processing of potential property methods

- Ensure generated symbols can use placement new

- Fix Mono not being found on the PATH on recent macOS versions

- Instantiate exception specifications before reading

- Update LLVM to the latest version

- Fix a syntax error in CMake listing Clang modules

- Enable building lld as part of LLVM

- Restore modules needed for compiling with Clang

- Support Visual Studio 2019 for building Clang

- Update Travis to Ubuntu Xenial 18.04

- Simplify and optimize the printing of pointers in C++

- Fix printing of function pointers in C++

- Don't export symbols for explicit specializations

- Avoid invalid template specializations in tests

- Update the printing of comments to the new Clang

- Work around MSVC 32 crashing reading of exported symbols in Mach-O

- Improve Xcode toolchain lookup to be more robust

- Implement logic for -fgnuc-version= argument required by LLVM

- Update LLVM to the latest version

- Refactor Clang builtins directory logic and move it to the managed side

- Escape C# strings correctly

- Improve CS_INTERNAL so it now applies to all declarations

- Print the parser target triple in verbose mode

- Always re-create the Clang resource directory when building

- Cleanup Clang resource directory lookup logic

- Remove old workaround for testing issue that does not seem necessary anymore

- Git ignore .vscode directory

- Workaround Premake issue when copying the resource include directories at build time

- Fix warning about #pragma once in source file

- Update bootstrap tool to run against latest LLVM

- Update bootstrap tool to find LLVM build directory

- Add options to disable tests and examples from the build

- Improve the ignoring of dependent name types

- Implement UnresolvedUsingType and UnresolvedUsingTypename

- Fix the tests for exception types

- Switch to Xcode 11 in Travis CI

- Extend printing and reading of exception types

- Fix the check to ignore dependent name types

- Ignore unused destructors when generating symbols

- Fix the printing of "noexcept" in C++

- Make destructors virtual in abstract classes for tests

- Avoid generating abstract implementations for template classes

- Fix template type checking in CovariantTypeComparer

- Git ignore nested temporary obj folders

- Workaround System.TypeLoad exception when running test-suite on macOS

- Fix enum with zeros for hex literals

- Fix the moving of free functions to classes to match by module too

- Generate valid C# when an external module has an unsupported operator

- Fix a possible overflown stack when ignoring

- Force compilation of all functions of specializations

- Fill in missed values when cloning functions

- Optimize the moving of functions to classes

- Delete a custom pass added as standard

- Fix the C++ printing of function pointers in parameters

- Eliminate generated symbols for ignored functions

- Fix printing of type defs in C++

- Remove the internal C# functions for virtual destructors

- Give unique names to exported inlined functions

- Generate symbols for methods of specializations

- Optimize all passes which visited useless items

- Make the pass for properties more extendable

- Simplify overrides of overrides of secondary bases

- Optimize calls to base getters in properties

- Fix comparison of char and const char* in overloading

- Optimize the pass for properties

- Clarify limitations around exceptions and RTTI

- Destroy returned by value std::strings

- Upgrade ANSI marshalling to UTF-8 marshalling

- Generate valid C# when a renamed override causes conflicts

- Ensure protected nested types are accessible with multiple inheritance

- Fix the regressed indentation of printed comments

- Generate projects for .NET 4.7 to use new features

- Simplify the generated C# for marshalling strings


## [0.10.1] - 2019-7-4

- Fix the getting of references to pointers in C#

- Fix the passing of references to pointers in C#

- Prefer non-mapped types when resolving ambiguous overloads

- Make indexers use non-trivial copy ctors if any

- Fix a memory leak when passing an indirect std::string

- Build the generator before test bindings for easy testing

- Fix memory leaks in the map for std::string

- Fix the passing of std::string by value

- Guard against null for objects passed by value

- Generate valid C# for implicit conversion to const char*


## [0.10.0] - 2019-6-25

- Generate valid C# when passing a const ref to char

- Generate valid C# when a secondary base has a public anonymous field

- Generate valid C# when a method from a secondary base has no native symbol

- Generate valid C# for typedef-ed type parameters

- Ensure found type maps always contain the type sought after

- Flatten anonymous types to avoid empty names

- Generate valid C# for template parameters with a default value

- Fix returned objects by value attributed with inalloca

- Fix default arguments to only map to null if pointers

- Generate valid C# for returned const void pointers

- Fix regressions with void pointers and references

- Generate valid C# for r-values to void pointers

- Make the default build for Windows 64-bit

- Sped the LLVM build on Windows up by parallelizing

- Generate valid C# when a field with an anon type starts with '$'

- Generate valid C# when a function is named "get<number>"

- Enable Clang-based look-up for system includes by default


## [0.9.2] - 2019-5-8

- Fix the Windows build by not storing Unicode in std::string

- Fixed type map support for typedef types

- Name anonymous types after the fields which use them

- Generate valid C# when std::string is only used for variables

- Generate valid C# when std::string is only used for non-private fields

- Support indirect parameters

- Add a test for passing by value of structs with copy ctors

- Add parsing and AST support for RecordArgABI information in class records.

- Fix the generated C++ for Xcode 10.2

- Optimize renaming of declarations named after keywords

- Optimize the cleaning of invalid names

- Fix a crash when a function pointer takes a function pointer

- Generate valid C# for returned const char*&

- Generate valid C# for overloads with types nested in templates

- Fix the naming of anonymous types when 2+ types are nested 2+ levels

- Remove ParserOptions.Abi since its misleading as it serves no purpose

- Improved robustness when parsing C++ ABI kind


## [0.9.1] - 2019-4-13

- Generate valid C# for template indexers taking const char*

- Restore removed specializations

- Add a generic pointer to resolve ambiguity

- Fix a crash when a function pointer is a template arg

- Expose public anonymous types

- Fix the generated C# for fields of type function pointer

- Fix the generated C# for const char*&

- Fix the pass for duplicate names not to compare return parameters

- Fix the generated C# when type arguments are mapped the same

- Fix typo in options: chsarp -> csharp

- Fix #1191 CppSharp.CLI.exe --rtti sets -fno-rtti to clang

- Fix the generated C# for a case with 2 template args

- Fix the generation of properties for locations in expressions

- Added statement visiting to IAstVisitor

- Fix the generated C# when a dependent param has a default value

- Fix ambiguous code when a nested type and a property-like method with overloads have the same name


## [0.9.0] - 2019-3-1

- Extended the AST with C++ statements and expressions.

- Made public the finding of enabled type maps by strings.

- Fixed the renaming of properties with the same name as their owners.

- Simplified the pass for properties.

- Skip ignored bases in `ClassExtensions.GetBaseProperty`.

- Fixed missing options copy in ParserOptions copy constructor.

- Use MSBuild when building LLVM on Windows due to PDB issues with Ninja.

- Improve robustness when parsing types and decls.

- Fixed indentation regressions.

- Deleted useless output.

- Fixed naming edge case in `GenerateProperties.GetReadWritePropertyName`.

- Added `PrintModuleOutputNamespace` option to `CSharpTypePrinter`.

- Fixed extraneous new lines when generating multi-line comments.

- Obsoleted all hand-written types for expressions.

- Use `takeError()` when handling errors in parsing libraries.

- Fixed a crash with `TranslationUnit.FileName` property.

- Added `ForceClangToolchainLookup` option to force to use Clang's toolchain lookup code.

- Extract `ParserOptions` cloning code into a copy constructor.

- Improve `ParserOptions.Verbose` to print compiler arguments.

- Fixed `Options.DryRun` to not generate any binding code.

- Added some helper methods in `Enumeration` to work with scoped enumerations.

- Added a parsing option to skip gathering of native layout info.

- Fixed the generated C# when an instance method has a parameter named "instance".

- Fixed the generated C# for const/non-const overloads with > 1 param.

- Fixed the generated C# when a ref parameter is named after a keyword.

- Fixed the generation for parameters of type void**.

- Fixed the generated C# for indexers in templates specialized with void*.

- Fixed the generated C# for template specializations of pointers.

- Fixed the generated C# for const void*& in parameters.

- Fixed the generated C# when returning a non-const char*.

- Fixed the generated C# for parameters initialized with {}.

- Fixed the generated C# when a template is specialized with T and const T.

- Fixed the generated C# when an unsigned enum is assigned a negative value.


## [0.8.23] - 2019-1-31

- Keep Clang libTooling libs when packaging LLVM.

- Improve error handling in build scripts `UseClang()`.

- Added .NET Core build folder to Git Ignore .

- Initial integration of Clang AST viewer GUI tool.

- Made an exception serializable and removed another.

- Fixed the warnings in the test C++ for C# only.

- Fixed a crash when parsing libraries on macOS.

- Fixed error handling when parsing non-existent libraries.

- Added support for building with Clang and LLD.

- Switched to use csc.exe Roslyn compiler under Mono

- Disable most of the support for explicit pre-C++11 ABI since we do not need it anymore

- Fixed warnings in native test code.

- Fixed the generation of dependent virtual methods.

- Fixed overloading of operators with parameters mapped to the same type.

- Extended the type maps for primitive strings to C++/CLI.

- Handled int and long in maps to help resolve ambiguity.

- Simplified type maps by unlinking them from declarations.

- Properly hashed types to optimize their storage in maps.

- Fixed right-value references creating ambiguous overloads.

- Fixed the generated code in a case of ambiguous overloads.

- Added type maps for primitive strings (pointers to char).

- Added an option for skipping private declarations.

- Tested indirect calls from native code of overrides in the target language.

- Initial support for building under .NET Core.

- Updated the CI on Linux to use GCC 7.

- Exported all additional symbols on macOS.

- Fixed error handling and message when parsing non-existent files.

- Added a test for passing an std::string by value.

- Fixed the marshalling of std::string with GCC 6+ on Linux.

- Added a type map for char.

- Make Windows10SDK detection more robust

- Cached found type maps for faster look-ups.

- Deleted unused and slow code.

- Fixed the parsing of functions with integral template args.

- Decreased the build time on the Linux CI.

- Fixed a crash when parsing type aliases.

- Fixed the build of the parser when its path has spaces.

- Changed type maps to only return types - no strings.

- Simplified type maps by using static objects to disable as needed.

- Optimized the walking of the managed AST.

- Optimized the generation of C# by not splitting any strings.

- Optimized the walking of the AST by simplifying its search.

- Fixed the late parsing of templates.

- Fixed LLVM/Clang tar.gz archive extraction using 7-Zip on Windows.

- Fixed Windows SDK version detection in build scripts.

- Resolved ambiguity between char-like types in the generated C#.

- Fixed the generated C# for templates with > 1 ctor taking a pointer to a class.

- Fixed the generated C# for pure virtual functions with default arguments.

- Fixed the generated C# for default arguments of type pointer to a function.

- Fixed the generated C# for a certain case of two default parameters.

- Fixed the generated C# for arguments with default values of "nullptr".

- Fixed the generated C# for setters with default parameters.

- Fixed the generated C# for public fields with types mapped to primitive.

- Fixed the generated C# for constant references to primitives.

- Upgraded the CI script to use Ubuntu 16.04.

- Fixed ambiguity when the type of a parameter is mapped to a type in an overload.


## [0.8.22] - 2018-9-15

- Fixed renaming when items of an enum only differ by case.

- Fixed the generated C# for destructors of abstract classes.

- Stopped using methods deprecated in recent Clang.

- Excluded many unused modules when building LLVM and Clang.

- Worked around a missing symbol from a template specialization on macOS.

- Updated to LLVM/Clang revisions 339502/339494 respectively.

- Fixed the generation when a secondary base is used in more than one unit.

- Fixed debugger display variable reference in Block class.


## [0.8.21] - 2018-8-1

- Only generated the system module in C# generators.

- Fixed missing save of generated module template to outputs.

- Fixed code generator to generate the system module even in single file per unit mode.

- Silenced verbose duplicate constructor and operator warnings.

- Improved the defaults of necessary generation options to improve accessibility.

- Called the parser setup code in CLI.

- Only included header files when scanning directories in CLI.


## [0.8.20] - 2018-5-22

- Fixed generation support for pointers to enums in C#.

- Fixed a case of functions converted to methods.

- Improve error handling in case of exceptions in CLI tool driver.

- Added an option to the CLI tool for enabling RTTI.

- Improved the  messages for errors in the CLI tool.

- Added parameter index to managed marshal variables.

- Changed the generated C# for const references to primitives as just primitives.

- Error messages are now written to stderr.


## [0.8.19] - 2018-1-30

- Added getting of the path to Xcode based on xcode-select.


## [0.8.18] - 2018-1-28

- Do not generate wrappers for template specializations if original method in template class is ignored.

- Add one more include path which Linux usually expects.

- Evaluate expressions for enums generated using GenerateEnumFromMacros

- Evaluate expressions when generating enum from macros - ExpressionEvaluator taken from https://github.com/codingseb/ExpressionEvaluator

- Set the name-space for enums generated from macros.

- Preliminary script for building 32-bit Nuget package

- Field property getter returns non-value types by reference instead of by copy.

- Update VS check when downloading pre-compiled LLVM packages.

- Add `IgnoreConversionToProperty(pattern)` and `ForceConversionToProperty(pattern)`.

- Add `UsePropertyDetectionHeuristics` option to `DriverOptions`.

- Add "run" to verbs.txt

- Added support for 16-bit wide characters (char16_t).

- Fixed the generated C++ for symbols when protected classes need them.

- Removed the possibility for conflicts between overloads when generating C++ for symbols.


## [0.8.17] - 2017-12-25

- Collected extra symbols in the order of their compilation.

- Fixed the generated C# when a specialisation of a template used as a secondary base has an invalid function.

- Fixed the generated C# when a template interface specialised with another specialisation returns a template parameter.

- Fixed the generated C# when a default arg is assigned a specialisation also used as a secondary base.

- Fixed a crash when a constructor takes a template or specialisation.

- Fixed the generated C# for public fields with type a dependent pointer.

- Enabled classes having specializations for secondary bases to call their extensions.

- Fixed the generated C# for subclasses of specialisations used as secondary bases.

- Fixed the generated C# when a template specialization with extensions is used for a secondary base.

- Extended the multiple inheritance to work for templates.

- Fixed a regression causing public fields of type specialization to be ignored.

- Fixed the generated C# for templates with optional arguments.

- Represented "void" with "object" for template arguments in the generated C#.

- Fixed the generated C# for public fields with type a dependent function pointer.

- Fixed the generated C# when a constructor has a default parameter with type an external specialisation.

- Set an index when marshalling the value in setters of properties.

- Fixed the generated C# when a function uses an external specialisation.

- Fixed the generated C# when a base class is an external specialisation.

- Fixed the generated C# for indexers with dependent keys.

- Fixed the generated C# for templated indexers specialised with enums.

- Add compiler/abi information to LLVM package names on linux.

- Exported entire specialisations when they only have valid functions.

- Considered dependent fields when generating internals for types nested in templates.

- Removed extensions for non-generated template specialisations.

- Fixed the generated C# when a template returns a specialisation with itself as a type arg.

- Fixed the generated C# for members of types nested in templates.

- Fixed the generated C# when a template is nested in another.

- Add ability for managed module wrapper to reference extra assemblies.

- Re-added linux include path that was removed by mistake.

- Enable debug information generation for all tests.

- Fix debug output not being generated when AST element had no comment.

- Explicitly link to libstdc++ on linux.

- All arguments passed to `build/Compile.sh` are passed to premake. Allows more fine-grained build customization when using this shell script for compiling.

- Tweak linux include dirs, previous change broke GCC7 include dirs on archlinux.

- Consistent class/struct keywords fixed for cases where wrapper class would contain members from several different translation units.

- Fix debug output breaking generated binding code.

- Completely remove `GenerateUnformatted()` method.

- CI: x64 builds on msvc and sudo requirement for travis

- Always generate formatted code.

- Fix `Delegates` name-space being not generated. In some cases `Delegates` name-space could be attached to a name-space which is not wrapped and as a result of that `Delegates` name-space was also not generated in wrapper code resulting in a wrapper build errors. Change adds extra logic which tries to find the correct library name-space if more than one name-space is present.

- Consistently declare classes/structs. Fixes issue where compilation error is produced due to file name containing constants matching class marked as value type.

- Fix linking to LLVM libs on linux, when system has llvm/clang installed.

- Enable cxx11 abi for GCC 4.9+ on linux.

- Worked around a bug in the Mono C# compiler when casting generics.

- Fixed a crash when the body of a templated function contains references to non-functions.

- Use correct LLVM build dir for includes from unpackaged LLVM builds

- get_llvm_build_dir() returns "build" subdir if it exists, if not - subdir with package name.

- Fix linked libs for linux

- Use correct LLVM build dir for includes from unpackaged LLVM builds.

- Removed a duplicate explicit instantiation from the tests.

- Cloning llvm from git replaced with downloading archives of exact commits from github. This is much faster.

- Worked around duplication of types nested in templates and forwarded.

- Fixed a crash when passing null as an std::string on Unix.

- Force-set platform type to managed targets.

- Fix linux include paths in ParserGen and CLI generator.

- Fix build errors in CppSharp.Parser.Bootstrap target.

- Fixed a crash when there are parsing errors.

- Fixed the collection of additional symbols to ignore warnings.

- Fixed the generated C# when a constructor takes a specialisation.

- Fixed a possible crash when instantiating template functions in the parser.

- Fixed the generated C# for templates with fields other templates not used anywhere else.

- Fixed the generated C# when using std::map.

- Fixed the generated C# for specialisations with an ignored specialisation as an arg.

- Fixed the generated C# for specialisations only used as type arguments.

- Removed extensions for internal template specialisations.

- Fixed the parsing of an undeclared template specialisation with an extension method.

- Validated bodies of instantiated template functions.

- Added a new field accessor synth kind.

- Improved IsSynthetized check to handle property setters.

- Improve get base method and property methods to work with generalized declarations.

- Added AssociatedDeclaration to Declaration copy constructor.

- Included template specialisations only used as returned types.

- Included the destructor of std::allocator to the C++ symbols.

- Prevented C++ generation for invalid specialised functions.

- Fixed the generated C# for fixed arrays of Booleans

- Updated to LLVM/Clang revisions 318543/318538 respectively.

- Fixed the script for LLVM to handle paths with spaces.

- Generalized method fields to declaration associations.

- Improved debugging display for declarations.

- Added optional visiting of property accessors.

- CodeGenerator is now an IAstVisitor.

- Cleaned up the additional parser options after parsing headers.

- Fixed the generated C++ for symbols to be compatible with Clang.

- Fixed the generated C# when a type nested in a template is forwarded.


## [0.8.16] - 2017-11-10

- Fixed a crash when parsing unnamed declarations in name-spaces.


## [0.8.15] - 2017-11-7

- Handled any level of nesting when generating internals for specialisations in C#.

- Fixed the generation of internals for template specialisations.

- Ensured symbols for nested template specialisations.

- Fixed the generated C++ for external template specialisations.

- Completed just class template specialisations used in functions.

- Fixed a regression of generating templates in C# with the option off.

- Optimised the parser by skipping the system translation units.

- Reused parameters between functions and their types (as Clang does).

- Added an option for specifying STD types to bind.

- Reduced time and memory during generation by skipping methods of most STD classes.

- Improved the check for a layout on a record.

- Fixed a crash when trying to get a source location for an implicit declaration.

- Fixed the generated C# for a fixed array of pointers.

- Removed unused STD enumerations from generation.

- Fixed a crash when a comment contains regular text wrapped in <>.

- Made only really used classes internal.

- Fixed a typing error in the name of a function.

- Ignored return parameters when fixing default arguments of overrides.

- Ensured no overflown stack in the AST converter.

- Fixed code generation for using template types.

- Improved debugging display for Type type.

- Fixed incorrectly generated bindings for class with non-type template arguments.

- Fixed the generated C# for templates derived from regular dynamic classes.

- Ensured all non-system template specialisations are complete.

- Fixed a problem when walking the managed AST because friend templated classes were seen as declared multiple times and resulted into a crash.

- Improved type notation in the manual.

- Documented string marshaling behavior.

- Fixed implicit class record walking in the parser.

- Added a new verbose flag to the CLI tool and improved verbose handling.

- Fixed duplicate generation of forward declared class.

- Small cleanup and minor optimizations in ResolveIncompleteDeclsPass.

- Improved the fix for handling non-type params in template specialisation types.

- Fixed template parsing issue with processing of type locs.

- Fixed a parser bug when dealing with DependentTemplateSpecializationTypeLoc.

- Fixed an erroneous usage of LLVM cast with a regular C++ type. Only found with debug-mode LLVM build.

- Fixed VS2017 system includes search error #957 (#958)


## [0.8.14] - 2017-9-17

- Added experimental support for templates.

- Fixed the generated C# when a virtual function takes a fixed array.

- Fixed the generated C# for fixed arrays in types of parameters.

- Fixed the generated C# for virtuals taking arrays of objects.

- Lookup Mono SDK path on Windows registry.

- Fixed the generated C# when a virtual function takes an array.

- Fixed the generated C# with 4+ modules and repetitive delegates.

- Added C# marshalling of parameters of type array of const char* const.

- Added C# marshalling of parameters of type array of const char*.

- Fixed null arrays in C# to be passed as such to C/C++.

- Added C# marshalling of parameters of type array of objects.

- Added C# marshalling of parameters of type array of primitives.

- Added marshalling of parameters of type array of pointers.

- Fixed the generated C# for two anonymous types nested in another anonymous type.

- Removed unused internals from the generated C#.

- Added an example for the parser API-s.

- Add C++17 to the parser options

- Compile.sh script now has improved error handling.

- Linux toolchain can be supplied in the same spirit in path probing on Mac.

- Enabled empty arrays of non-primitives only when not using MSVC.

- Ignore zero-sized constant C array types.

- The compilation platform is now nullable by default and validated by the host platforms.

- Added LLVM target initialization and supporting libraries for parsing inline assembly.

- Fixed a crash when trying to use a VS version missing from the system.

- Fixed the binding of multiple identical function pointers with a calling convention.

- Got rid of anonymous names for delegates.

- Fixed the calling conventions of delegates.

- Ensures that if a delegate is used for a virtual as well as something else, it finally ends up as public.

- Fixed the code generation when the return type of a method is a function pointer that has been used somewhere else as well.

- Added Access and Calling convention to the delegate definition.

- Generated properties from setters returning Booleans.

- Added some aliases to options in the CLI tool.

- [generator] Improved processing for C++ inline namespaces.

- Fixed initial output messages in the CLI.

- Generated properties from <type> get()/void get(<type>) pairs.

- Restored the option for generating one C# file per unit.

- Fixed the sorting of modules to also work with manually added dependencies.

- Do not generated unformatted code if debug mode is enabled.

- Added an option to the CLI for enabling debug mode for generated output.

- Improved the directory setup in the CLI in case the path is not a file path.

- Adds a new option to the CLI for automatic compilation of generated code.

- Adds a new dedicated "-exceptions" flag to enable C++ exceptions in the CLI.

- Added a new -A option to the CLI to pass compiler arguments to Clang parser.

- Fixed the name of an option in the CLI.

- Removed the showing of help for the CLI if we have output an error previously.

- Improved error messages in the CLI.

- Improve platform detection in the CLI so the current platform is set by default.

- Fixed a directory check in the CLI that was throwing exceptions in Mono.

- Fixed the generated C# indexers for specialisations of pointers.

- Fixed the generated C# for increment and decrement operators.

- Removed leftovers in the comments from unsupported custom xml tags.

- Fixed the generation of symbols to check all base classes up the chain.

- Printed the text from unsupported comment tags.

- Fixed the generated C# for a case of a typedef of a function pointer.

    Typedefs of function pointers can be written in two ways:

      typedef void (*typedefedFuncPtr)();

      int f(typedefedFuncPtr fptr);

      typedef void (typedefedFuncPtr)();

      int f(typedefedFuncPtr* fptr);

      Up until now we only supported the former.

- Fixed the C# generation for functions with typedefed function pointers as params

- Set the name-space of a parameter to its function.

- Included the comments plain text to the remarks block.

- Fix the typo in LLVM.lua

- Prevented projects from being generated using GenerateProjects.bat

- Fixed the generated C# for setters with a reference to a primitive type.

- Ensured a single element for remarks in the generated XML documentation comments.

- Fixed the renaming of methods in forwarded types from secondary bases in dependencies.

- Added to a method a list of its overridden methods.

- Generated internals of external specialisations only if the template has template fields.

- Equalised the access of overrides and their base methods.

- Fixed the code generation for indexers returning a void pointer.

- Fixed the generated C# when a protected constructor has a parameter with a protected type.

- Fixed the generated C# when an external specialisation with a dependent field is used as a field.

- Made Function a DeclarationContext to match the Clang AST.

- Made the C/C++ language switches adjustable in managed code.

- Added an option to enable or disable RTTI.

- Fixed the generation of inlines to handle types in classes in name-spaces.