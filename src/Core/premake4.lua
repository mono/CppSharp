project "CppSharp"

  kind "SharedLib"
  language "C#"

  files { "**.cs" }
  links
  {
    "System",
    "System.Core",
    "CppSharp.AST",
  }

  SetupParser()

  configuration "vs*"
    location "."