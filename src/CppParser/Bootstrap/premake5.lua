project "CppSharp.Parser.Bootstrap"

  SetupManagedProject()

  kind "ConsoleApp"
  language "C#"
  debugdir "."
  
  files { "*.cs", "*.lua" }
  links { "CppSharp", "CppSharp.AST", "CppSharp.Generator", "CppSharp.Parser" }

  filter { "action:not netcore" }
    links { "System", "System.Core" }

  SetupParser()
