# CppSharp 0.9.4 25.6.2019

* Generate valid C# when passing a const ref to char

* Generate valid C# when a secondary base has a public anonymous field

* Generate valid C# when a method from a secondary base has no native symbol

* Generate valid C# for typedef-ed type parameters

* Ensure found type maps always contain the type sought after

* Flatten anonymous types to avoid empty names

* Generate valid C# for template parameters with a default value

* Fix returned objects by value attributed with inalloca

* Fix default arguments to only map to null if pointers

* Generate valid C# for returned const void pointers

* Fix regressions with void pointers and references

* Generate valid C# for r-values to void pointers

* Make the default build for Windows 64-bit

* Sped the LLVM build on Windows up by parallelizing

* Generate valid C# when a field with an anon type starts with '$'

* Generate valid C# when a function is named "get<number>"

* Enable Clang-based look-up for system includes by default