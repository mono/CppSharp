project "CppSharp.Parser.CSharp"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"

  dependson { "CppSharp.CppParser" }

  files { "**.lua" }
  vpaths { ["*"] = "*" }

  links { "CppSharp.Runtime" }

  AddPlatformSpecificFiles("", "**.cs")

function SetupParser()
  links
  {
    "CppSharp.Parser.CSharp",
    "CppSharp.Runtime"
  }
end