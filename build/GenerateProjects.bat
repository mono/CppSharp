@echo off
goto menu

:menu
echo Build Project Generator:
echo.
echo [0] Clean
echo [1] Visual C++ 2010
echo [2] Visual C++ 2012
echo [3] Visual C++ 2012 (C++ parser)
echo [4] Visual C++ 2013
echo [5] GNU Make
echo.

:choice
set /P C="Choice: "
if "%C%"=="5" goto gmake
if "%C%"=="4" goto vs2013
if "%C%"=="3" goto vs2012_cpp
if "%C%"=="2" goto vs2012
if "%C%"=="1" goto vs2010
if "%C%"=="0" goto clean

:clean
"premake5" --file=premake4.lua clean
goto quit

:vs2010
"premake5" --file=premake4.lua vs2010
goto quit

:vs2012
"premake5" --file=premake4.lua vs2012
goto quit

:vs2012_cpp
"premake5" --file=premake4.lua --parser=cpp vs2012
goto quit

:vs2013
"premake5" --file=premake4.lua vs2013
goto quit

:gmake
"premake5" --file=premake4.lua gmake
goto quit

:quit
pause
:end