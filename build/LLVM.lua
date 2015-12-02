-- Setup the LLVM dependency directories

LLVMRootDir = depsdir .. "/llvm/"

require "scripts/LLVM"

function SearchLLVM()
  local pkg_path = basedir .. "/scripts/" .. get_llvm_package_name()
  if os.isdir(pkg_path) then
    LLVMRootDir = pkg_path
  elseif os.isdir(LLVMRootDir) then
  else
    error("Error finding an LLVM build")
  end

  print("Using LLVM build: " .. LLVMRootDir)
end

function get_llvm_build_dir()
  return path.join(LLVMRootDir, "build")
end

function SetupLLVMIncludes()
  local c = configuration()

  local LLVMBuildDir = get_llvm_build_dir()
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

  local LLVMBuildDir = get_llvm_build_dir()
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
