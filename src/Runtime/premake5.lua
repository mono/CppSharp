project "CppSharp.Runtime"

  SetupManagedProject()

  kind "SharedLib"
  clr "Unsafe"

  files   { "**.cs" }
  vpaths { ["*"] = "*" }
 
  filter { "action:not netcore"}
    links { "System" }

  filter { "action:vs*" }
  	defines { "MSVC" }

  filter { "system:macosx" }
  	defines { "LIBCXX" }
