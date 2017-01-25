project "CppSharp.Generator"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"

  files   { "**.cs", "**verbs.txt" }
  excludes { "Filter.cs" }

  links
  {
  	"System",
  	"System.Core",
  	"CppSharp",
  	"CppSharp.AST",
  	"CppSharp.Parser"
  }

  SetupParser()

  configuration '**verbs.txt'
    buildaction "Embed"
