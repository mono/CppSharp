project "CppSharp.Parser.Bootstrap"

  kind "ConsoleApp"
  language "C#"
  SetupManagedProject()
  debugdir "."

  files { "Bootstrap.cs", "*.lua" }
  links { "CppSharp", "CppSharp.AST", "CppSharp.Generator", "System.Core" }

  SetupParser()

project "CppSharp.Parser.BootstrapBindings"
  
  kind "SharedLib"
  language "C#"
  SetupManagedProject()
  
  dependson { "CppSharp.CppParser" }
  flags { common_flags, "Unsafe" }

  files
  {
    "Dummy.cs", "**.lua"
  }

  links { "CppSharp.Runtime" }
