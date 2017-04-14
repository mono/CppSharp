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
