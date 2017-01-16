project "CppSharp.CLI"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"

  files { "**.cs" }
  links
  {
    "System",
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser"
  }

  SetupParser()
