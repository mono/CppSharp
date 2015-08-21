-- Setup the LLVM dependency directories

LLVMRootDir = "../../deps/llvm/"
LLVMBuildDir = "../../deps/llvm/build/"

-- TODO: Search for available system dependencies

function SetupLLVMIncludes()
  local c = configuration()

  includedirs
  {
    path.join(LLVMRootDir, "include"),
    path.join(LLVMRootDir, "tools/clang/include"),    
    path.join(LLVMRootDir, "tools/clang/lib"),    
    path.join(LLVMBuildDir, "include"),
    path.join(LLVMBuildDir, "tools/clang/include"),
  }

  configuration(c)
end

function SetupLLVMLibs()
  local c = configuration()

  libdirs { path.join(LLVMBuildDir, "lib") }

  configuration { "Debug", "vs*" }
    libdirs { path.join(LLVMBuildDir, "Debug/lib") }

  configuration { "Release", "vs*" }
    libdirs { path.join(LLVMBuildDir, "RelWithDebInfo/lib") }

  configuration "not vs*"
    defines { "__STDC_CONSTANT_MACROS", "__STDC_LIMIT_MACROS" }

  configuration "macosx"
    links { "c++", "curses", "pthread", "z" }

  configuration "*"
    links
    {
      "clangFrontend",
      "clangDriver",
      "clangSerialization",
      "clangCodeGen",
      "clangParse",
      "clangSema",
      "clangAnalysis",
      "clangEdit",
      "clangAST",
      "clangLex",
      "clangBasic",
      "clangIndex",
      "LLVMLinker",
      "LLVMipo",
      "LLVMVectorize",
      "LLVMBitWriter",
      "LLVMIRReader",
      "LLVMAsmParser",
      "LLVMOption",
      "LLVMInstrumentation",
      "LLVMProfileData",
      "LLVMX86AsmParser",
      "LLVMX86Desc",
      "LLVMObject",
      "LLVMMCParser",
      "LLVMBitReader",
      "LLVMX86Info",
      "LLVMX86AsmPrinter",
      "LLVMX86Utils",
      "LLVMCodeGen",
      "LLVMScalarOpts",
      "LLVMInstCombine",
      "LLVMTransformUtils",
      "LLVMAnalysis",
      "LLVMTarget",
      "LLVMMC",
      "LLVMCore",
      "LLVMSupport",
    }
    
  configuration(c)
end
