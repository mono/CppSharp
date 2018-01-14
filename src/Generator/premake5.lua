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
  	"System",
  	"System.Core",
    "Microsoft.CSharp",
  	"CppSharp",
  	"CppSharp.AST",
  	"CppSharp.Parser"
  }

  SetupParser()

  filter { 'files:**verbs.txt' }
    buildaction "Embed"

  filter {}