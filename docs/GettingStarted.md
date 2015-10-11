# Getting started

From an higher level overview, CppSharp will take a bunch of user-provided C/C++
headers and generate either C++/CLI or C# code that can be compiled into a
regular .NET assembly.

To get started you can either compile from source or get one of the pre-compiled binary releases from the [releases archive](https://github.com/mono/CppSharp/releases).

# Building from source

## LLVM/Clang source repositories ##

SVN repository urls found here: [http://clang.llvm.org/get_started.html](http://clang.llvm.org/get_started.html)

Git repository urls found here: [http://llvm.org/docs/GettingStarted.html#git-mirror](http://llvm.org/docs/GettingStarted.html#git-mirror)

## Common setup

1. Clone CppSharp to `<CppSharp>`
2. Clone LLVM to `<CppSharp>\deps\llvm`
3. Clone Clang to `<CppSharp>\deps\llvm\tools\clang`
4. Create directory `<CppSharp>\deps\llvm\build`

Required LLVM/Clang commits:

[LLVM: see /build/LLVM-commit.](https://github.com/mono/CppSharp/tree/master/build/LLVM-commit)

[Clang: see /build/Clang-commit.](https://github.com/mono/CppSharp/tree/master/build/Clang-commit)

## Compiling on Windows/Visual Studio

### Compiling LLVM on Windows/Visual Studio (32-bit binaries)

```shell
cd <CppSharp>\deps\llvm\build

cmake -G "Visual Studio 12" -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false -DCLANG_INCLUDE_DOCS=false -DCLANG_BUILD_EXAMPLES=false -DLLVM_TARGETS_TO_BUILD="X86" -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false ..

msbuild LLVM.sln /p:Configuration=RelWithDebInfo;Platform=Win32 /m
```

OR, if you need 64-bit binaries:

```shell
cd <CppSharp>\deps\llvm\build

cmake -G "Visual Studio 12 Win64" -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false -DCLANG_INCLUDE_DOCS=false -DCLANG_BUILD_EXAMPLES=false -DLLVM_TARGETS_TO_BUILD="X86" -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false ..

msbuild LLVM.sln /p:Configuration=RelWithDebInfo;Platform=x64 /m
```

### Compiling CppSharp on Windows/Visual Studio

```shell
cd <CppSharp>\build

generateprojects.bat
msbuild vs2013\CppSharp.sln /p:Configuration=Release;Platform=x86
```

Building in *Release* is recommended because else the Clang parser will be
excruciatingly slow.

It has been reported that running the solution upgrade process under VS 2013 breaks the build due
to an incompatibility of .NET versions between projects (4.5 and 4.0). If you experience this
problem you can change the targetted .NET version of the projects to be the same or just do not
run the upgrade process after generation. 

## Compiling on Mac OS X

### Compiling LLVM on Mac OS X

1. Compile LLVM solution in *RelWithDebInfo* mode
   The following CMake variables should be enabled:
    - LLVM_ENABLE_LIBCXX (enables libc++ standard library support)
    - LLVM_BUILD_32_BITS for 32-bit builds (defaults to 64-bit)

```shell
mkdir -p deps/llvm/build && cd deps/llvm/build

cmake -G "Unix Makefiles" -DLLVM_ENABLE_LIBCXX=true -DLLVM_BUILD_32_BITS=true -DCMAKE_BUILD_TYPE=RelWithDebInfo ..

make
```

### Compiling CppSharp on Mac OS X

1. Run `premake5-osx gmake` in <CppSharp>\build
2. Build generated makefiles:
    - 32-bit builds: `config=release_x32 make`
    - 64-bit builds: `config=release_x64 make`

The version you compile needs to match the version of the Mono VM installed on your system which you can find by running `mono --version`. The reason for this is because a 32-bit VM will only be able to load 32-bit shared libraries and vice-versa for 64-bits.

## Compiling on Linux

Only 64-bits builds are supported at the moment. 

### Compiling LLVM on Linux

If you do not have native build tools you can install them first with:

```shell
sudo apt-get install cmake ninja-build build-essential
```


```shell
cd deps/llvm/build

cmake -G Ninja -DCLANG_BUILD_EXAMPLES=false -DCLANG_INCLUDE_DOCS=false -DCLANG_INCLUDE_TESTS=false -DCLANG_INCLUDE_DOCS=false -DCLANG_BUILD_EXAMPLES=false -DLLVM_TARGETS_TO_BUILD="X86" -DLLVM_INCLUDE_EXAMPLES=false -DLLVM_INCLUDE_DOCS=false -DLLVM_INCLUDE_TESTS=false ..

ninja
```

### Compiling CppSharp on Linux

We depend on a somewhat recent version of Mono (.NET 4.5). Ubuntu 14.04 contains recent enough Mono by default.

```shell
sudo apt-get install mono-devel
```

If you are using another distribution then please look into the [download page](http://www.mono-project.com/download/#download-lin) on the Mono website.

Generate the makefiles, and build CppSharp:

```shell
cd <CppSharp>/build
./premake5-linux gmake
make -C gmake config=release_x64
```

If you need more verbosity from the builds invoke `make` as:

```shell
verbose=true make -C gmake config=release_x64
```

Additionally, you may want to run a very simple test to see that it works. The test needs to find CppSharp library, so cppsharp-test needs to be clone in to the same directory where you cloned CppSharp. Also, the CppSharp directory needs to be named "cppsharp".

```shell
git clone git://github.com/tomba/cppsharp-test.git
cd cppsharp-test
make runtest
```

## Generating bindings

Suppose we have the following declarations in a file named `Sample.h` and we
want to bind it to .NET.

```csharp
class Foo
{
public:

	int a;
	float b;
};

int FooAdd(Foo* foo);
```

The easiest way to get started with CppSharp is to create a new class and
implement the `ILibrary` interface.

Each implemented method will be called by the generator during different
parts of the binding process.

```csharp
public interface ILibrary
{
	/// Setup the driver options here.
	void Setup(Driver driver);

	/// Setup your passes here.
	void SetupPasses(Driver driver);

	/// Do transformations that should happen before passes are processed.
	void Preprocess(Driver driver, ASTContext ctx);

	/// Do transformations that should happen after passes are processed.
	void Postprocess(Driver driver, ASTContext ctx);
}
```

Then you just need to call the `ConsoleDriver.Run` static method with your
an instance of your class to start the generation process. Like this:

```csharp
ConsoleDriver.Run(new SampleLibrary());
```

Now let's drill through each of the interface methods in more detail:

1. `void Setup(Driver driver)`

This is the first method called and here you should setup all the options needed
for Clang to correctly parse your code. You can get at the options through a
property in driver object. The essential ones are:

**Parsing**

- Defines
- Include directories
- Headers
- Libraries
- Library directories

**Generator**

- Output language (C# or C++/CLI)
- Output namespace
- Output directory


Here's how the setup method could be implemented for binding the sample code
above:

```csharp
void Setup(Driver driver)
{
    var options = driver.Options;
    options.GeneratorKind = GeneratorKind.CSharp;
    options.LibraryName = "Sample";
    options.Headers.Add("Sample.h");
    options.Libraries.Add("Sample.lib");
}
```

Pretty simple! We just need to tell the name of our library, what headers to
process and the path to the compiled library containing the exported symbols
of the declarations. And of course, what kind of output language we want.

This is enough to get the generator outputting some bindings:

```csharp
public unsafe partial class Foo : IDisposable
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Internal
    {
        [FieldOffset(0)]
        public int a;

        [FieldOffset(4)]
        public float b;

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Sample.Native", CallingConvention = CallingConvention.ThisCall,
            EntryPoint="??0Foo@@QAE@XZ")]
        public static extern System.IntPtr Foo0(System.IntPtr instance);
    }

    public System.IntPtr _Instance { get; protected set; }

    internal Foo(Foo.Internal* native)
        : this(new System.IntPtr(native))
    {
    }

    internal Foo(Foo.Internal native)
        : this(&native)
    {
    }

    internal Foo(System.IntPtr native)
    {
        _Instance = native;
    }

    public Foo()
    {
        _Instance = Marshal.AllocHGlobal(8);
        Internal.Foo0(_Instance);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Marshal.FreeHGlobal(_Instance);
    }

    public int a
    {
        get
        {
            var _ptr = (Internal*)_Instance.ToPointer();
            return _ptr->a;
        }

        set
        {
            var _ptr = (Internal*)_Instance.ToPointer();
            _ptr->a = value;
        }
    }

    public float b
    {
        get
        {
            var _ptr = (Internal*)_Instance.ToPointer();
            return _ptr->b;
        }

        set
        {
            var _ptr = (Internal*)_Instance.ToPointer();
            _ptr->b = value;
        }
    }
}

public partial class SampleSample
{
    public struct Internal
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport("Sample.Native", CallingConvention = CallingConvention.Cdecl,
            EntryPoint="?FooAdd@@YAHPAVFoo@@@Z")]
        public static extern int FooAdd0(System.IntPtr foo);
    }

    public static int FooAdd(Foo foo)
    {
        var arg0 = foo._Instance;
        var ret = Internal.FooAdd0(arg0);
        return ret;
    }
}
```

The generator creates one managed class corresponding to each native class.
It also creates one class per file to hold all the free functions in the
native code, since free functions are not supported under C#. By default,
it will use the library name followed by the file name, hence `SampleSample`.

Each bound class will have an `Internal` nested type which is used for interop
with the native code. It holds the native P/Invoke declarations and has the
same size as the native class, as it will be used to pass an instance of the
object whenever value-type semantics are expected in the native code.

Alright, now that we know how to generate barebones bindings, let's see how
we can clean them up.

## Cleaning up after you

CppSharp provides plenty of support for customization of the generated code.

A couple of things could be improved in the generated code above:

- Property names do not follow the .NET naming convention
- `FooAdd` function could be an instance method instead of a static method

The main mechanism provided to customize the code are passes.

A pass is a simple tree [visitor](https://en.wikipedia.org/wiki/Visitor_pattern)
whose methods get called for each declaration that was parsed from the headers
(or translation units).

CppSharp already comes with a collection of useful built-in passes and we will
now see how to use them to fix the flaws enumerated above. 

2. `void SetupPasses(Driver driver)`

New passes are added to the generator by using the API provided by `PassBuilder`.

```csharp
void SetupPasses(Driver driver)
{
	driver.TranslationUnitPasses.RenameDeclsUpperCase(RenameTargets.Any);
	driver.TranslationUnitPasses.AddPass(new FunctionToInstanceMethodPass());
}
```

Re-generate the bindings and voila:

```csharp
public unsafe partial class Foo : IDisposable
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Internal
    {
        [FieldOffset(0)]
        public int a;

        [FieldOffset(4)]
        public float b;

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Sample.Native", CallingConvention = CallingConvention.ThisCall,
            EntryPoint="??0Foo@@QAE@XZ")]
        public static extern System.IntPtr Foo0(System.IntPtr instance);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Sample.Native", CallingConvention = CallingConvention.Cdecl,
            EntryPoint="?FooAdd@@YAHPAVFoo@@@Z")]
        public static extern int Add0(System.IntPtr instance);
    }

    public System.IntPtr _Instance { get; protected set; }

    internal Foo(Foo.Internal* native)
        : this(new System.IntPtr(native))
    {
    }

    internal Foo(Foo.Internal native)
        : this(&native)
    {
    }

    internal Foo(System.IntPtr native)
    {
        _Instance = native;
    }

    public Foo()
    {
        _Instance = Marshal.AllocHGlobal(8);
        Internal.Foo0(_Instance);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        Marshal.FreeHGlobal(_Instance);
    }

    public int A
    {
        get
        {
            var _ptr = (Internal*)_Instance.ToPointer();
            return _ptr->a;
        }

        set
        {
            var _ptr = (Internal*)_Instance.ToPointer();
            _ptr->a = value;
        }
    }

    public float B
    {
        get
        {
            var _ptr = (Internal*)_Instance.ToPointer();
            return _ptr->b;
        }

        set
        {
            var _ptr = (Internal*)_Instance.ToPointer();
            _ptr->b = value;
        }
    }

    public int Add()
    {
        var ret = Internal.Add0(_Instance);
        return ret;
    }
}
```

## Custom processing

Now that the bindings are looking good from a .NET perspective, let's see how
we can achieve more advanced things by using the remaining overloads in the
interface.

3. `void Preprocess(Driver driver, ASTContext ctx);`
4. `void Postprocess(Driver driver, ASTContext ctx);`

As their comments suggest, these get called either before or after the the
passes we setup earlier are run and they allow you free reign to manipulate
the declarations before the output generator starts processing them.

Let's say we want to change the class to provide .NET value semantics,
drop one field from the generated bindings and rename the `FooAdd` function.
    
```csharp            
void Postprocess(Driver driver, ASTContext ctx)
{
  	ctx.SetClassAsValueType("Foo");
  	ctx.SetNameOfFunction("FooAdd", "FooCalc");
  	ctx.IgnoreClassField("Foo", "b");
}            
```

Re-generate the bindings and this is what we get:

```csharp
public unsafe partial struct Foo
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct Internal
    {
        [FieldOffset(0)]
        public int a;

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Sample.Native", CallingConvention = CallingConvention.ThisCall,
            EntryPoint="??0Foo@@QAE@XZ")]
        public static extern System.IntPtr Foo0(System.IntPtr instance);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("Sample.Native", CallingConvention = CallingConvention.Cdecl,
            EntryPoint="?FooAdd@@YAHPAVFoo@@@Z")]
        public static extern int Calc0(System.IntPtr instance);
    }

    internal Foo(Foo.Internal* native)
        : this(new System.IntPtr(native))
    {
    }

    internal Foo(Foo.Internal native)
        : this(&native)
    {
    }

    internal Foo(System.IntPtr native)
    {
        var _ptr = (Internal*)native.ToPointer();
        A = _ptr->a;
    }

    internal Internal ToInternal()
    {
        var _native = new Foo.Internal();
        _native.a = A;
        return _native;
    }

    internal void FromInternal(Internal* native)
    {
        var _ptr = native;
        A = _ptr->a;
    }

    public int A;

    public int Calc()
    {
        var _instance = ToInternal();
        var ret = Internal.Calc0(new System.IntPtr(&_instance));
        FromInternal(&_instance);
        return ret;
    }
}
```

This is just a very small example of what is available. Since the entire AST
is accessible, pretty much every customization one might need is possible.

The methods we called are in fact just regular .NET extension methods
provided by the library to be used as helpers for common operations.

## Where to go from here

Hopefully now you have a better idea of how the generator works and how to
setup simple customizations to get the outputs better mapped to .NET. 

This barely touched the surface of what can be done with CppSharp, so please
check out the user and developer reference manuals for more information. 
