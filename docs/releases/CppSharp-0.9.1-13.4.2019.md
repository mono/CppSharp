# CppSharp 0.9.1 13.4.2019

* Generate valid C# for template indexers taking const char*

* Restore removed specializations

* Add a generic pointer to resolve ambiguity

* Fix a crash when a function pointer is a template arg

* Expose public anonymous types

* Fix the generated C# for fields of type function pointer

* Fix the generated C# for const char*&

* Fix the pass for duplicate names not to compare return parameters

* Fix the generated C# when type arguments are mapped the same

* Fix typo in options: chsarp -> csharp

* Fix #1191 CppSharp.CLI.exe --rtti sets -fno-rtti to clang

* Fix the generated C# for a case with 2 template args

* Fix the generation of properties for locations in expressions

* Added statement visiting to IAstVisitor

* Fix the generated C# when a dependent param has a default value

* Fix ambiguous code when a nested type and a property-like method with overloads have the same name