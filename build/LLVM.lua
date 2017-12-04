-- Setup the LLVM dependency directories

LLVMRootDir = depsdir .. "/llvm/"

local LLVMDirPerConfiguration = false

local LLVMRootDirDebug = ""
local LLVMRootDirRelease = ""

require "scripts/LLVM"

function SearchLLVM()
  LLVMRootDirDebug = basedir .. "/scripts/" .. get_llvm_package_name(nil, "Debug")
  LLVMRootDirRelease = basedir .. "/scripts/" .. get_llvm_package_name()

  if os.isdir(LLVMRootDirDebug) or os.isdir(LLVMRootDirRelease) then
    LLVMDirPerConfiguration = true
    print("Using debug LLVM build: " .. LLVMRootDirDebug)
    print("Using release LLVM build: " .. LLVMRootDirRelease)
  elseif os.isdir(LLVMRootDir) then
    print("Using LLVM build: " .. LLVMRootDir)
  else
    error("Error finding an LLVM build")
  end
end

function get_llvm_build_dir()
  local packageDir = path.join(LLVMRootDir, get_llvm_package_name())
  local buildDir = path.join(LLVMRootDir, "build")
  if os.isdir(buildDir) then
    return buildDir
  else
    return packageDir
  end
end

function SetupLLVMIncludes()
  local c = filter()

  if LLVMDirPerConfiguration then
    filter { "configurations:Debug" }
      includedirs
      {
        path.join(LLVMRootDirDebug, "include"),
        path.join(LLVMRootDirDebug, "tools/clang/include"),
        path.join(LLVMRootDirDebug, "tools/clang/lib"),
        path.join(LLVMRootDirDebug, "build/include"),
        path.join(LLVMRootDirDebug, "build/tools/clang/include"),
      }

    filter { "configurations:Release" }
      includedirs
      {
        path.join(LLVMRootDirRelease, "include"),
        path.join(LLVMRootDirRelease, "tools/clang/include"),
        path.join(LLVMRootDirRelease, "tools/clang/lib"),
        path.join(LLVMRootDirRelease, "build/include"),
        path.join(LLVMRootDirRelease, "build/tools/clang/include"),
      }
  else
    local LLVMBuildDir = get_llvm_build_dir()
    includedirs
    {
      path.join(LLVMRootDir, "include"),
      path.join(LLVMRootDir, "tools/clang/include"),
      path.join(LLVMRootDir, "tools/clang/lib"),
      path.join(LLVMBuildDir, "include"),
      path.join(LLVMBuildDir, "tools/clang/include"),
    }
  end

  filter(c)
end

function CopyClangIncludes()
  local clangBuiltinIncludeDir = path.join(LLVMRootDir, "lib")

  if LLVMDirPerConfiguration then
    local clangBuiltinDebug = path.join(LLVMRootDirDebug, "lib")
    local clangBuiltinRelease = path.join(LLVMRootDirRelease, "lib")

    if os.isdir(path.join(clangBuiltinDebug, "clang")) then
      clangBuiltinIncludeDir = clangBuiltinDebug
    end

    if os.isdir(path.join(clangBuiltinRelease, "clang")) then
      clangBuiltinIncludeDir = clangBuiltinRelease
    end    
  end

  if os.isdir(clangBuiltinIncludeDir) then
    postbuildcommands { string.format("{COPY} %s %%{cfg.buildtarget.directory}", clangBuiltinIncludeDir) }
  end
end

function SetupLLVMLibs()
  local c = filter()

  filter { "action:not vs*" }
    defines { "__STDC_CONSTANT_MACROS", "__STDC_LIMIT_MACROS" }

  filter { "system:macosx" }
    links { "c++", "curses", "pthread", "z" }

  filter { "action:vs*" }
    links { "version" }

  filter {}

  if LLVMDirPerConfiguration then
    filter { "configurations:Debug" }
      libdirs { path.join(LLVMRootDirDebug, "build/lib") }

    filter { "configurations:Release" }
      libdirs { path.join(LLVMRootDirRelease, "build/lib") }
  else
    local LLVMBuildDir = get_llvm_build_dir()
    libdirs { path.join(LLVMBuildDir, "lib") }

    filter { "configurations:Debug", "action:vs*" }
      libdirs { path.join(LLVMBuildDir, "Debug/lib") }

    filter { "configurations:Release", "action:vs*" }
      libdirs { path.join(LLVMBuildDir, "RelWithDebInfo/lib") }
  end

  filter {}

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
      "LLVMX86CodeGen",
      "LLVMX86Disassembler",
      "LLVMCodeGen",
      "LLVMSelectionDAG",
      "LLVMGlobalISel",
      "LLVMDebugInfoCodeView",
      "LLVMScalarOpts",
      "LLVMInstCombine",
      "LLVMTransformUtils",
      "LLVMAnalysis",
      "LLVMTarget",
      "LLVMMCDisassembler",
      "LLVMMC",
      "LLVMCoverage",
      "LLVMCore",
      "LLVMSupport",
      "LLVMBinaryFormat",
      "LLVMDemangle"
    }
    
  filter(c)
end
