project "CppSharp.Runtime"

  SetupManagedProject()

  kind "SharedLib"
  clr "Unsafe"

  filter { "toolset:msc*" }
  	defines { "MSVC" }

  filter { "system:macosx" }
  	defines { "LIBCXX" }
