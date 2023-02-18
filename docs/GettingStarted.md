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

The following steps should be called from the VS developer command prompt.

1. Generate the VS solution

    ```shell
    cd <CppSharp>\build
    <sh> build.sh generate -configuration Release -platform x64
    ```

> :information_source: You can use the `-target-framework` option to target any valid .NET target framework.

2. Compile the VS projects

    You can open `CppSharp.sln` and hit F5 or compile via the command line:

    ```
    <sh> build.sh -configuration Release -platform x64
    ```

Building in *Release* is recommended because else we will use the Clang parser
debug configuration, which will be too slow for practical use beyond debugging.

The solution generated will be for Visual Studio 2019.

If you have a more recent version of Visual Studio, you can either:
- install Visual Studio 2019 
- install Visual Studio 2019 build tools, from [Visual Studio Installer](https://visualstudio.microsoft.com/downloads/)
  - select the payload *MSVC v142 - VS 2019 C++ x64/x86 build tools (v14.29-16.11)*

Please note that Windows isn't natively able to run sh scripts, to remediate this, you can either:

- use the one from Visual Studio if present
  - e.g. `C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\usr\bin\sh.exe`
- use the one from [Git for Windows](https://gitforwindows.org/)
  - e.g. `C:\Program Files\Git\bin\sh.exe`

When opening the solution for the first time on a more recent version than Visual Studio 2019, you will be prompted to retarget projects to one of the platform toolset available on your system.

## Compiling on macOS or Linux


1. Generate the VS solution and makefiles 


    ```shell
    cd <CppSharp>\build
    ./build.sh generate -configuration Release -platform x64
    ```

> :information_source: You can use the `-target-framework` option to target any valid .NET target framework.

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
