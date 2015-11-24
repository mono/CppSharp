rmdir /s /q out
mkdir out
mkdir out\tools\clang
mkdir out\tools\clang\lib\CodeGen
mkdir out\tools\clang\lib\Driver
mkdir out\build\
mkdir out\build\tools\clang\

set rbcopy=%systemroot%\System32\robocopy /NDL /NFL /NJH /NJS
set dircopy=%rbcopy% /E

%dircopy% include\ out\include
%dircopy% build\include\ out\build\include
%dircopy% build\RelWithDebInfo\lib out\build\RelWithDebInfo\lib

%dircopy% tools\clang\include\ out\tools\clang\include
copy /Y tools\clang\lib\CodeGen\*.h out\tools\clang\lib\CodeGen >nul
copy /Y tools\clang\lib\Driver\*.h out\tools\clang\lib\Driver >nul
%dircopy% build\tools\clang\include\ out\build\tools\clang\include

del out\build\RelWithDebInfo\lib\LLVM*ObjCARCOpts*.lib
del out\build\RelWithDebInfo\lib\clang*ARC*.lib
del out\build\RelWithDebInfo\lib\clang*Matchers*.lib
del out\build\RelWithDebInfo\lib\clang*Rewrite*.lib
del out\build\RelWithDebInfo\lib\clang*StaticAnalyzer*.lib
del out\build\RelWithDebInfo\lib\clang*Tooling*.lib
