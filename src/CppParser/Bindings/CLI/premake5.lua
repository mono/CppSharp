include "../../../../build/LLVM.lua"

project "CppSharp.Parser.CLI"
  
  kind "SharedLib"
  language "C++"
  SetupNativeProject()
  SetupLLVMIncludes()
  
  dependson { "CppSharp.CppParser" }
  flags { common_flags, "Managed" }

  configuration "vs*"
    buildoptions { clang_msvc_flags }  

  configuration "*"
  
  files
  {
    "**.h",
    "**.cpp",
    "**.lua"
  }
  
  includedirs
  {
    "../../../../include/",
    "../../../../src/CppParser/"
  }
  
  configuration "*"

  links { "CppSharp.CppParser" }

function SetupParser()
  links { "CppSharp.Parser.CLI" }
end  