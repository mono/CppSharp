project "CppSharp.Parser.Gen"

  SetupManagedProject()

  kind "ConsoleApp"
  language "C#"
  debugdir "."

  links { "CppSharp.Generator" }
