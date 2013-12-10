-- Setup the LLVM dependency directories

LLVMRootDir = "../../deps/LLVM/"
LLVMBuildDir = "../../deps/LLVM/build/"

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
    libdirs { path.join(LLVMBuildDir, "lib/Debug") }

  configuration { "Release", "vs*" }
    libdirs { path.join(LLVMBuildDir, "lib/RelWithDebInfo") }

  configuration "not vs*"
    buildoptions { "-fpermissive" }
    defines { "__STDC_CONSTANT_MACROS", "__STDC_LIMIT_MACROS" }
    libdirs { path.join(LLVMBuildDir, "lib") }

  configuration "macosx"
    links { "c++", "curses", "pthread", "z" }

  configuration(c)
end