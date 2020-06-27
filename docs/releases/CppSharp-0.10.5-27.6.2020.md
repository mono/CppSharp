# CppSharp 0.10.5 27.6.2020

* Don't add ABI-specific parameters when wrapping C

* Prioritize public non-field properties when resolving naming conflicts

* Fix patching of virtual tables for MSVC with RTTI

* Free the memory of the patched v-tables

* Fix parsing of member pointers with MSVC

* Generate valid C# for constructors taking const&

* Generate valid C# for returned function pointers

* Expose returned values of non-void setters

* Ensure enumerations lack conflicts when renamed

* Fix generation for fields of type const reference