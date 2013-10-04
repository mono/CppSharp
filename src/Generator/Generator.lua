project "CppSharp.Generator"

  kind "SharedLib"
  language "C#"
  location "."

  files   { "**.cs", "**.bmp", "**.resx", "**.config", "**verbs.txt" }
  excludes { "Filter.cs" }
  
  links { "System", "System.Core", "CppSharp.AST", "CppSharp.Parser" }
  
  configuration '**verbs.txt'
    buildaction "Embed"
