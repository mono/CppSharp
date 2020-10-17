project "CppSharp.Generator"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"

  files   { "**.cs", "**verbs.txt" }
  excludes { "Filter.cs" }
  vpaths { ["*"] = "*" }

  dependson { "Std-symbols" }

  links
  {
  	"CppSharp",
  	"CppSharp.AST",
  	"CppSharp.Parser"
  }

  SetupParser()

  nuget
  {
    "System.CodeDom:4.7.0",
    "Microsoft.CSharp:4.7.0"
  }

  filter { 'files:**verbs.txt' }
    buildaction "Embed"

  filter {}