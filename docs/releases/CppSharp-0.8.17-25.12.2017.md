# CppSharp 0.8.17 25.12.2017



Collected extra symbols in the order of their compilation.

Fixed the generated C# when a specialisation of a template used as a secondary base has an invalid function.

Fixed the generated C# when a template interface specialised with another specialisation returns a template parameter.

Fixed the generated C# when a default arg is assigned a specialisation also used as a secondary base.

Fixed a crash when a constructor takes a template or specialisation.

Fixed the generated C# for public fields with type a dependent pointer.

Enabled classes having specializations for secondary bases to call their extensions.

Fixed the generated C# for subclasses of specialisations used as secondary bases.

Fixed the generated C# when a template specialization with extensions is used for a secondary base.

Extended the multiple inheritance to work for templates.

Fixed a regression causing public fields of type specialization to be ignored.

Fixed the generated C# for templates with optional arguments.

Represented "void" with "object" for template arguments in the generated C#.

Fixed the generated C# for public fields with type a dependent function pointer.

Fixed the generated C# when a constructor has a default parameter with type an external specialisation.

Set an index when marshalling the value in setters of properties.

Fixed the generated C# when a function uses an external specialisation.

Fixed the generated C# when a base class is an external specialisation.

Fixed the generated C# for indexers with dependent keys.

Fixed the generated C# for templated indexers specialised with enums.

Add compiler/abi information to LLVM package names on linux.

Exported entire specialisations when they only have valid functions.

Considered dependent fields when generating internals for types nested in templates.

Removed extensions for non-generated template specialisations.

Fixed the generated C# when a template returns a specialisation with itself as a type arg.

Fixed the generated C# for members of types nested in templates.

Fixed the generated C# when a template is nested in another.

Add ability for managed module wrapper to reference extra assemblies.

Re-added linux include path that was removed by mistake.

Enable debug information generation for all tests.

Fix debug output not being generated when AST element had no comment.

Explicitly link to libstdc++ on linux.

All arguments passed to `build/Compile.sh` are passed to premake. Allows more fine-grained build customization when using this shell script for compiling.

Tweak linux include dirs, previous change broke GCC7 include dirs on archlinux.

Consistent class/struct keywords fixed for cases where wrapper class would contain members from several different translation units.

Fix debug output breaking generated binding code.

Completely remove `GenerateUnformatted()` method.

CI: x64 builds on msvc and sudo requirement for travis

Always generate formatted code.

Fix `Delegates` name-space being not generated. In some cases `Delegates` name-space could be attached to a name-space which is not wrapped and as a result of that `Delegates` name-space was also not generated in wrapper code resulting in a wrapper build errors. Change adds extra logic which tries to find the correct library name-space if more than one name-space is present.

Consistently declare classes/structs. Fixes issue where compilation error is produced due to file name containing constants matching class marked as value type.

Fix linking to LLVM libs on linux, when system has llvm/clang installed.

Enable cxx11 abi for GCC 4.9+ on linux.

Worked around a bug in the Mono C# compiler when casting generics.

Fixed a crash when the body of a templated function contains references to non-functions.

Use correct LLVM build dir for includes from unpackaged LLVM builds

get_llvm_build_dir() returns "build" subdir if it exists, if not - subdir with package name.

Fix linked libs for linux

Use correct LLVM build dir for includes from unpackaged LLVM builds.

Removed a duplicate explicit instantiation from the tests.

Cloning llvm from git replaced with downloading archives of exact commits from github. This is much faster.

Worked around duplication of types nested in templates and forwarded.

Fixed a crash when passing null as an std::string on Unix.

Force-set platform type to managed targets.

Fix linux include paths in ParserGen and CLI generator.

Fix build errors in CppSharp.Parser.Bootstrap target.

Fixed a crash when there are parsing errors.

Fixed the collection of additional symbols to ignore warnings.

Fixed the generated C# when a constructor takes a specialisation.

Fixed a possible crash when instantiating template functions in the parser.

Fixed the generated C# for templates with fields other templates not used anywhere else.

Fixed the generated C# when using std::map.

Fixed the generated C# for specialisations with an ignored specialisation as an arg.

Fixed the generated C# for specialisations only used as type arguments.

Removed extensions for internal template specialisations.

Fixed the parsing of an undeclared template specialisation with an extension method.

Validated bodies of instantiated template functions.

Added a new field accessor synth kind.

Improved IsSynthetized check to handle property setters.

Improve get base method and property methods to work with generalized declarations.

Added AssociatedDeclaration to Declaration copy constructor.

Included template specialisations only used as returned types.

Included the destructor of std::allocator to the C++ symbols.

Prevented C++ generation for invalid specialised functions.

Fixed the generated C# for fixed arrays of Booleans

Updated to LLVM/Clang revisions 318543/318538 respectively.

Fixed the script for LLVM to handle paths with spaces.

Generalized method fields to declaration associations.

Improved debugging display for declarations.

Added optional visiting of property accessors.

CodeGenerator is now an IAstVisitor.

Cleaned up the additional parser options after parsing headers.

Fixed the generated C++ for symbols to be compatible with Clang.

Fixed the generated C# when a type nested in a template is forwarded.