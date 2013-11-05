CppSharp is a library that allows you to generate .NET bindings that wrap C/C++ code allowing interoperability with managed languages. This can be useful if you have an existing native codebase and want to add scripting support, or want to consume an existing native library in your managed code.

## News

* November 5th 2013 - Released a new version (329) with a lot of bug fixes for operators and vtables.
* September 22nd 2013 - Conversion (type cast) operators wrapped (thanks to <a href="https://github.com/ddobrev">@ddobrev</a>)
* September 21st 2013 - Multiple inheritance now supported (thanks to <a href="https://github.com/ddobrev">@ddobrev</a>)

* September 11th 2013 - Added wrapping of inlined functions (thanks to <a href="https://github.com/ddobrev">@ddobrev</a>)
* September 11th 2013 - New binaries available for Windows (VS2012)

## Binaries

VS2012 32-bit:

- [CppSharp_VS2012_329_artifacts.zip](https://dl.dropboxusercontent.com/u/194502/CppSharp/CppSharp_VS2012_329_artifacts.zip) (_November 5th 2013_) 

- [CppSharp_VS2012_306_artifacts.zip](https://dl.dropboxusercontent.com/u/194502/CppSharp/CppSharp_VS2012_306_artifacts.zip) (_October 25th 2013_) 

- [CppSharp_VS2012_183_artifacts.zip](https://dl.dropboxusercontent.com/u/194502/CppSharp/CppSharp_VS2012_183_artifacts.zip) (_September 17th 2013_)

## Getting started

The documentation is still a work-in-progress, please see the following resources for more information:

[Getting Started](docs/GettingStarted.md)

[User's Manual](docs/UsersManual.md)

[Developer's Manual](docs/DevManual.md)

## Why reinvent the wheel?

There are not many automated binding tools around, the only real alternative is SWIG. So how is it different from SWIG?

 * No need to generate a C layer to interop with C++.
 * Based on an actual C++ parser (Clang) so very accurate.
 * Understands C++ at the ABI (application binary interface) level
 * Easily extensible semantics via user passes
 * Strongly-typed customization APIs
 * Can be used as a library

## Can I use it yet?

It is being used to bind "real-world" complex codebases successfully, so give it a shot.

Since C and C++ provide such a wide array of features I'm sure there's still tonnes of bugs and unsupported edge cases, but give it a try and report any bugs you find and I'll try to fix them ASAP.


## Similiar Tools

* Sharppy - .NET bindings generator for unmanaged C++
[https://code.google.com/p/sharppy/](https://code.google.com/p/sharppy/)

* XInterop
[http://xinterop.com/](http://xinterop.com/)

* SWIG
[http://www.swig.org/](http://www.swig.org/)

* Cxxi
[https://github.com/mono/cxxi/](https://github.com/mono/cxxi/)
