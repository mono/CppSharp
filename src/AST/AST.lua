project "CppSharp.AST"

  kind  "SharedLib"
  language "C#"

  configuration "vs*"
  	location "."

  files { "*.cs" }
  links { "System", "System.Core" }