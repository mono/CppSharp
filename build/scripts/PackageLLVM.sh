cd ../../deps/llvm

if [ -d "out" ]; then rm -rf out; fi
mkdir out
mkdir -p out/lib/clang
mkdir -p out/tools/clang
mkdir -p out/tools/clang/lib/CodeGen
mkdir -p out/tools/clang/lib/Driver
mkdir -p out/build/
mkdir -p out/build/lib
mkdir -p out/build/tools/clang
mkdir -p out/build/tools/clang/lib

cp -R include/ out/
cp -R build/include/ out/build
cp build/lib/*.a out/build/lib

cp -R tools/clang/include/ out/tools/clang
cp -R tools/clang/lib/CodeGen/*.h out/tools/clang/lib/CodeGen
cp -R tools/clang/lib/Driver/*.h out/tools/clang/lib/Driver
cp -R build/lib/clang out/lib/clang
cp -R build/tools/clang/include/ out/build/tools/clang

rm out/build/lib/libllvm*ObjCARCOpts*.a
rm out/build/lib/libclang*ARC*.a
rm out/build/lib/libclang*Matchers*.a
rm out/build/lib/libclang*Rewrite*.a
rm out/build/lib/libclang*StaticAnalyzer*.a
rm out/build/lib/libclang*Tooling*.a

7z a llvm_linux_x86_64.7z ./out/* 