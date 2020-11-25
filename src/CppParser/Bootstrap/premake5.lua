project "CppSharp.Parser.Bootstrap"

  SetupManagedProject()

  kind "ConsoleApp"
  language "C#"
  debugdir "."

  links { "CppSharp.Generator" }
