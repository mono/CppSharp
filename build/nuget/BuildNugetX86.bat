@ECHO OFF
ECHO *** NuGet package builder
SET CPPSHARP_WGET_EXE=C:\Program Files (x86)\GnuWin32\bin\wget.exe
SET CPPSHARP_NUGET_EXE=nuget.exe
SET CPPSHARP_PKG_DIR=pkgdir
SET CPPSHARP_NUSPEC_SRC=cppsharp.nuspec
SET CPPSHARP_NUSPEC_TGT=%CPPSHARP_PKG_DIR%\cppsharp.nuspec
SET CPPSHARP_SCRIPTS=..\scripts
SET CPPSHARP_RELEASE_DIR=..\..\vs2017\lib\Release_x86

REM check for nuget, download it if necessary
IF EXIST "%CPPSHARP_NUGET_EXE%" (
	GOTO :nugetexists
)

IF NOT EXIST "%CPPSHARP_WGET_EXE%" (
	ECHO *** wget not found ... get it at http://gnuwin32.sourceforge.net/packages/wget.htm
	GOTO failed
) ELSE (
	ECHO *** nuget not found, downloading it ...
	"%CPPSHARP_WGET_EXE%" --no-check-certificate https://dist.nuget.org/win-x86-commandline/latest/nuget.exe
)

IF NOT EXIST "%CPPSHARP_NUGET_EXE%" (
	ECHO *** nuget download failed !
	GOTO failed
)

:nugetexists
ECHO *** packing started

REM create scratch directory
IF EXIST "%CPPSHARP_PKG_DIR%" RMDIR /S /Q "%CPPSHARP_PKG_DIR%"
MKDIR "%CPPSHARP_PKG_DIR%"
COPY "%CPPSHARP_NUSPEC_SRC%" "%CPPSHARP_NUSPEC_TGT%" > NUL

REM copy package content
PUSHD %CPPSHARP_PKG_DIR%
MKDIR lib
MKDIR output
MKDIR tools
XCOPY /Q "%CPPSHARP_RELEASE_DIR%\CppSharp.AST.dll" "lib"
XCOPY /Q "%CPPSHARP_RELEASE_DIR%\CppSharp.dll" "lib"
XCOPY /Q "%CPPSHARP_RELEASE_DIR%\CppSharp.Generator.dll" "lib"
XCOPY /Q "%CPPSHARP_RELEASE_DIR%\CppSharp.Parser.CLI.dll" "lib"
XCOPY /Q "%CPPSHARP_RELEASE_DIR%\CppSharp.Parser.dll" "lib"
XCOPY /Q "%CPPSHARP_RELEASE_DIR%\CppSharp.Runtime.dll" "lib"
XCOPY /Q "%CPPSHARP_RELEASE_DIR%\CppSharp.CLI.exe" "tools"
XCOPY /Q "%CPPSHARP_RELEASE_DIR%\CppSharp.CppParser.dll" "output"
XCOPY /Q /E /I "%CPPSHARP_RELEASE_DIR%\clang" "output\lib\clang"
POPD

REM pack (suppress warnings), cleanup
"%CPPSHARP_NUGET_EXE%" pack "%CPPSHARP_NUSPEC_TGT%" -NoPackageAnalysis
RMDIR /S /Q "%CPPSHARP_PKG_DIR%"

ECHO *** packing finished
GOTO quit

:failed
ECHO *** an error occurred, aborting ...
PAUSE

:quit
goto :eof
SET CPPSHARP_WGET_EXE=
SET CPPSHARP_NUGET_EXE=
SET CPPSHARP_PKG_DIR=
SET CPPSHARP_NUSPEC_SRC=
SET CPPSHARP_NUSPEC_TGT=
SET CPPSHARP_SCRIPTS=
SET CPPSHARP_RELEASE_DIR=
