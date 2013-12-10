project "CppSharp.Runtime"

  kind "SharedLib"
  language "C#"
  flags { "Unsafe" }

  files   { "**.cs" }
  links { "System" }

  configuration "vs*"
  	location "."
  	defines { "MSVC" }

  configuration "macosx"
  	defines { "LIBCXX" }
