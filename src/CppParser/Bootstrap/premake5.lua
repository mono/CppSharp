project "CppSharp.Parser.Bootstrap"

  SetupManagedProject()

  kind "ConsoleApp"
  language "C#"
  debugdir "."
  
  files { "Bootstrap.cs", "*.lua" }
  links { "CppSharp", "CppSharp.AST", "CppSharp.Generator", "System", "System.Core" }

  SetupParser()
  