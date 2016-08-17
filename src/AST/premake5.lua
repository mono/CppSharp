project "CppSharp.AST"

  kind  "SharedLib"
  language "C#"

  SetupManagedProject()

  files { "*.cs" }
  links { "System", "System.Core" }
