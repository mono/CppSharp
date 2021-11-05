-- Setup the LLVM dependency directories

LLVMRootDir = builddir .. "/llvm/llvm-project"

local LLVMDirPerConfiguration = false

local LLVMRootDirDebug = ""
local LLVMRootDirRelease = ""

require "llvm/LLVM"

function SearchLLVM()
  LLVMRootDirDebug = builddir .. "/llvm/" .. get_llvm_package_name(nil, "Debug")
  LLVMRootDirRelease = builddir .. "/llvm/" .. get_llvm_package_name()

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
        path.join(LLVMRootDirDebug, "lld/include"),
        path.join(LLVMRootDirDebug, "clang/include"),
        path.join(LLVMRootDirDebug, "clang/lib"),
        path.join(LLVMRootDirDebug, "build/include"),
        path.join(LLVMRootDirDebug, "build/clang/include"),
      }

    filter { "configurations:Release" }
      includedirs
      {
        path.join(LLVMRootDirRelease, "include"),
        path.join(LLVMRootDirRelease, "lld/include"),
        path.join(LLVMRootDirRelease, "clang/include"),
        path.join(LLVMRootDirRelease, "clang/lib"),
        path.join(LLVMRootDirRelease, "build/include"),
        path.join(LLVMRootDirRelease, "build/clang/include"),
      }
  else
    local LLVMBuildDir = get_llvm_build_dir()
    includedirs
    {
      path.join(LLVMRootDir, "include"),
      path.join(LLVMRootDir, "lld/include"),
      path.join(LLVMRootDir, "clang/include"),
      path.join(LLVMRootDir, "clang/lib"),
      path.join(LLVMBuildDir, "include"),
      path.join(LLVMBuildDir, "clang/include"),
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
    postbuildcommands
    { 
        -- Workaround Premake OS-specific behaviour when copying folders:
        -- See: https://github.com/premake/premake-core/issues/1232
        '{RMDIR} "%%{cfg.buildtarget.directory}/lib"',
        '{MKDIR} "%%{cfg.buildtarget.directory}/lib"',
        string.format('{COPY} "%s" "%%{cfg.buildtarget.directory}/lib"', clangBuiltinIncludeDir) 
    }
  end
end

function SetupLLVMLibs()
  local c = filter()

  filter { "system:not msc*" }
    defines { "__STDC_CONSTANT_MACROS", "__STDC_LIMIT_MACROS" }

  filter { "system:macosx" }
    links { "c++", "curses", "pthread", "z" }

  filter { "toolset:msc*" }
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

    filter { "configurations:Debug", "toolset:msc*" }
      libdirs { path.join(LLVMBuildDir, "Debug/lib") }

    filter { "configurations:Release", "toolset:msc*" }
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
      "LLVMWindowsManifest",
      "LLVMDebugInfoPDB",
      "LLVMLTO",
      "LLVMPasses",
      "LLVMObjCARCOpts",
      "LLVMLibDriver",
      "LLVMFrontendOpenMP",
      "LLVMOption",
      "LLVMCoverage",
      "LLVMCoroutines",
      "LLVMX86Disassembler",
      "LLVMX86AsmParser",
      "LLVMX86CodeGen",
      "LLVMX86Desc",
      "LLVMX86Info",
      "LLVMipo",
      "LLVMInstrumentation",
      "LLVMVectorize",
      "LLVMLinker",
      "LLVMIRReader",
      "LLVMAsmParser",
      "LLVMMCDisassembler",
      "LLVMCFGuard",
      "LLVMGlobalISel",
      "LLVMSelectionDAG",
      "LLVMAsmPrinter",
      "LLVMDebugInfoDWARF",
      "LLVMCodeGen",
      "LLVMTarget",
      "LLVMScalarOpts",
      "LLVMInstCombine",
      "LLVMAggressiveInstCombine",
      "LLVMTransformUtils",
      "LLVMBitWriter",
      "LLVMAnalysis",
      "LLVMProfileData",
      "LLVMObject",
      "LLVMTextAPI",
      "LLVMBitReader",
      "LLVMCore",
      "LLVMRemarks",
      "LLVMBitstreamReader",
      "LLVMMCParser",
      "LLVMMC",
      "LLVMDebugInfoCodeView",
      "LLVMDebugInfoMSF",
      "LLVMBinaryFormat",
      "LLVMSupport",
      "LLVMDemangle",
      "lldCommon",
      "lldCore",
      "lldCOFF",
      "lldELF",
      "lldDriver",
      "lldReaderWriter",
      "lldYAML",
      "lldMachO"
    }
    
  filter(c)
end
