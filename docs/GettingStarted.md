# Getting started

From an higher level overview, CppSharp will take a bunch of user-provided C/C++
headers and generate either C++/CLI or C# code that can be compiled into a
regular .NET assembly.

To get started you can either compile from source or get one of the pre-compiled
binary releases:

* [GitHub releases](https://github.com/mono/CppSharp/releases)
* [NuGet packages](https://www.nuget.org/packages/CppSharp/)

# Building from source

## Common setup

1. Clone CppSharp Git repository
2. Generate build files
3. Build the source code
4. Generate bindings

## Pre-requisites

LLVM is a core dependency of CppSharp.

By default, the `build.sh` command will automatically download a pre-built binary LLVM package
compatible with your system and version of CppSharp (the same ones we use for all our
continuous integration (CI) builds).

Or you can choose to build LLVM and Clang from source if you prefer,
please check the [LLVM](LLVM.md) documentation page for more information.

The build scripts also depend on `curl` command and 7-Zip on Windows, so please
make sure those are installed on your system.

## Compiling on Windows/Visual Studio

1. Generate the VS solution

    ```shell
    cd <CppSharp>\build
    ./build.sh generate -configuration Release -platform x64
    ```

2. Compile the VS projects

    You can open `CppSharp.sln` and hit F5 or compile via the command line:

    ```
    ./build.sh -configuration Release -platform x64
    ```

Building in *Release* is recommended because else we will use the Clang parser
debug configuration, which will be too slow for practical use beyond debugging.

## Compiling on macOS or Linux

1. Generate the VS solution and makefiles 

    ```shell
    cd <CppSharp>\build
    ./build.sh generate -configuration Release -platform x64
    ```

2. Compile the csproj files and makefiles

    ```
    ./build.sh -configuration Release -platform x64
    ```

If the above script fails, you can try these equivalent manual steps:

1. Build the generated makefiles:

    ```
    make -C gmake config=release_x64
    ```

2. Build the generated VS solution:

    ```
    msbuild CppSharp.sln -p:Configuration=Release -p:Platform=x64
    ```

If you need more verbosity from the builds invoke `make` and `msbuild` as:

```shell
make -C gmake config=release_x64 verbose=true
msbuild CppSharp.sln -p:Configuration=Release -p:Platform=x64 -verbosity:detailed
```

## Running the testsuite

1. Change directory to `<CppSharp>\build`
2. Run `./test.sh` to run the tests.

## Linux notes

Only 64-bits builds are supported. 

# Generating bindings

You can now progress to generating your first bindings, explained in our [Generating bindings](GeneratingBindings.md) page.
