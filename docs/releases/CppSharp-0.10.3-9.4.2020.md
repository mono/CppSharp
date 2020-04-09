# CppSharp 0.10.3 9.4.2019

* Ensure complete template specializations in AST

* Add an option and checking for generation of deprecated declarations

* Implement parsing and AST processing of C++ deprecated attributes

* Make sure we use a native type printer for symbols code generation

* Git ignore new include folders for cross generation

* Fix marshaling for wchar_t in C++ generator mode

* Fix includes setup for parser bindings generation in macOS host platform

* Fix generation of field property setters in C++ generator

* Fix FieldToProperty pass to ignore non-public properties in C++ generator

* Fix declaration access for generated fields flattened from anonymous types

* Change standard type maps to be more specific about its supported generators

* Generate forward ref header for enum defined inside a class in C++/CLI (#1322) - Ali Alamiri

* Move the GenerateInclude logic to GetTypeReference (#1319) - Ali Alamiri

* By reference enum param fixes (#1321) - Ali Alamiri

* Add declaration context as object to function class block - Ali Alamiri

* Add blocks for ctor, dtor, and finalizer bodies. Add constructor that takes a bool from the caller to indicate if the callee should own the pointer passed to it or not - Ali Alamiri

* Add generic type map handling methods for later refactoring of generators

* Fix type printing of typedef qualifiers in C++ type printer

* Fix C++ parser ambiguity in generation of C++ method identifiers by wrapping them in parens

* Fix generation of C++ constructor for abstract classes

* Fix generation of native instance constructors in C++ generator

* Fix marshaling of C++ references in C++ marshaler

* Minor refactoring to allow better IDE inspection and debugging

* Rewrite GenerateEnumFromMacros to search through multiple translation units

* Fix CppTypePrinter to check for typemaps for tag types and keep track of them

* Implement a few overloads in CppTypePrinter that trigger the type maps checking code

* Fix ignore type checking to take type maps into account

* Fix ignored declaration checking to properties desugar field types

* Fix GetterSetterToProperty pass heuristic to also detect “on” as a verb

* CppTypePrinter now takes a BindingContext for further usage in type maps handling

* Only generate “override” in header files for C++ generator

* Guard MoveFunctionToClassPass pass registration against CLI and C# generators

* Ignore generated field method acessors when processing properties in GetterSetterToProperty

* Add cli namespace to header to ensure the array type does not conflict with other types called array - Ali Alamiri

* Marshal non primitive fixed arrays (#1311) - Ali Alamiri

* Ensure typedefs of std::vector are mapped - Ali Alamiri

* Simplify the structure of the LLVM package

* Always keep default constructors in the AST

* Keep copy/move constructors and assignment in AST

* Move the body of a template function to its header

* Implement proper array printing with C array name suffix notation

* Fix CLITypeReferences so it generates regular C++ code in C++ generator mode

* Add CXXOperatorArityZero enum item for further usage in subsequent code

* Initial C++ generator backend