project "CppSharp.Parser"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"

  files { "**.cs" }
  vpaths { ["*"] = "*" }

  links
  {
    "System",
    "System.Core",
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Runtime"
  }

  SetupParser()
