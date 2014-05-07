clang_msvc_flags =
{
  "/wd4146", "/wd4244", "/wd4800", "/wd4345",
  "/wd4355", "/wd4996", "/wd4624", "/wd4291"
}

project "CppSharp.Parser"
  
  kind "SharedLib"
  language "C++"
  SetupNativeProject()
  
  dependson { "CppSharp.AST" }
  flags { common_flags, "Managed" }

  usingdirs { libdir }
  
  -- usingdirs is only supported in per-file configs in our
  -- premake build. remove this once this support is added
  -- at the project level.
  
  configuration { "Main.cpp" }
    flags { "Managed" }
    usingdirs { libdir }

  configuration { "Parser.cpp" }
    flags { "Managed" }
    usingdirs { libdir }

  configuration { "Comments.cpp" }
    flags { "Managed" }
    usingdirs { libdir }
    
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
  
  SetupLLVMIncludes()
  SetupLLVMLibs()
  
  configuration "*"
  
