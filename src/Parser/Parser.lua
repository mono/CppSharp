project "Parser"
  
  kind "SharedLib"
  language "C++"
  location "."
  platforms { "x32" }

  flags { common_flags, "Managed" }

  configuration "vs*"
    buildoptions { common_msvc_copts }
  
  -- usingdirs is only supported in per-file configs in our
  -- premake build. remove this once this support is added
  -- at the project level.
  
  configuration { "Main.cpp" }
    flags { "Managed" }
    usingdirs { "../../bin/" }
    
  configuration { "Parser.cpp" }
    flags { "Managed" }
    usingdirs { "../../bin/" }

  configuration "*"
  
  files
  {
    "**.h",
    "**.cpp",
    "**.lua"
  }
  
  includedirs
  {
    "../../deps/LLVM/include",
    "../../deps/LLVM/build/include",
    "../../deps/LLVM/tools/clang/include",
    "../../deps/LLVM/build/tools/clang/include"
  }
  
  configuration "Debug"
    libdirs { "../../deps/LLVM/build/lib/Debug" }

  configuration "Release"
    libdirs { "../../deps/LLVM/build/lib/RelWithDebInfo" }
  
  configuration "*"
  
  links
  {
    "LLVMSupport",
    "LLVMAsmParser",
    "LLVMMC",
    "LLVMMCParser",
    "LLVMX86AsmParser",
    "LLVMX86AsmPrinter",
    "LLVMX86Desc",
    "LLVMX86Info",
    "LLVMX86Utils",
    "clangAnalysis",
    "clangBasic",
    "clangAST",
    "clangDriver",
    "clangEdit",
    "clangFrontend",
    "clangLex",
    "clangParse",
    "clangSema",
    "clangSerialization"
  }