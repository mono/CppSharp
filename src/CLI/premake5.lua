project "CppSharp.CLI"

  SetupManagedProject()

  kind "ConsoleApp"
  language "C#"

  files { "**.cs" }
  vpaths { ["*"] = "*" }

  links
  {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser"
  }

  SetupParser()

  filter { "action:not netcore"}
    links
    {
      "System",
      "System.Core"
    }

  if _OPTIONS.netcore then
    dotnetframework "netcoreapp3.1"
  else
    dotnetframework "4.7.2"
  end
