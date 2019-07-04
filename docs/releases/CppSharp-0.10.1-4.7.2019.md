# CppSharp 0.10.1 7.4.2019

* Fix the getting of references to pointers in C#

* Fix the passing of references to pointers in C#

* Prefer non-mapped types when resolving ambiguous overloads

* Make indexers use non-trivial copy ctors if any

* Fix a memory leak when passing an indirect std::string

* Build the generator before test bindings for easy testing

* Fix memory leaks in the map for std::string

* Fix the passing of std::string by value

* Guard against null for objects passed by value

* Generate valid C# for implicit conversion to const char*