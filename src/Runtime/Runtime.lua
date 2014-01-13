project "CppSharp.Runtime"

  kind "SharedLib"
  language "C#"
  flags { "Unsafe" }

  SetupManagedProject()

  files   { "**.cs" }
  links { "System" }

  configuration "vs*"
  	location "."
  	defines { "MSVC" }

  configuration "macosx"
  	defines { "LIBCXX" }
