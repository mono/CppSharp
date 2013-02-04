project "Generator.Tests"

  kind "SharedLib"
  language "C#"
  location "."

  files { "**.cs" }
  
  libdirs 
  {
    depsdir .. "/NUnit",
    depsdir .. "/NSubstitute"
  }
  
  links
  {
    "System",
    "System.Core",
    "Bridge",
    "Generator",
    "Parser",
    "NUnit.Framework",
    "NSubstitute"
  }
