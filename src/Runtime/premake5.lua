project "CppSharp.Runtime"

  SetupManagedProject()

  kind "SharedLib"
  clr "Unsafe"

  files   { "**.cs" }
  links { "System" }

  configuration "vs*"
  	defines { "MSVC" }

  configuration "macosx"
  	defines { "LIBCXX" }
