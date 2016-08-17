project "CppSharp.Runtime"

  kind "SharedLib"
  SetupManagedProject()
  flags { "Unsafe" }

  files   { "**.cs" }
  links { "System" }

  configuration "vs*"
  	defines { "MSVC" }

  configuration "macosx"
  	defines { "LIBCXX" }
