project "CppSharp.AST"

  kind  "SharedLib"
  language "C#"
  location "."

  files { "*.cs" }
  links { "System", "System.Core" }