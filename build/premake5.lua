-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

config = {}

dofile "Helpers.lua"
dofile "LLVM.lua"

solution "CppSharp"

  buildconfig = io.open("../BuildConfig.cs", "w+")
  io.output(buildconfig)
  io.write("namespace CppSharp.Parser", "\n{\n    ")
  io.write("public static class BuildConfig", "\n    {\n        ")
  io.write("public const string Choice = \"" .. action .. "\";\n")
  io.write("    }\n}")
  io.close(buildconfig)

  configurations { "Debug", "Release" }
  platforms { target_architecture() }

  characterset "Unicode"
  symbols "On"
  
  location (builddir)
  objdir (objsdir)
  targetdir (libdir)
  debugdir (bindir)

  filter "system:windows"
    defines { "WINDOWS" }
	
  filter {}

  group "Libraries"
    include (srcdir .. "/Core")
    include (srcdir .. "/AST")
    include (srcdir .. "/CppParser")
    include (srcdir .. "/CppParser/Bindings")
    include (srcdir .. "/CppParser/ParserGen")
    include (srcdir .. "/Parser")
    include (srcdir .. "/CLI")
    include (srcdir .. "/Generator")
    include (srcdir .. "/Generator.Tests")
    include (srcdir .. "/Runtime")

  dofile "Tests.lua"

  group "Tests"
      IncludeTests()

  if string.starts(action, "vs") then

  group "Examples"
    IncludeExamples()
  
  end
