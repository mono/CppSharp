project "CppSharp.Parser.Bootstrap"

  kind "ConsoleApp"
  language "C#"
  SetupManagedProject()
  debugdir "."
  
  files { "Bootstrap.cs", "*.lua" }
  links { "CppSharp", "CppSharp.AST", "CppSharp.Generator", "System", "System.Core" }

  SetupParser()
  