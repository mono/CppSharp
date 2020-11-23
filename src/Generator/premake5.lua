project "CppSharp.Generator"

  SetupManagedProject()
  SetupParser()

  kind "SharedLib"
  language "C#"
  dependson { "Std-symbols" }

  links
  {
  	"CppSharp",
  	"CppSharp.AST",
  	"CppSharp.Parser"
  }

  nuget
  {
    "System.CodeDom:4.7.0",
    "Microsoft.CSharp:4.7.0"
  }

  files { "**verbs.txt" }
  filter { 'files:**verbs.txt' }
    buildaction "Embed"

  filter {}