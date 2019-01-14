project "CppSharp.Parser.Gen"

  kind "ConsoleApp"
  language "C#"
  SetupManagedProject()
  debugdir "."
  
  files { "ParserGen.cs", "*.lua" }
  vpaths { ["*"] = "*" }

  links
  {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser",
  }

  SetupParser()

  filter { "action:not netcore"}
    links { "System.Core" }
