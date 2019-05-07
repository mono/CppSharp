# CppSharp 0.9.2 8.5.2019

* Fix the Windows build by not storing Unicode in std::string

* Fixed type map support for typedef types

* Name anonymous types after the fields which use them

* Generate valid C# when std::string is only used for variables

* Generate valid C# when std::string is only used for non-private fields

* Support indirect parameters

* Add a test for passing by value of structs with copy ctors

* Add parsing and AST support for RecordArgABI information in class records.

* Fix the generated C++ for Xcode 10.2

* Optimize renaming of declarations named after keywords

* Optimize the cleaning of invalid names

* Fix a crash when a function pointer takes a function pointer

* Generate valid C# for returned const char*&

* Generate valid C# for overloads with types nested in templates

* Fix the naming of anonymous types when 2+ types are nested 2+ levels

* Remove ParserOptions.Abi since its misleading as it serves no purpose

* Improved robustness when parsing C++ ABI kind