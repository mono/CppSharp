project "CppSharp.AST"

  kind  "SharedLib"
  language "C#"

  SetupManagedProject()

  files { "*.cs" }
  vpaths { ["*"] = "*" }

  links { "System", "System.Core" }
