project "CppSharp"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"

  files { "**.cs" }
  vpaths { ["*"] = "*" }
  
  links
  {
    depsdir .. "/vs2017/Microsoft.VisualStudio.Setup.Configuration.Interop"
  }

  nuget { "Microsoft.Win32.Registry:4.7.0" }
