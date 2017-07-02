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
    "System",
    "System.Core",
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser",
    "nunit.framework",
    "NSubstitute"
  }
