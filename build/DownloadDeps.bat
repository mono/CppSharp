@echo off
goto menu

:menu
echo Download Dependencies:
echo.
echo [1] Download All Dependencies
echo [2] Download NuGet Dependencies
echo [3] Git Clone LLVM / Clang
echo [0] Cancel
echo.

:choice
set /P C="Choice: "
if "%C%"=="1" call :depend_all
if "%C%"=="2" call :depend_nuget
if "%C%"=="3" call :depend_gitclone
if "%C%"=="0" goto quit
goto quit

:depend_all
call :depend_nuget
call :depend_gitclone
goto :EOF

:depend_nuget
premake5 --file=premake4.lua depend_nuget
goto :EOF

:depend_gitclone
premake5 --file=premake4.lua depend_gitclone
goto :EOF

:quit
pause
:end