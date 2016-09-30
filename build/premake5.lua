-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

config = {}

dofile "Helpers.lua"
dofile "LLVM.lua"

solution "CppSharp"

  configurations { "Debug", "Release" }
  platforms { "x32", "x64" }

  characterset "Unicode"
  symbols "On"
  
  location (builddir)
  objdir (objsdir)
  targetdir (libdir)
  debugdir (bindir)

  configuration "windows"
    defines { "WINDOWS" }
	
  configuration {}

  group "Libraries"
    include (srcdir .. "/Core")
    include (srcdir .. "/AST")
    include (srcdir .. "/CppParser")
    include (srcdir .. "/CppParser/Bindings")
    include (srcdir .. "/CppParser/ParserGen")
    include (srcdir .. "/Parser")    
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
