project "CppSharp.Generator"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"

  files   { "**.cs", "**verbs.txt" }
  excludes { "Filter.cs" }
  vpaths { ["*"] = "*" }

  links
  {
  	"CppSharp",
  	"CppSharp.AST",
  	"CppSharp.Parser"
  }

  SetupParser()

  filter { "action:netcore"}
    nuget
    {
      "System.CodeDom:4.5.0",
      "Microsoft.CSharp:4.5.0"
    }

  filter { "action:not netcore"}
    links
    {
      "System",
      "System.Core",
      "Microsoft.CSharp"
    }

  filter { 'files:**verbs.txt' }
    buildaction "Embed"

  filter {}