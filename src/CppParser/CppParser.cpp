/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#include "CppParser.h"
#include "Parser.h"
#include <llvm/Support/Host.h>
#include <clang/Basic/Version.inc>

namespace CppSharp { namespace CppParser {

CppParserOptions::CppParserOptions()
    : ASTContext(0)
    , toolSetToUse(0)
    , noStandardIncludes(false)
    , noBuiltinIncludes(false)
    , microsoftMode(false)
    , verbose(false)
    , unityBuild(false)
    , skipPrivateDeclarations(true)
    , skipLayoutInfo(false)
    , skipFunctionBodies(true)
    , clangVersion(CLANG_VERSION_STRING)
{
}

CppParserOptions::~CppParserOptions() {}

std::string CppParserOptions::getClangVersion() { return clangVersion; }

DEF_VECTOR_STRING(CppParserOptions, Arguments)
DEF_VECTOR_STRING(CppParserOptions, SourceFiles)
DEF_VECTOR_STRING(CppParserOptions, IncludeDirs)
DEF_VECTOR_STRING(CppParserOptions, SystemIncludeDirs)
DEF_VECTOR_STRING(CppParserOptions, Defines)
DEF_VECTOR_STRING(CppParserOptions, Undefines)
DEF_VECTOR_STRING(CppParserOptions, SupportedStdTypes)

ParserResult::ParserResult()
    : targetInfo(0)
{
}

ParserResult::ParserResult(const ParserResult& rhs)
    : kind(rhs.kind)
    , Diagnostics(rhs.Diagnostics)
    , Libraries(rhs.Libraries)
    , targetInfo(rhs.targetInfo)
{}

ParserResult::~ParserResult()
{
    for (auto Library : Libraries)
    {
        delete Library;
    }
}

DEF_VECTOR(ParserResult, ParserDiagnostic, Diagnostics)
DEF_VECTOR(ParserResult, NativeLibrary*, Libraries)

LinkerOptions::LinkerOptions(const char* Triple)
{
    llvm::Triple TargetTriple(Triple && strlen(Triple) > 0 ?
        llvm::Triple::normalize(Triple) : llvm::sys::getDefaultTargetTriple());
    switch (TargetTriple.getOS())
    {
    case llvm::Triple::OSType::Win32:
        addArguments("-dll");
        addArguments("libcmt.lib");
        switch (TargetTriple.getEnvironment())
        {
        case llvm::Triple::EnvironmentType::MSVC:
            break;

        case llvm::Triple::EnvironmentType::GNU:
            addArguments("libstdc++-6.dll");
            break;

        default:
            throw std::invalid_argument("Target triple environment");
        }
        break;

    case llvm::Triple::OSType::Linux:
        addArguments("-L/usr/lib/x86_64-linux-gnu");
        addArguments("-lc");
        addArguments("--shared");
        addArguments("-rpath");
        addArguments(".");
        break;

    case llvm::Triple::OSType::Darwin:
    case llvm::Triple::OSType::MacOSX:
        addArguments("-lc++");
        addArguments("-lSystem");
        addArguments("-dylib");
        addArguments("-sdk_version");
        addArguments("10.12.0");
        addArguments("-L/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk/usr/lib");
        addArguments("-rpath");
        addArguments(".");
        break;

    default:
        throw std::invalid_argument("Target triple operating system");
    }
}

LinkerOptions::LinkerOptions(const LinkerOptions& Other)
{
    for (const std::string& Argument : Other.Arguments)
    {
        Arguments.push_back(Argument);
    }
    for (const std::string& LibraryDir : Other.LibraryDirs)
    {
        LibraryDirs.push_back(LibraryDir);
    }
    for (const std::string& Library : Other.Libraries)
    {
        Libraries.push_back(Library);
    }
}

LinkerOptions::~LinkerOptions() {}

DEF_VECTOR_STRING(LinkerOptions, Arguments)
DEF_VECTOR_STRING(LinkerOptions, LibraryDirs)
DEF_VECTOR_STRING(LinkerOptions, Libraries)

ParserDiagnostic::ParserDiagnostic() {}

ParserDiagnostic::ParserDiagnostic(const ParserDiagnostic& rhs)
    : fileName(rhs.fileName)
    , message(rhs.message)
    , level(rhs.level)
    , lineNumber(rhs.lineNumber)
    , columnNumber(rhs.columnNumber)
{}

ParserDiagnostic::~ParserDiagnostic() {}

} }