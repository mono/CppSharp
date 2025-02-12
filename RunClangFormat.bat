@echo off
setlocal enabledelayedexpansion

echo Looking for clang-format...

set "FOLDERS=src examples tests"
set "ALLOWED_EXTENSIONS=*.cpp *.hpp *.c *.h *.inl"

set CLANG_FORMAT_PATH=VC\Tools\Llvm\bin\clang-format.exe
set VSWHERE_PATH="%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"

set CLANG_FORMAT_OPTIONS=--fallback-style=none -i

: =================================================================================
: Check vswhere
if not exist %VSWHERE_PATH% (
    echo [91mCould not locate vshere.exe at: %VSWHERE_PATH%
    pause
    exit
)

: Find visual studio installation path
for /f "usebackq tokens=*" %%i in (`call %VSWHERE_PATH% -latest -products * -property installationPath`) do (
  set VS_INSTALL_DIR=%%i
)

: Find visual studio installation path
if not exist "%VS_INSTALL_DIR%" (
    echo [91mVisual Studio Installation directory not found. Tried looking at: %VS_INSTALL_DIR%
    pause
    exit
)

set CLANG_FORMAT_ABSOLUTE_PATH="%VS_INSTALL_DIR%\%CLANG_FORMAT_PATH%"

: Verify clang-format actually exists as well
if not exist %CLANG_FORMAT_ABSOLUTE_PATH% (
    echo [91mclang-format.exe could not be located at: %CLANG_FORMAT_ABSOLUTE_PATH%
    pause
    exit
)
: =================================================================================

echo Found clang-format.exe at: %CLANG_FORMAT_ABSOLUTE_PATH%
: Display clang-format version
%CLANG_FORMAT_ABSOLUTE_PATH% --version

set DRY_RUN=0
: set /p DRY_RUN="Dry run? (1/0): "

if %DRY_RUN%==1 (
    set CLANG_FORMAT_OPTIONS=%CLANG_FORMAT_OPTIONS% --dry-run --Werror
)

: Format all files, in the specified list of directories
: Loop through each file in the directory (and subdirectories)
for %%p in (%FOLDERS%) do (
    echo Formatting files in folder "%%p"...
    cd %%p

    for /r %%f in (%ALLOWED_EXTENSIONS%) do (
        : Skip certain folders
        call :shouldSkipFile %%f

        if not errorlevel 1 (
            %CLANG_FORMAT_ABSOLUTE_PATH% %CLANG_FORMAT_OPTIONS% %%f
        ) 
        :: else (
        ::     echo Skipping file %%f...
        :: )
    )

    cd ..
)

echo Done!
pause
exit

:shouldSkipFile
: Skip auto generated files
call :findInString %1 Bindings\
if errorlevel 1 (
    exit /B 1
)

call :findInString %1 CppParser\Expr
if errorlevel 1 (
    exit /B 1
)
call :findInString %1 CppParser\ParseExpr
if errorlevel 1 (
    exit /B 1
)
call :findInString %1 CppParser\Stmt
if errorlevel 1 (
    exit /B 1
)
call :findInString %1 CppParser\ParseStmt
if errorlevel 1 (
    exit /B 1
)

: Skip CLI files (clang-format doesn't support them properly)
call :findInString %1 CLI\
if errorlevel 1 (
    exit /B 1
)

exit /B 0

:findInString
set searchString=%1
set key=%2

call set keyRemoved=%%searchString:%key%=%%

if not "x%keyRemoved%"=="x%searchString%" (
    exit /B 1
) else (
    exit /B 0
)
