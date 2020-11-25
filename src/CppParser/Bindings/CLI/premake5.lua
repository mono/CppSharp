include "../../../../build/LLVM.lua"

project "CppSharp.Parser.CLI"
  SetupNativeProject()

  kind "SharedLib"
  language "C++"

  dependson { "CppSharp.CppParser" }
  flags { common_flags }
  clr "NetCore"

  filter "toolset:msc*"
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
  
  SetupLLVMIncludes()

  filter {}

  links { "CppSharp.CppParser" }

CppSharpParserBindings = "CppSharp.Parser.CLI"