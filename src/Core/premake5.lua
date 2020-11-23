project "CppSharp"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"

  nuget {
    "Microsoft.Win32.Registry:4.7.0",
    "Microsoft.VisualStudio.Setup.Configuration.Interop:2.3.2262-g94fae01e"
  }
