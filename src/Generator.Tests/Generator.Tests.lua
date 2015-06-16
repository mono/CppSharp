project "CppSharp.Generator.Tests"

  kind "SharedLib"
  SetupManagedProject()

  files { "**.cs" }
  
  libdirs 
  {
    depsdir .. "/NUnit.2.6.2/lib",
    depsdir .. "/NSubstitute.1.4.3.0/lib/NET40"
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
