project "CppSharp.Generator"

  kind "SharedLib"
  language "C#"
  location "."

  SetupManagedProject()

  files   { "**.cs", "**verbs.txt" }
  excludes { "Filter.cs" }

  links { "System", "System.Core", "CppSharp", "CppSharp.AST" }

  SetupParser()

  configuration '**verbs.txt'
    buildaction "Embed"
