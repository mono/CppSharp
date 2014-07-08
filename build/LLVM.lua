-- Setup the LLVM dependency directories

LLVMRootDir = path.getabsolute("../deps/llvm")
LLVMBuildDir = path.join(LLVMRootDir, "build")

function FindFile(base, files)
    i = 1
    while files[i] do
        fullpath = path.join(base, files[i])
        if os.isfile(fullpath) then
            return fullpath
        end
        i = i + 1
    end
end

LLVMConfig = FindFile(LLVMBuildDir, {
    "RelWithDebInfo/bin/llvm-config",
    "RelWithDebInfo/bin/llvm-config.exe",
    "Release/bin/llvm-config",
    "Release/bin/llvm-config.exe",
    "bin/llvm-config",
    "bin/llvm-config.exe",
    "Debug/bin/llvm-config",
    "Debug/bin/llvm-config.exe"
})

if not LLVMConfig then
    print "llvm-config not found"
    os.exit(1)
end

function RunLLVMConfig(params)
    local output = os.outputof(LLVMConfig .. " " .. params)
    output = string.gsub(output, "[\r\n]", "")

    -- trim leading and trailing whitespace
    output = string.gsub(output, "^%s*(.-)%s*$", "%1")

    return output
end

function GetLLVMLibs()
    local libs = RunLLVMConfig("--libs engine option")
    libs = string.gsub(libs, "-l", "")
    libs = string.explode(libs, " ")
    return libs
end

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
    }
    links { GetLLVMLibs() }
    
  configuration(c)
end
