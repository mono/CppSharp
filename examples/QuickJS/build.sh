set -ex
mono ../../build/vs2015/lib/Debug_x64/CppSharp.CLI.exe -gen=qjs -module=test -prefix=js_ test-native.h
../../build/premake5-osx --file=premake5.lua gmake
make clean
make

cp gen/bin/release/libtest.dylib .
../../deps/QuickJS/qjs test.js