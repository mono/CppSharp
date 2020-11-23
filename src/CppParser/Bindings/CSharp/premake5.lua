if not EnabledManagedProjects() then
  return
end

project "CppSharp.Parser.CSharp"
  SetupManagedProject()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"
  enabledefaultcompileitems "false"
  enabledefaultnoneitems "false"

  dependson { "CppSharp.CppParser" }
  links { "CppSharp.Runtime" }

  AddPlatformSpecificFiles("", "**.cs")
  AddPlatformSpecificFiles("", "**.cpp")

function SetupParser()
  links
  {
    "CppSharp.Parser.CSharp",
    "CppSharp.Runtime"
  }
end