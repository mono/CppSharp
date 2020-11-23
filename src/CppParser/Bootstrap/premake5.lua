project "CppSharp.Parser.Bootstrap"

  SetupManagedProject()
  SetupParser()

  kind "ConsoleApp"
  language "C#"
  debugdir "."

  links {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser"
  }
