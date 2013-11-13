# Getting started

From an higher level overview, CppSharp will take a bunch of user-provided C/C++
headers and generate either C++/CLI or C# code that can be compiled into a
regular .NET assembly.

Since there are no binary releases yet, the project needs to be compiled from
source first.

## Compiling on Windows/Visual Studio

1. Clone CppSharp to `<CppSharp>`
2. Clone LLVM to `<CppSharp>\deps\llvm`
3. Clone Clang to `<CppSharp>\deps\llvm\tools\clang` (see:
   [http://clang.llvm.org/get_started.html](http://clang.llvm.org/get_started.html))
4. Run CMake in `<CppSharp>\deps\llvm` and compile solution in *RelWithDebInfo* mode
5. Run `GenerateProjects.bat` in <CppSharp>\build
6. Build generated solution in *Release*.

Building in *Release* is recommended because else the Clang parser will be
excruciatingly slow.

Last updated to LLVM/Clang revision: `r194603`

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
	void SetupPasses(Driver driver, PassBuilder passes);

	/// Do transformations that should happen before passes are processed.
	void Preprocess(Driver driver, Library lib);

	/// Do transformations that should happen after passes are processed.
	void Postprocess(Driver driver, Library lib);
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
    options.GeneratorKind = LanguageGeneratorKind.CSharp;
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

2. `void SetupPasses(Driver driver, PassBuilder passes)`

New passes are added to the generator by using the API provided by `PassBuilder`.

```csharp
void SetupPasses(Driver driver, PassBuilder passes)
{
	passes.RenameDeclsUpperCase(RenameTargets.Any);
	passes.FunctionToInstanceMethod();
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

3. `void Preprocess(Driver driver, Library lib);`
4. `void Postprocess(Driver driver, Library lib);`

As their comments suggest, these get called either before or after the the
passes we setup earlier are run and they allow you free reign to manipulate
the declarations before the output generator starts processing them.

Let's say we want to change the class to provide .NET value semantics,
drop one field from the generated bindings and rename the `FooAdd` function.
    
```csharp            
void Postprocess(Driver driver, Library lib)
{
  	lib.SetClassAsValueType("Foo");
  	lib.SetNameOfFunction("FooAdd", "FooCalc");
  	lib.IgnoreClassField("Foo", "b");
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