project "CppSharp.Runtime"

  SetupManagedProject()

  kind "SharedLib"
  clr "Unsafe"

  files   { "**.cs" }
  excludes { "obj/**" }
  vpaths { ["*"] = "*" }
 
  filter { "action:not netcore"}
    links { "System" }

  filter { "action:vs*" }
  	defines { "MSVC" }

  filter { "system:macosx" }
  	defines { "LIBCXX" }
