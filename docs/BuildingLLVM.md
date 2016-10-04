# Compiling LLVM and Clang from source

This document explains how to build LLVM and Clang from source code.

It's a process only recommended for developers that need to make changes to LLVM or Clang, or
build the binary packages needed for the CI system.

Git repository URLs found here: [http://llvm.org/docs/GettingStarted.html#git-mirror]
(http://llvm.org/docs/GettingStarted.html#git-mirror)

1. Clone LLVM to `<CppSharp>\deps\llvm`
2. Clone Clang to `<CppSharp>\deps\llvm\tools\clang`

Required LLVM/Clang commits:

[LLVM: see /build/LLVM-commit.](https://github.com/mono/CppSharp/tree/master/build/LLVM-commit)

[Clang: see /build/Clang-commit.](https://github.com/mono/CppSharp/tree/master/build/Clang-commit)

## Compiling on Windows/Visual Studio

```shell
cd <CppSharp>\deps\llvm\build

cmake -G "Visual Studio 12" -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false -DCLANG_INCLUDE_DOCS=false -DCLANG_BUILD_EXAMPLES=false -DLLVM_TARGETS_TO_BUILD="X86" -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false ..

msbuild LLVM.sln /p:Configuration=RelWithDebInfo;Platform=Win32 /m
```

Or, if you need 64-bit binaries:

```shell
cd <CppSharp>\deps\llvm\build

cmake -G "Visual Studio 12 Win64" -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false -DCLANG_INCLUDE_DOCS=false -DCLANG_BUILD_EXAMPLES=false -DLLVM_TARGETS_TO_BUILD="X86" -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false ..

msbuild LLVM.sln /p:Configuration=RelWithDebInfo;Platform=x64 /m
```

## Compiling on Mac OS X

### Compiling manually

1. Compile LLVM solution in *RelWithDebInfo* mode
   The following CMake variables should be enabled:
    - LLVM_ENABLE_LIBCXX (enables libc++ standard library support)
    - LLVM_BUILD_32_BITS for 32-bit builds (defaults to 64-bit)

```shell
mkdir -p deps/llvm/build && cd deps/llvm/build

cmake -G "Unix Makefiles" -DLLVM_ENABLE_LIBCXX=true -DLLVM_BUILD_32_BITS=true -DCMAKE_BUILD_TYPE=RelWithDebInfo ..

make
```

### Compiling using the build script

Before building, ensure cmake is installed under Applications/Cmake.app and Ninja is installed in your PATH.

1. Navigate to `build/scripts`
2. Clone, build and package LLVM with
```
../premake5-osx --file=LLVM.lua clone_llvm
../premake5-osx --file=LLVM.lua build_llvm
../premake5-osx --file=LLVM.lua package_llvm
```

If the clone_llvm step fails, you can try to manually clone LLVM and Clang as explained above. You should still run clone_llvm to ensure that you are on the correct revision.

The compile flags for cmake can be edited in `build/scripts/LLVM.lua`, e.g. if you need to build a 64-bit version.


## Compiling on Linux

If you do not have native build tools you can install them first with:

```shell
sudo apt-get install cmake ninja-build build-essential
```

And then build LLVM with:

```shell
cd deps/llvm/build

cmake -G Ninja -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false -DCLANG_INCLUDE_DOCS=false -DCLANG_BUILD_EXAMPLES=false -DLLVM_TARGETS_TO_BUILD="X86" -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false ..

ninja
```
