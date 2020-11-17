project "CppSharp.Generator.Tests"

  kind "SharedLib"
  SetupManagedProject()

  files { "**.cs" }
  excludes { "obj/**" }
  vpaths { ["*"] = "*" }

  libdirs
  {
    depsdir .. "/NUnit",
    depsdir .. "/NSubstitute"
  }

  files { testsdir .. "/Native/AST.h", testsdir .. "/Native/ASTExtensions.h", testsdir .. "/Native/Passes.h" }
  filter "files:**.h"
     buildaction "None"
  filter {}

  SetupParser()

  links
  {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser",
  }

  filter { "action:netcore"}
    nuget
    {
      "NUnit:3.11.0",
      "NSubstitute:4.0.0-rc1"
    }

  filter { "action:not netcore"}
    links
    {
      "System",
      "System.Core",
      "Microsoft.CSharp",
      "nunit.framework",
      "NSubstitute"
    }
