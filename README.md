CppSharp is a binding tool that automatically generates either C#
or C++/CLI wrappers around your C/C++ libraries by parsing headers.


Directory structure
-------------------

Manual.md
  Work-in-progress documentation for this tool.

build/
  Premake build scripts.

src/
  Runtime
    Helper runtime library to bridge the C++ standard library.
  Bridge
    Contains the needed classes to bridge the Clang parser to .NET.
  Parser
    C++/CLI based wrapper around the C++ Clang libraries.
  Generator
    The Clang-based binding generator.

tests/
  Regression tests.

examples/
  Hello
    Small, Hello, World! example.