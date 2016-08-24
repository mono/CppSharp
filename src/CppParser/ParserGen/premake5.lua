project "CppSharp.Parser.Gen"

  kind "ConsoleApp"
  language "C#"
  SetupManagedProject()
  debugdir "."
  
  files { "ParserGen.cs", "*.lua" }
  links { "CppSharp", "CppSharp.AST", "CppSharp.Generator", "System.Core" }

  SetupParser()