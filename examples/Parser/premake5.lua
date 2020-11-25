include "../../build/Tests.lua"

project "Parser"
  kind "ConsoleApp"
  language "C#"
  debugdir "."
  links { "CppSharp.Parser" }

  SetupManagedProject()
