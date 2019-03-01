# CppSharp 0.9.0 1.3.2019

* Extended the AST with C++ statements and expressions.

* Made public the finding of enabled type maps by strings.

* Fixed the renaming of properties with the same name as their owners.

* Simplified the pass for properties.

* Skip ignored bases in `ClassExtensions.GetBaseProperty`.

* Fixed missing options copy in ParserOptions copy constructor.

* Use MSBuild when building LLVM on Windows due to PDB issues with Ninja.

* Improve robustness when parsing types and decls.

* Fixed indentation regressions.

* Deleted useless output.

* Fixed naming edge case in `GenerateProperties.GetReadWritePropertyName`.

* Added `PrintModuleOutputNamespace` option to `CSharpTypePrinter`.

* Fixed extraneous new lines when generating multi-line comments.

* Obsoleted all hand-written types for expressions.

* Use `takeError()` when handling errors in parsing libraries.

* Fixed a crash with `TranslationUnit.FileName` property.

* Added `ForceClangToolchainLookup` option to force to use Clang's toolchain lookup code.

* Extract `ParserOptions` cloning code into a copy constructor.

* Improve `ParserOptions.Verbose` to print compiler arguments.

* Fixed `Options.DryRun` to not generate any binding code.

* Added some helper methods in `Enumeration` to work with scoped enumerations.

* Added a parsing option to skip gathering of native layout info.

* Fixed the generated C# when an instance method has a parameter named "instance".

* Fixed the generated C# for const/non-const overloads with > 1 param.

* Fixed the generated C# when a ref parameter is named after a keyword.

* Fixed the generation for parameters of type void**.

* Fixed the generated C# for indexers in templates specialized with void*.

* Fixed the generated C# for template specializations of pointers.

* Fixed the generated C# for const void*& in parameters.

* Fixed the generated C# when returning a non-const char*.

* Fixed the generated C# for parameters initialized with {}.

* Fixed the generated C# when a template is specialized with T and const T.

* Fixed the generated C# when an unsigned enum is assigned a negative value.