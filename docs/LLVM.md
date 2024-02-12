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
./build.sh clone_llvm
./build.sh build_llvm
./build.sh package_llvm
```

You can specify an `--platform=x86` or `--platform=x64` flag to the invocations above to specify an explicit build architecture.  

If the `clone_llvm` step fails, you can try to manually clone LLVM and Clang as explained below.
You should still run clone_llvm to ensure that you are on the correct revision.

If using Visual Studio / MSVC, be sure to run the scripts from a [VS Command Prompt](https://docs.microsoft.com/en-us/dotnet/framework/tools/developer-command-prompt-for-vs).

## Cloning from Git

1. Clone LLVM to `<CppSharp>\build\llvm\llvm`

```
git clone http://llvm.org/git/llvm.git
```

2. Clone Clang to `<CppSharp>\build\llvm\llvm\tools\clang`

```
cd llvm/tools
git clone http://llvm.org/git/clang.git
```

Official LLVM instructions can be found here: [http://llvm.org/docs/GettingStarted.html#git-mirror](http://llvm.org/docs/GettingStarted.html#git-mirror)

Make sure to use the revisions specified below, or you will most likely get compilation errors.

Required LLVM/Clang commits:

[LLVM: see /build/llvm/LLVM-commit.](https://github.com/mono/CppSharp/tree/master/build/llvm/LLVM-commit)

To change to the revisions specified above you can run the following commands:

```
git -C deps/llvm reset --hard <llvm-rev>
git -C deps/llvm/tools/clang reset --hard <clang-rev>
```

# Downloading the LLVM and Clang packages manually

The dependencies can be automatically downloaded by running:

```
cd <CppSharp>\build
./build.sh download_llvm
```

After this, you should end up with one or multiple <CppSharp>/build/llvm/llvm-<revision>-<os>-<configuration> folders
containing the headers and libraries for LLVM.

If you do not end up with the folder, which can happen due to, for instance, not having 7-Zip on the path on Windows,
then you can manually extract the .7z archives in <CppSharp>/build/llvm to their respective folders.
