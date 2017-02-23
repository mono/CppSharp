project "CppSharp"

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
  }
