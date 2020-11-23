project "CppSharp.Generator.Tests"

  kind "SharedLib"
  SetupManagedProject()

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
    "System.CodeDom:4.7.0",
    "Microsoft.CSharp:4.7.0",
    "Microsoft.NET.Test.Sdk:16.8.0",
    "NUnit:3.11.0",
    "NUnit3TestAdapter:3.17.0",
  }