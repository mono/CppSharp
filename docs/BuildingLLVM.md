# Compiling LLVM and Clang from source

This document explains how to build LLVM and Clang from source code.

It's a process only recommended for developers that need to make changes to LLVM or Clang, or
build the binary packages needed for the CI system. If you just want to build CppSharp, then
check out the getting started guide section on how to download our pre-compiled LLVM packages.

## Compiling using the build script

This is the preferred way to compile LLVM for usage by CppSharp.

1. Before building, ensure Git, CMake and Ninja are installed and acessible from the command line.

Check the official download pages for each project for further instructions:

- [Git](https://git-scm.com/downloads)
- [Ninja](https://github.com/ninja-build/ninja/wiki/Pre-built-Ninja-packages)
- [CMake](https://cmake.org/download/)

2. Navigate to the `<CppSharp>/build` directory
3. Clone, build and package LLVM with
```
$PREMAKE --file=scripts/LLVM.lua clone_llvm
$PREMAKE --file=scripts/LLVM.lua build_llvm
$PREMAKE --file=scripts/LLVM.lua package_llvm
```

`$PREMAKE` should be replaced with `premake5.exe`, `premake5-osx` or `premake5-linux-64` depending on environment.

You can specify an `--arch=x86` or `--arch=x64` flag to the invocations above to specify an explicit build architecture.  

If the `clone_llvm` step fails, you can try to manually clone LLVM and Clang as explained below.
You should still run clone_llvm to ensure that you are on the correct revision.

## Cloning from Git

1. Clone LLVM to `<CppSharp>\deps\llvm`

```
git clone http://llvm.org/git/llvm.git
```

2. Clone Clang to `<CppSharp>\deps\llvm\tools\clang`

```
cd llvm/tools
git clone http://llvm.org/git/clang.git
```

Official LLVM instructions can be found here: [http://llvm.org/docs/GettingStarted.html#git-mirror]
(http://llvm.org/docs/GettingStarted.html#git-mirror)

Make sure to use the revisions specified below, or you will most likely get compilation errors.

Required LLVM/Clang commits:

[LLVM: see /build/LLVM-commit.](https://github.com/mono/CppSharp/tree/master/build/LLVM-commit)
[Clang: see /build/Clang-commit.](https://github.com/mono/CppSharp/tree/master/build/Clang-commit)

To change to the revisions specified above you can run the following commands:

```
git -C deps/llvm reset --hard <llvm-rev>
git -C deps/llvm/tools/clang reset --hard <clang-rev>
```



