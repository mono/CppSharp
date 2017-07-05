project "CppSharp.Runtime"

  SetupManagedProject()

  kind "SharedLib"
  clr "Unsafe"

  files   { "**.cs" }
  vpaths { ["*"] = "*" }
 
  links { "System" }

  filter { "action:vs*" }
  	defines { "MSVC" }

  filter { "system:macosx" }
  	defines { "LIBCXX" }
