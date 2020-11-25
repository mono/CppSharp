project "CppSharp.CLI"

  SetupManagedProject()

  kind "ConsoleApp"
  language "C#"

  links { "CppSharp.Generator" }
