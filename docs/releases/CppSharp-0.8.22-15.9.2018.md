# CppSharp 0.8.22 15.9.2018

* Fixed renaming when items of an enum only differ by case.

* Fixed the generated C# for destructors of abstract classes.

* Stopped using methods deprecated in recent Clang.

* Excluded many unused modules when building LLVM and Clang.

* Worked around a missing symbol from a template specialization on macOS.

* Updated to LLVM/Clang revisions 339502/339494 respectively.

* Fixed the generation when a secondary base is used in more than one unit.

* Fixed debugger display variable reference in Block class.