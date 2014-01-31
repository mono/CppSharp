project "CppSharp.Parser.Gen"

  kind "ConsoleApp"
  language "C#"
  SetupManagedProject()
  debugdir "."
  
  files { "ParserGen.cs", "*.lua" }
  links { "CppSharp.AST", "CppSharp.Generator" }

  SetupParser()
  
project "CppSharp.Parser.CSharp"
  
  kind "SharedLib"
  language "C#"
  SetupManagedProject()
  
  dependson { "CppSharp.CppParser" }
  flags { common_flags, "Unsafe" }

  files
  {
    "**.lua"
  }

  links { "CppSharp.Runtime" }

  configuration "macosx"
      files { "CSharp/i686-apple-darwin12.4.0/**.cs" }

  configuration "not macosx"
      files { "CSharp/*.cs" }

  configuration ""

if string.starts(action, "vs") then

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

end