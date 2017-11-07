# CppSharp 0.8.15 7.11.2017



Handled any level of nesting when generating internals for specialisations in C#.

Fixed the generation of internals for template specialisations.

Ensured symbols for nested template specialisations.

Fixed the generated C++ for external template specialisations.

Completed just class template specialisations used in functions.

Fixed a regression of generating templates in C# with the option off.

Optimised the parser by skipping the system translation units.

Reused parameters between functions and their types (as Clang does).

Added an option for specifying STD types to bind.

Reduced time and memory during generation by skipping methods of most STD classes.

Improved the check for a layout on a record.

Fixed a crash when trying to get a source location for an implicit declaration.

Fixed the generated C# for a fixed array of pointers.

Removed unused STD enumerations from generation.

Fixed a crash when a comment contains regular text wrapped in <>.

Made only really used classes internal.

Fixed a typing error in the name of a function.

Ignored return parameters when fixing default arguments of overrides.

Ensured no overflown stack in the AST converter.

Fixed code generation for using template types.

Improved debugging display for Type type.

Fixed incorrectly generated bindings for class with non-type template arguments.

Fixed the generated C# for templates derived from regular dynamic classes.

Ensured all non-system template specialisations are complete.

Fixed a problem when walking the managed AST because friend templated classes were seen as declared multiple times and resulted into a crash.

Improved type notation in the manual.

Documented string marshaling behavior.

Fixed implicit class record walking in the parser.

Added a new verbose flag to the CLI tool and improved verbose handling.

Fixed duplicate generation of forward declared class.

Small cleanup and minor optimizations in ResolveIncompleteDeclsPass.

Improved the fix for handling non-type params in template specialisation types.

Fixed template parsing issue with processing of type locs.

Fixed a parser bug when dealing with DependentTemplateSpecializationTypeLoc.

Fixed an erroneous usage of LLVM cast with a regular C++ type. Only found with debug-mode LLVM build.

Fixed VS2017 system includes search error #957 (#958)