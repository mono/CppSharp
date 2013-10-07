clang_msvc_flags =
{
  "/wd4146", "/wd4244", "/wd4800", "/wd4345",
  "/wd4355", "/wd4996", "/wd4624", "/wd4291",
  "/wd4251"
}

LLVMRootDir = "../../deps/LLVM/"
LLVMBuildDir = "../../deps/LLVM/build_mingw/"

project "CppSharp.CppParser"
  
  kind "SharedLib"
  language "C++"
  SetupNativeProject()
  flags { common_flags }
  flags { "NoRTTI" }

  configuration "vs*"
    buildoptions { clang_msvc_flags }
    files { "VSLookup.cpp" }    

  configuration "*"
  
  files
  {
    "*.h",
    "*.cpp",
    "*.lua"
  }
  
  includedirs
  {
    path.join(LLVMRootDir, "include"),
    path.join(LLVMRootDir, "tools/clang/include"),    
    path.join(LLVMRootDir, "tools/clang/lib"),    
    path.join(LLVMBuildDir, "include"),
    path.join(LLVMBuildDir, "tools/clang/include"),
  }
  
  configuration { "Debug", "vs*" }
    libdirs { path.join(LLVMBuildDir, "lib/Debug") }

  configuration { "Release", "vs*" }
    libdirs { path.join(LLVMBuildDir, "lib/RelWithDebInfo") }

  configuration "not vs*"
    buildoptions { "-fpermissive" }
    defines { "__STDC_CONSTANT_MACROS", "__STDC_LIMIT_MACROS" }
    libdirs { path.join(LLVMBuildDir, "lib") }

  configuration "macosx"
    links { "c++", "curses", "pthread", "z" }
  
  configuration "*"
  
  links
  {
    "clangAnalysis",
    "clangAST",
    "clangBasic",
    "clangCodeGen",
    "clangDriver",
    "clangEdit",
    "clangFrontend",
    "clangLex",
    "clangParse",
    "clangSema",
    "clangSerialization",
    "LLVMAnalysis",
    "LLVMAsmParser",
    "LLVMBitReader",
    "LLVMBitWriter",
    "LLVMCodeGen",
    "LLVMCore",
    "LLVMipa",
    "LLVMipo",
    "LLVMInstCombine",
    "LLVMInstrumentation",
    "LLVMIRReader",
    "LLVMLinker",
    "LLVMMC",
    "LLVMMCParser",
    "LLVMObjCARCOpts",
    "LLVMObject",
    "LLVMOption",
    "LLVMScalarOpts",
    "LLVMSupport",
    "LLVMTarget",
    "LLVMTransformUtils",
    "LLVMVectorize",
    "LLVMX86AsmParser",
    "LLVMX86AsmPrinter",
    "LLVMX86Desc",
    "LLVMX86Info",
    "LLVMX86Utils",
  }

include ("Bindings")