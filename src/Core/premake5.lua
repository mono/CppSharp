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
    depsdir .. "/vs2017/Microsoft.VisualStudio.Setup.Configuration.Interop"
  }
