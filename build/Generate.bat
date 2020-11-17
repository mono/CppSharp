@echo off
goto menu

:menu
echo Build Project Generator:
echo.
echo [0] Clean
echo [1] Visual C++ 2019
echo [2] Visual C++ 2017
echo [3] GNU Make

echo.

:choice
set /P C="Choice: "
if "%C%"=="0" goto clean
if "%C%"=="1" goto vs2019
if "%C%"=="2" goto vs2017
if "%C%"=="3" goto gmake

:clean
"premake5" --file=premake5.lua clean
goto quit

:vs2019
"premake5" --file=premake5.lua vs2019
goto quit

:vs2017
"premake5" --file=premake5.lua vs2017 
goto quit

:gmake
"premake5" --file=premake5.lua gmake
goto quit

:quit
pause
:end