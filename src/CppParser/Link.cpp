/************************************************************************
*
* CppSharp
* Licensed under the simplified BSD license. All rights reserved.
*
************************************************************************/

#include "CppParser.h"
#include "Parser.h"
#include <Driver/ToolChains/MSVC.h>
#include <Driver/ToolChains/Linux.h>
#include <lld/Common/Driver.h>

using namespace CppSharp::CppParser;

void Parser::Link(const std::string& File, const CppLinkerOptions* LinkerOptions)
{
    std::vector<const char*> args;
    llvm::StringRef Dir(llvm::sys::path::parent_path(File));
    llvm::StringRef Stem = llvm::sys::path::stem(File);

    const llvm::Triple Triple = c->getTarget().getTriple();
    switch (Triple.getOS())
    {
    case llvm::Triple::OSType::Win32:
        args.push_back("-subsystem:windows");
        switch (Triple.getEnvironment())
        {
        case llvm::Triple::EnvironmentType::MSVC:
            LinkWindows(LinkerOptions, args, Dir, Stem);
            break;

        case llvm::Triple::EnvironmentType::GNU:
            LinkWindows(LinkerOptions, args, Dir, Stem, true);
            break;

        default:
            throw std::invalid_argument("Target triple environment");
        }
        break;

    case llvm::Triple::OSType::Linux:
        LinkELF(LinkerOptions, args, Dir, Stem);
        break;

    case llvm::Triple::OSType::Darwin:
    case llvm::Triple::OSType::MacOSX:
        LinkMachO(LinkerOptions, args, Dir, Stem);
        break;

    default:
        throw std::invalid_argument("Target triple operating system");
    }
}

void Parser::LinkWindows(const CppLinkerOptions* LinkerOptions,
    std::vector<const char*>& args,
    const llvm::StringRef& Dir, llvm::StringRef& Stem, bool MinGW)
{
#ifdef _WIN32
    using namespace llvm;
    using namespace clang;

    if (MinGW)
    {
        args.push_back("-lldmingw");
    }

    const Triple& Triple = c->getTarget().getTriple();
    driver::Driver D("", Triple.str(), c->getDiagnostics());
    opt::InputArgList Args(0, 0);
    driver::toolchains::MSVCToolChain TC(D, Triple, Args);

    std::vector<std::string> LibraryPaths;
    LibraryPaths.push_back("-libpath:" + TC.getSubDirectoryPath(
        clang::driver::toolchains::MSVCToolChain::SubDirectoryType::Lib));
    std::string CRTPath;
    if (TC.getUniversalCRTLibraryPath(Args, CRTPath))
        LibraryPaths.push_back("-libpath:" + CRTPath);
    std::string WinSDKPath;
    if (TC.getWindowsSDKLibraryPath(Args, WinSDKPath))
        LibraryPaths.push_back("-libpath:" + WinSDKPath);
    for (const auto& LibraryDir : LinkerOptions->LibraryDirs)
        LibraryPaths.push_back("-libpath:" + LibraryDir);
    for (const auto& LibraryPath : LibraryPaths)
        args.push_back(LibraryPath.data());

    for (const std::string& Arg : LinkerOptions->Arguments)
    {
        args.push_back(Arg.data());
    }

    std::string LibExtension(MinGW ? "" : ".lib");

    std::vector<std::string> Libraries;
    for (const auto& Library : LinkerOptions->Libraries)
        Libraries.push_back(Library + LibExtension);
    for (const auto& Library : Libraries)
        args.push_back(Library.data());

    args.push_back(c->getFrontendOpts().OutputFile.data());
    SmallString<1024> Output(Dir);
    sys::path::append(Output, Stem + ".dll");
    std::string Out("-out:" + std::string(Output));
    args.push_back(Out.data());

    lld::coff::link(args, false, outs(), errs());
#endif
}

void Parser::LinkELF(const CppLinkerOptions* LinkerOptions,
    std::vector<const char*>& args,
    llvm::StringRef& Dir, llvm::StringRef& Stem)
{
#ifdef __linux__
    using namespace llvm;

    args.push_back("-flavor gnu");
    for (const std::string& Arg : LinkerOptions->Arguments)
    {
        args.push_back(Arg.data());
    }

    std::string LinkingDir("-L" + Dir.str());
    args.push_back(LinkingDir.data());
    std::vector<std::string> LibraryDirs;
    for (const auto& LibraryDir : LinkerOptions->LibraryDirs)
        LibraryDirs.push_back("-L" + LibraryDir);
    for (const auto& LibraryDir : LibraryDirs)
        args.push_back(LibraryDir.data());

    std::vector<std::string> Libraries;
    for (const auto& Library : LinkerOptions->Libraries)
        Libraries.push_back("-l" + Library);
    for (const auto& Library : Libraries)
        args.push_back(Library.data());

    args.push_back(c->getFrontendOpts().OutputFile.data());

    args.push_back("-o");
    SmallString<1024> Output(Dir);
    sys::path::append(Output, "lib" + Stem + ".so");
    std::string Out(Output);
    args.push_back(Out.data());

    lld::elf::link(args, false, outs(), errs());
#endif
}

void Parser::LinkMachO(const CppLinkerOptions* LinkerOptions,
    std::vector<const char*>& args,
    llvm::StringRef& Dir, llvm::StringRef& Stem)
{
#ifdef __APPLE__
    using namespace llvm;

    args.push_back("-flavor darwin");
    for (const std::string& Arg : LinkerOptions->Arguments)
    {
        args.push_back(Arg.data());
    }

    std::string LinkingDir("-L" + Dir.str());
    args.push_back(LinkingDir.data());
    std::vector<std::string> LibraryDirs;
    for (const auto& LibraryDir : LinkerOptions->LibraryDirs)
        LibraryDirs.push_back("-L" + LibraryDir);
    for (const auto& LibraryDir : LibraryDirs)
        args.push_back(LibraryDir.data());

    std::vector<std::string> Libraries;
    for (const auto& Library : LinkerOptions->Libraries)
        Libraries.push_back("-l" + Library);
    for (const auto& Library : Libraries)
        args.push_back(Library.data());

    args.push_back(c->getFrontendOpts().OutputFile.data());

    args.push_back("-o");
    SmallString<1024> Output(Dir);
    sys::path::append(Output, "lib" + Stem + ".dylib");
    std::string Out(Output);
    args.push_back(Out.data());

    lld::macho::link(args, false, outs(), errs());
#endif
}