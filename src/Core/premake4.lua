project "CppSharp"

  kind "SharedLib"
  language "C#"
  flags { "Unsafe"}

  SetupManagedProject()

  files { "**.cs" }
  links
  {
    "System",
    "System.Core",
    "CppSharp.AST",
    "CppSharp.Runtime"
  }

  SetupParser()
