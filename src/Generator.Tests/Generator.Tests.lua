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
  
  links
  {
    "System",
    "System.Core",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser",
    "NUnit.Framework",
    "NSubstitute"
  }
