Building cxxi project on Windows using Visual Studio 2010
---------------------------------------------------------

Prerequisites:
--------------

- Install cmake
    http://www.cmake.org/
    I have 2.8.8 version

- Install Tortoise Git
    http://code.google.com/p/tortoisegit/
    I've installed it into "C:\Program Files (x86)"

- Install Tortoise SVN
    I've installed with command line toolset.

- Install Python 2.x
    http://www.python.org/
    I have 2.72
    
    
Building:
---------

1. LLVM & clang
---------------

> mkdir cppreflect & chdir cppreflect
> svn co http://llvm.org/svn/llvm-project/llvm/trunk llvm

( I've used revision: -r 172262 )

(Referred instructions 
http://llvm.org/docs/GettingStarted.html#checkout )

> cd llvm/tools
> svn co http://llvm.org/svn/llvm-project/cfe/trunk clang
> cd ../..

(I've used revision: -r 172262)

(Referred instructions:
http://clang.llvm.org/get_started.html )

Don't close console yet - and - 
Press (Windows)+R - Start cmake-gui.
- Browse soure code - pinpoint to newly added path.

(I for example had: E:/projects_prototype/cppreflect/llvm )

In where to build binaries - select llvm path with /build - like 
for me: E:/projects_prototype/cppreflect/llvm/build

Press configure - select "Visual Studio 10" - Ok.

Wait for configure to complete and press "Generate".

Go with explorer into build folder and open LLVM.sln with Visual studio.

Right click on 'clang' project and build it.

For development purposes - it's recommended to use 'Debug' / Win32 
because release builds have optimizations enabled - re-compiling
same source code will be slow.
If you're not planning to develop cxxi further - 'Release' is your 
configuration.

It's not recommended to build everything - since it will consume more
your time - runs tests and do everything you don't want it to do.

2. cxxi
---------------

In console window type:

> set PATH=C:\Program Files (x86)\Git\bin;%PATH%
> git clone https://github.com/tapika/cxxi-1.git cxxi_trunk
> cd cxxi_trunk\build
> premake4.exe vs2010


Use explorer and go to into the same folder cxxi_trunk\build -
and open Cxxi.sln from there.

Compile code.


Compiled binaries will be located in 

   cxxi_trunk\bin


Have Fun.
