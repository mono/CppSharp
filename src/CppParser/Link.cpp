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

void Parser::Link(const std::string& File, const LinkerOptions* LinkerOptions)
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
            LinkMSVC(LinkerOptions, args, Dir, Stem);
            break;

        case llvm::Triple::EnvironmentType::GNU:
            LinkMinGW(LinkerOptions, args, Dir, Stem);
            break;

        default:
            break;
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
        break;
    }
}

void Parser::LinkMSVC(const LinkerOptions* LinkerOptions,
    std::vector<const char*>& args, const llvm::StringRef& Dir, llvm::StringRef& Stem)
{
#ifdef _WIN32
    using namespace llvm;
    using namespace clang;

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

    args.push_back("-dll");
    args.push_back("libcmt.lib");

    std::vector<std::string> Libraries;
    for (const auto& Library : LinkerOptions->Libraries)
        Libraries.push_back(Library + ".lib");
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

void Parser::LinkMinGW(const LinkerOptions* LinkerOptions,
    std::vector<const char*>& args, const llvm::StringRef& Dir, llvm::StringRef& Stem)
{
#ifdef _WIN32
    using namespace llvm;
    using namespace clang;

    args.push_back("-lldmingw");

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

    args.push_back("-dll");
    args.push_back("libcmt.lib");
    args.push_back("libstdc++.dll.a");

    // TODO: MinGW has an additional library dir for its specific C++ STD (above); hard-coded now for testing but needs fixing
    args.push_back("-libpath:C:\\Qt\\Tools\\mingw730_64\\lib\\gcc\\x86_64-w64-mingw32\\7.3.0");
    //    std::string LinkingDir("-L" + Dir.str());

    std::vector<std::string> Libraries;
    for (const auto& Library : LinkerOptions->Libraries)
        // TODO: libs are a problem with MinGW; first, I need the lib dir as well as the bin dir given now; second, the libs are lib*.a, not *.lib;
        // the -l syntax should handle it but calling MinGW directly hangs the process; still, it's highly desirable to use MinGW because it does much of the work, such as this conversion from lib*.a
        // on the other hand, keeping the MSVC allows me to reuse almost all of the code from LinkMSVC
        Libraries.push_back("lib" + Library + ".a");
    for (const auto& Library : Libraries)
        args.push_back(Library.data());

    args.push_back(c->getFrontendOpts().OutputFile.data());
    SmallString<1024> Output(Dir);
    sys::path::append(Output, Stem + ".dll");
    std::string Out("-out:" + std::string(Output));
    args.push_back(Out.data());

    lld::coff::link(args, false, outs(), errs());
#endif
//#ifdef _WIN32
//    using namespace llvm;
//    using namespace clang;
//
//    //g++ -c -fno-keep-inline-dllexport -Wa,-mbig-obj -O2 -std=gnu++11 -Wall -W -Wextra -fexceptions -mthreads -DUNICODE -D_UNICODE -DWIN32 -DMINGW_HAS_SECURE_API=1 -DQT_NO_DEBUG -DQT_GUI_LIB -DQT_CORE_LIB -I.
//    //    -IC:\Qt\5.12.5\mingw73_64\include -IC:\Qt\5.12.5\mingw73_64\include\QtGui -IC:\Qt\5.12.5\mingw73_64\include\QtANGLE -IC:\Qt\5.12.5\mingw73_64\include\QtCore -Irelease
//    //    -I/include -IC:\Qt\5.12.5\mingw73_64\mkspecs\win32-g++  -o release\QtCore-symbols.o QtCore-symbols.cpp
//
//    const Triple& Triple = c->getTarget().getTriple();
//    driver::Driver D("", Triple.str(), c->getDiagnostics());
//    driver::toolchains::MSVCToolChain TC(D, Triple, opt::InputArgList(0, 0));
//
//    std::vector<std::string> LibraryPaths;
//    LibraryPaths.push_back("-L" + TC.getSubDirectoryPath(
//        clang::driver::toolchains::MSVCToolChain::SubDirectoryType::Lib));
//    std::string CRTPath;
//    if (TC.getUniversalCRTLibraryPath(CRTPath))
//        LibraryPaths.push_back("-L" + CRTPath);
//    std::string WinSDKPath;
//    if (TC.getWindowsSDKLibraryPath(WinSDKPath))
//        LibraryPaths.push_back("-L" + WinSDKPath);
//    for (const auto& LibraryDir : LinkerOptions->LibraryDirs)
//        LibraryPaths.push_back("-L" + LibraryDir);
//    for (const auto& LibraryPath : LibraryPaths)
//        args.push_back(LibraryPath.data());
//
//    args.push_back("libcmt.lib");
//    args.push_back("-lstdc++");
//    args.push_back("--shared");
//
//    args.push_back("-LC:\\Qt\\Tools\\mingw730_64\\lib\\gcc\\x86_64-w64-mingw32\\7.3.0");
//    std::string LinkingDir("-L" + Dir.str());
//    args.push_back(LinkingDir.data());
//    std::vector<std::string> LibraryDirs;
//    for (const auto& LibraryDir : LinkerOptions->LibraryDirs)
//        LibraryDirs.push_back("-L" + LibraryDir);
//    for (const auto& LibraryDir : LibraryDirs)
//        args.push_back(LibraryDir.data());
//
//    std::vector<std::string> Libraries;
//    for (const auto& Library : LinkerOptions->Libraries)
//        Libraries.push_back("-l" + Library);
//    for (const auto& Library : Libraries)
//        args.push_back(Library.data());
//
//    args.push_back(c->getFrontendOpts().OutputFile.data());
//
//    args.push_back("-o");
//    SmallString<1024> Output(Dir);
//    sys::path::append(Output, Stem + ".dll");
//    std::string Out(Output);
//    args.push_back(Out.data());
//
//    lld::mingw::link(args, false, outs(), errs());
//#endif
}

void Parser::LinkELF(const LinkerOptions* LinkerOptions,
    std::vector<const char*>& args,
    llvm::StringRef& Dir, llvm::StringRef& Stem)
{
#ifdef __linux__
    using namespace llvm;

    args.push_back("-flavor gnu");
    args.push_back("-L/usr/lib/x86_64-linux-gnu");
    args.push_back("-lc");
    args.push_back("--shared");
    args.push_back("-rpath");
    args.push_back(".");

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

void Parser::LinkMachO(const LinkerOptions* LinkerOptions,
    std::vector<const char*>& args,
    llvm::StringRef& Dir, llvm::StringRef& Stem)
{
#ifdef __APPLE__
    using namespace llvm;

    args.push_back("-flavor darwinnew");
    args.push_back("-lc++");
    args.push_back("-lSystem");
    args.push_back("-dylib");
    args.push_back("-sdk_version");
    args.push_back("10.12.0");
    args.push_back("-L/Library/Developer/CommandLineTools/SDKs/MacOSX.sdk/usr/lib");
    args.push_back("-rpath");
    args.push_back(".");

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

    lld::mach_o::link(args, false, outs(), errs());
#endif
}