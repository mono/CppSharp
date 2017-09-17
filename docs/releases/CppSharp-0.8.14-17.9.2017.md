# CppSharp 0.8.14 17.9.2017



Added experimental support for templates.

Fixed the generated C# when a virtual function takes a fixed array.

Fixed the generated C# for fixed arrays in types of parameters.

Fixed the generated C# for virtuals taking arrays of objects.

Lookup Mono SDK path on Windows registry.

Fixed the generated C# when a virtual function takes an array.

Fixed the generated C# with 4+ modules and repetitive delegates.

Added C# marshalling of parameters of type array of const char* const.

Added C# marshalling of parameters of type array of const char*.

Fixed null arrays in C# to be passed as such to C/C++.

Added C# marshalling of parameters of type array of objects.

Added C# marshalling of parameters of type array of primitives.

Added marshalling of parameters of type array of pointers.

Fixed the generated C# for two anonymous types nested in another anonymous type.

Removed unused internals from the generated C#.

Added an example for the parser API-s.

Add C++17 to the parser options

Compile.sh script now has improved error handling.

Linux toolchain can be supplied in the same spirit in path probing on Mac.

Enabled empty arrays of non-primitives only when not using MSVC.

Ignore zero-sized constant C array types.

The compilation platform is now nullable by default and validated by the host platforms.

Added LLVM target initialization and supporting libraries for parsing inline assembly.

Fixed a crash when trying to use a VS version missing from the system.

Fixed the binding of multiple identical function pointers with a calling convention.

Got rid of anonymous names for delegates.

Fixed the calling conventions of delegates.

Ensures that if a delegate is used for a virtual as well as something else, it finally ends up as public.

Fixed the code generation when the return type of a method is a function pointer that has been used somewhere else as well.

Added Access and Calling convention to the delegate definition.

Generated properties from setters returning Booleans.

Added some aliases to options in the CLI tool.

[generator] Improved processing for C++ inline namespaces.

Fixed initial output messages in the CLI.

Generated properties from <type> get()/void get(<type>) pairs.

Restored the option for generating one C# file per unit.

Fixed the sorting of modules to also work with manually added dependencies.

Do not generated unformatted code if debug mode is enabled.

Added an option to the CLI for enabling debug mode for generated output.

Improved the directory setup in the CLI in case the path is not a file path.

Adds a new option to the CLI for automatic compilation of generated code.

Adds a new dedicated "-exceptions" flag to enable C++ exceptions in the CLI.

Added a new -A option to the CLI to pass compiler arguments to Clang parser.

Fixed the name of an option in the CLI.

Removed the showing of help for the CLI if we have output an error previously.

Improved error messages in the CLI.

Improve platform detection in the CLI so the current platform is set by default.

Fixed a directory check in the CLI that was throwing exceptions in Mono.

Fixed the generated C# indexers for specialisations of pointers.

Fixed the generated C# for increment and decrement operators.

Removed leftovers in the comments from unsupported custom xml tags.

Fixed the generation of symbols to check all base classes up the chain.

Printed the text from unsupported comment tags.

Fixed the generated C# for a case of a typedef of a function pointer.

    - Typedefs of function pointers can be written in two ways:

      typedef void (*typedefedFuncPtr)();

      int f(typedefedFuncPtr fptr);

      typedef void (typedefedFuncPtr)();

      int f(typedefedFuncPtr* fptr);

      Up until now we only supported the former.

Fixed the C# generation for functions with typedefed function pointers as params

Set the name-space of a parameter to its function.

Included the comments plain text to the remarks block.

Fix the typo in LLVM.lua

Prevented projects from being generated using GenerateProjects.bat

Fixed the generated C# for setters with a reference to a primitive type.

Ensured a single element for remarks in the generated XML documentation comments.

Fixed the renaming of methods in forwarded types from secondary bases in dependencies.

Added to a method a list of its overridden methods.

Generated internals of external specialisations only if the template has template fields.

Equalised the access of overrides and their base methods.

Fixed the code generation for indexers returning a void pointer.

Fixed the generated C# when a protected constructor has a parameter with a protected type.

Fixed the generated C# when an external specialisation with a dependent field is used as a field.

Made Function a DeclarationContext to match the Clang AST.

Made the C/C++ language switches adjustable in managed code.

Added an option to enable or disable RTTI.

Fixed the generation of inlines to handle types in classes in name-spaces.