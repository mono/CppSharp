project "CppSharp.Generator.Tests"

  kind "SharedLib"
  SetupManagedProject()

  files { "**.cs" }
  
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
    "nunit.framework",
    "NSubstitute"
  }
