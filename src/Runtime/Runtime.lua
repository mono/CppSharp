project "Runtime"

  kind "SharedLib"
  language "C#"
  location "."

  files   { "**.cs" }
  
  links { "System" }
