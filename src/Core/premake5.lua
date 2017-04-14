project "CppSharp"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"

  files { "**.cs" }
  vpaths { ["*"] = "*" }
  
  libdirs{ depsdir .."/vs2017" }
  
  links
  {
    "System",
    "System.Core",
    "Microsoft.VisualStudio.Setup.Configuration.Interop"
  }
