project "CppSharp.AST"

  kind  "SharedLib"
  language "C#"

  SetupManagedProject()

  files { "*.cs" }
  vpaths { ["*"] = "*" }

  filter { "action:not netcore"}
    links { "System", "System.Core" }
