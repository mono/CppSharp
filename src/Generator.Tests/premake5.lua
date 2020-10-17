project "CppSharp.Generator.Tests"

  kind "SharedLib"
  SetupManagedProject()

  files { "**.cs" }
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

  nuget
  {
    "NUnit:3.12.0",
    "NSubstitute:4.2.2"
  }
