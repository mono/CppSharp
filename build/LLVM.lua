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
  
  configuration { "Debug", "vs*" }
    libdirs { path.join(LLVMBuildDir, "Debug/lib") }

  configuration { "Release", "vs*" }
    libdirs { path.join(LLVMBuildDir, "RelWithDebInfo/lib") }

  configuration "not vs*"
    buildoptions { "-fpermissive" }
    defines { "__STDC_CONSTANT_MACROS", "__STDC_LIMIT_MACROS" }
    libdirs { path.join(LLVMBuildDir, "lib") }

  configuration "macosx"
    links { "c++", "curses", "pthread", "z" }

  configuration(c)
end