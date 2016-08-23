# Getting started

From an higher level overview, CppSharp will take a bunch of user-provided C/C++
headers and generate either C++/CLI or C# code that can be compiled into a
regular .NET assembly.

To get started you can either compile from source or get one of the pre-compiled binary
releases from the [releases archive](https://github.com/mono/CppSharp/releases).

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

Alternatively, if on Windows, just double click on `<CppSharp>/build/DownloadDeps.bat`.

After this, you should end up with one or multiple `<CppSharp>/build/scripts/llvm-<revision>-<os>-<configuration>` folders
containing the headers and libraries for LLVM.

If you do not end up with the folder, which can happen due to, for instance, not having 7-Zip on the path on Windows,
then you can manually extract the .7z archives in `<CppSharp>/build/scripts` to their respective folders.

### Building LLVM and Clang from source

Please check the guide in [Compiling LLVM and Clang from source](BuildingLLVM.md)

## Compiling on Windows/Visual Studio

```shell
cd <CppSharp>\build

GenerateProjects.bat
msbuild vs2013\CppSharp.sln /p:Configuration=Release;Platform=x86
```

Building in *Release* is recommended because else the Clang parser will be
excruciatingly slow.

It has been reported that running the solution upgrade process under VS 2013 breaks the build due
to an incompatibility of .NET versions between projects (4.5 and 4.0). If you experience this
problem you can change the targetted .NET version of the projects to be the same or just do not
run the upgrade process after generation. 

## Compiling on Mac OS X

1. Run `./premake5-osx gmake` in `<CppSharp>\build`
2. Build the generated makefiles:
    - 32-bit builds: `config=release_x32 make -C gmake`
    - 64-bit builds: `config=release_x64 make -C gmake`

The version you compile needs to match the version of the Mono VM installed on your 
system which you can find by running `mono --version`. The reason for this is because
a 32-bit VM will only be able to load 32-bit shared libraries and vice-versa for 64-bits.

## Compiling on Linux

Only 64-bits builds are supported at the moment. 

We depend on a somewhat recent version of Mono (.NET 4.5).
Ubuntu 14.04 contains recent enough Mono by default, which you can install with:

```shell
sudo apt-get install mono-devel
```

If you are using another distribution then please look into the [download page](http://www.mono-project.com/download/#download-lin) on the Mono website.

Generate the makefiles, and build CppSharp:

```shell
cd <CppSharp>/build
./premake5-linux-64 gmake
make -C gmake config=release_x64
```

If you need more verbosity from the builds invoke `make` as:

```shell
verbose=true make -C gmake config=release_x64
```

If you get the following error, please see [issue #625](https://github.com/mono/CppSharp/issues/625#issuecomment-189283549):

```
/usr/include/wchar.h(39,11): fatal: 'stdarg.h' file not found CppSharp has encountered an error while parsing code.
```

# Generating bindings

You can now progress to generating your first bindings, explained in our [Generating bindings](GeneratingBindings.md) page.

