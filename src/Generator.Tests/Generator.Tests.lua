project "CppSharp.Generator.Tests"

  kind "SharedLib"
  language "C#"
  location "."

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
    "NUnit.Framework",
    "NSubstitute"
  }
