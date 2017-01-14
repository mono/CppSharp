project "CppSharp.Generator"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"

  files   { "**.cs", "**verbs.txt", path.join(depsdir, "InjectModuleInitializer", "**.cs")  }
  excludes { "Filter.cs" }

  libdirs 
  {
    depsdir .. "/Mono.Cecil"
  }

  links
  {
  	"System",
  	"System.Core",
  	"CppSharp",
  	"CppSharp.AST",
  	"CppSharp.Parser",
  	"Mono.Cecil.dll",
  	"Mono.Cecil.Mdb.dll",
  	"Mono.Cecil.Pdb.dll"
  }

  SetupParser()

  configuration '**verbs.txt'
    buildaction "Embed"
