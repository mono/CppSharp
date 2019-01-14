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

  filter { "action:netcore" }
    dotnetframework "netcoreapp2.0"
