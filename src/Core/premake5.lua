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

  filter { "action:netcore" }
    nuget { "Microsoft.Win32.Registry:4.5.0" }

  filter { "action:not netcore"}
    links { "System", "System.Core" }
