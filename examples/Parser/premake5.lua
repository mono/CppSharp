include "../../build/Tests.lua"

project "Parser"
  kind "ConsoleApp"
  language "C#"
  debugdir "."
  
  files { "**.cs", "./*.lua" }
  links
  {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Parser"
  }

  SetupManagedProject()
  SetupParser()
