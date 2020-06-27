# CppSharp 0.10.4 23.5.2020

* Simplify the required headers for macOS bindings

* Option to allow caller to specify it does not want unchanged output files to be modified. This supports incremental build in VS. (#1373) - Ali Alamiri <ali.alamiri@sage.com>

* CreateInstance factory overload to pass flag informing wrapper if it should own the native pointer passed to it. - Ali Alamiri <ali.alamiri@sage.com>

* force bash use to avoid `./premake5-linux: 3: ./premake5-linux: Bad substitution` error if other default shell in the system - Lorenzo Delana <lorenzo.delana@gmail.com>

* Made the original virtual tables static too

* Pass native pointers to bases in the generated C#

* Check type maps when printing C++ for pointers

* Do not add type alias templates twice to the AST

* Fix all leaks of memory in the old expressions

* Add template functions to their context

* Fix leaking memory by removing a useless assignment

* Fix leaking the memory of an entire Clang AST

* Ignore type maps when printing C++ for symbols

* Implement more accurate managed type printing in C++ type printer.

* Use a native type printer context when looking for type maps in CSharpSourcesExtensions.DisableTypeMap.

* Use explicit type printer when printing types in C# GenerateMethodSpecifier.

* Refactor CodeGenerator.GenerateMethodSpecifier to allow explicit specifier kind.

* Do not check declaration access for explicitly generated declarations.

* Fix TranslationUnit.FileRelativeDirectory to be more robust against null include paths.

* Fix formatting to of Declaration.GenerationKind to ease debugging.

* Ignore implicitly deleted copy constructor methods.

* Correctly marshal constant arrays in C++/CLI (#1346)

* Marshal pointer to primitive typedefs in C++/CLI (#1355) - Ali Alamiri <ali.alamiri@sage.com>

* Fix a regression when renaming classes

* Fix naming conflicts with nested types and members

* publish the clang lib folder - Ali Alamiri <ali.alamiri@sage.com>

* Implement basic support for parsing function-like macros.

* Implement TranslationUnit.ToString() to help with debugging.

* Add debug option and flags to the Premake build and compile scripts.

* Generate valid C# for parameters typedef-ed to mapped types

* Update the version of Mono used for builds (CI)

* Fix the regressed C# marshalling of char*

* Handle pointer to pointer param (#1343) - Ali Alamiri <ali.alamiri@sage.com>

* Handle returned pointers to std::vector in C++/CLI - Ali Alamiri <ali.alamiri@sage.com>

* Implement abstract templates to call virtuals

* Correctly align printed information for debugging

* Set the render kind of inline command comments

* Fix all memory leaks in tests

* Generate by ref parameters of type a pointer to enum

* Use UnsupportedType description for type name instead of empty string (#1339) - Ali Alamiri <ali.alamiri@sage.com>