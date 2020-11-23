project "CppSharp.CLI"

  SetupManagedProject()
  SetupParser()

  kind "ConsoleApp"
  language "C#"

  links
  {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser"
  }
