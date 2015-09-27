-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

config = {}

dofile "Helpers.lua"
dofile "Tests.lua"
dofile "LLVM.lua"
dofile "Parser.lua"

solution "CppSharp"

  configurations { "Debug", "Release" }
  platforms { "x32", "x64" }
  flags { common_flags }
  
  location (builddir)
  objdir (path.join(builddir, "obj"))
  targetdir (libdir)
  debugdir (bindir)

  if action == "vs2015" then

  configuration "vs2015"
    framework "4.6"

  end

  configuration "vs2013"
    framework "4.0"

  configuration "vs2012"
    framework "4.0"

  configuration "windows"
    defines { "WINDOWS" }
	
  configuration {}
    
  if string.starts(action, "vs") then

  group "Examples"
    IncludeExamples()
  
  end
  
  group "Tests"
      IncludeTests()
      
  group "Libraries"
    include (srcdir .. "/Core")
    include (srcdir .. "/AST/AST.lua")
    include (srcdir .. "/Generator/Generator.lua")
    include (srcdir .. "/Generator.Tests/Generator.Tests.lua")
    include (srcdir .. "/Runtime/Runtime.lua")
    include (srcdir .. "/CppParser")
