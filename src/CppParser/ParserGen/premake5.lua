project "CppSharp.Parser.Gen"

  SetupManagedProject()
  SetupParser()

  kind "ConsoleApp"
  language "C#"
  debugdir "."

  links
  {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser",
  }
