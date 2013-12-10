project "CppSharp.Parser.Gen"

  kind "ConsoleApp"
  language "C#"
  location "."
  debugdir "."
  
  files { "ParserGen.cs", "*.lua" }
  links { "CppSharp.AST", "CppSharp.Generator" }

  configuration { "vs*" }
    links { "CppSharp.Parser" }

  configuration { "not vs*" }
    links { "CppSharp.Parser.CSharp" }
  
project "CppSharp.Parser.CSharp"
  
  kind "SharedLib"
  language "C#"
  location "."
  
  dependson { "CppSharp.CppParser" }
  flags { common_flags, "Unsafe" }

  files
  {
    "CSharp/**.cs",
    "**.lua"
  }

  links { "CppSharp.Runtime" }

configuration "vs*"

  project "CppSharp.Parser.CLI"
    
    kind "SharedLib"
    language "C++"
    SetupNativeProject()
    
    dependson { "CppSharp.CppParser" }
    flags { common_flags, "Managed" }

    configuration "vs*"
      buildoptions { clang_msvc_flags }  

    configuration "*"
    
    files
    {
      "CLI/AST.h",
      "CLI/AST.cpp",
      "CLI/**.h",
      "CLI/**.cpp",
      "**.lua"
    }
    
    includedirs { "../../../include/", "../../../src/CppParser/" }
    
    configuration "*"
    links { "CppSharp.CppParser" }