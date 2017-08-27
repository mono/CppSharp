include "../../../../build/LLVM.lua"

project "CppSharp.Parser.CLI"
  
  kind "SharedLib"
  language "C++"
  SetupNativeProject()
  SetupLLVMIncludes()
  
  dependson { "CppSharp.CppParser" }
  flags { common_flags }
  clr "On"

  filter "action:vs*"
    buildoptions { clang_msvc_flags }  

  filter {}
  
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
  
  filter {}

  links { "CppSharp.CppParser" }

function SetupParser()
  links { "CppSharp.Parser.CLI" }
end  