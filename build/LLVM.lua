-- Setup the LLVM dependency directories

LLVMRootDir = "../../deps/llvm/"
LLVMBuildDir = "../../deps/llvm/build/"

-- TODO: Search for available system dependencies

function SetupLLVMLibs()
  local c = configuration()

  includedirs
  {
    path.join(LLVMRootDir, "include"),
    path.join(LLVMRootDir, "tools/clang/include"),    
    path.join(LLVMRootDir, "tools/clang/lib"),    
    path.join(LLVMBuildDir, "include"),
    path.join(LLVMBuildDir, "tools/clang/include"),
  }
  
  libdirs { path.join(LLVMBuildDir, "lib") }

  configuration { "Debug", "vs*" }
    libdirs { path.join(LLVMBuildDir, "Debug/lib") }

  configuration { "Release", "vs*" }
    libdirs { path.join(LLVMBuildDir, "RelWithDebInfo/lib") }

  configuration "not vs*"
    buildoptions { "-fpermissive" }
    defines { "__STDC_CONSTANT_MACROS", "__STDC_LIMIT_MACROS" }

  configuration "macosx"
    links { "c++", "curses", "pthread", "z" }

  configuration "*"
    links
    {
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
      "LLVMProfileData",
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
    }
    
  configuration(c)
end
