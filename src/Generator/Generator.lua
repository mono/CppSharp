project "Generator"

  kind "ConsoleApp"
  language "C#"
  location "."

  files   { "**.cs", "**.bmp", "**.resx", "**.config" }
  excludes { "Filter.cs" }
  
  links { "System", "System.Core", "Bridge", "Parser" }
