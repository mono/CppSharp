# Getting started

From an higher level overview, CppSharp will take a bunch of user-provided C/C++
headers and generate either C++/CLI or C# code that can be compiled into a
regular .NET assembly.

To get started you can either compile from source or get one of the pre-compiled
binary releases (only provided for Windows, at the moment):

* [GitHub releases](https://github.com/mono/CppSharp/releases)
* [NuGet packages](https://www.nuget.org/packages/CppSharp/)

# Building from source

## Common setup

1. Clone CppSharp Git repository
2. Setup LLVM and Clang dependencies
3. Generate build files using Premake
4. Build the source code
5. Generate bindings

## Setting up LLVM and Clang dependencies

You can either build LLVM and Clang from source or download one of our pre-built binary 
dependency packages (the same ones we use for all our continuous integration (CI) builds).

### Downloading the LLVM and Clang packages

The dependencies can be automatically downloaded by running:

```shell
cd <CppSharp>\build
premake5 --file=scripts/LLVM.lua download_llvm # on Windows
premake5-osx --file=scripts/LLVM.lua download_llvm # on OSX
premake5-linux-64 --file=scripts/LLVM.lua download_llvm # on Linux
```

Alternatively, if on Windows, just run `<CppSharp>/build/DownloadDeps.bat` from a Visual Studio command prompt
corresponding to the VS version you want to use.

After this, you should end up with one or multiple `<CppSharp>/build/scripts/llvm-<revision>-<os>-<configuration>` folders
containing the headers and libraries for LLVM.

If you do not end up with the folder, which can happen due to, for instance, not having 7-Zip on the path on Windows,
then you can manually extract the .7z archives in `<CppSharp>/build/scripts` to their respective folders.

### Building LLVM and Clang from source

Please check the guide in [Compiling LLVM and Clang from source](BuildingLLVM.md)

## Compiling on Windows/Visual Studio

1. Generate the VS solution and project files 

```shell
cd <CppSharp>\build
GenerateProjects.bat
```

2. Compile the project

You can open `CppSharp.sln` and hit F5 or compile via the command line:

```
msbuild vs2017\CppSharp.sln /p:Configuration=Release;Platform=x86
```

Building in *Release* is recommended because else we will use the Clang parser
debug configuration, which will be too slow for practical use beyond debugging.

## Compiling on macOS or Linux

1. Change directory to `<CppSharp>\build`
2. Run `./Compile.sh` to generate the project files and compile the code.

If the above script fails, you can try these equivalent manual steps:

1. Generate the Makefiles

```
./premake5-osx gmake # if on OSX
./premake5-linux-64 gmake # if on Linux
```

2. Build the generated makefiles:
    - 32-bit builds: `make -C gmake config=release_x86`
    - 64-bit builds: `make -C gmake config=release_x64`

The version you compile needs to match the version of the Mono VM installed on your 
system which you can find by running `mono --version`. The reason for this is because
a 32-bit VM will only be able to load 32-bit shared libraries and vice-versa for 64-bits.

If you need more verbosity from the builds invoke `make` as:

```shell
make -C gmake config=release_x64 verbose=true
```

## Running the testsuite

1. Change directory to `<CppSharp>\build`
2. Run `./InstallNugets.sh` to install the NUnit test runner from Nuget.
3. Run `./RunTests.sh` to run the tests.

## Linux notes

Only 64-bits builds are supported. 

We depend on a recent version of Mono.

Please look into the [download page](http://www.mono-project.com/download/#download-lin) on the
Mono website for official install instructions.

# Generating bindings

You can now progress to generating your first bindings, explained in our [Generating bindings](GeneratingBindings.md) page.

