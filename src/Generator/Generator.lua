project "CppSharp.Generator"

  kind "SharedLib"
  language "C#"
  location "."

  files   { "**.cs", "**.bmp", "**.resx", "**.config", "**verbs.txt" }
  excludes { "Filter.cs" }

  links { "System", "System.Core", "CppSharp.AST" }

  configuration "vs*"
    links { "CppSharp.Parser" }

  configuration "not vs*"
    dependson { "CppSharp.CppParser" }
    links { "CppSharp.Parser.CSharp" }

  configuration '**verbs.txt'
    buildaction "Embed"
