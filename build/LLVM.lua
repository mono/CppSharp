-- Setup the LLVM dependency directories

LLVMInstallDir = "../../deps/llvm/install"
ClangSrcDir = "../../deps/llvm/tools/clang"

-- TODO: Search for available system dependencies

function SetupLLVMIncludes()
  local c = configuration()

  includedirs
  {
    path.join(LLVMInstallDir, "include"),
    -- We need this to include the private clang CodeGen stuff
    path.join(ClangSrcDir, "lib"),
  }

  configuration(c)
end

function SetupLLVMLibs()
  local c = configuration()

  libdirs { path.join(LLVMInstallDir, "lib") }

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
      "clangIndex",
    }
    StaticLinksOpt { "LLVMProfileData" }
    
  configuration(c)
end
