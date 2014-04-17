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
  objdir (builddir .. "/obj/")
  targetdir (libdir)
  libdirs { libdir }
  debugdir (bindir)

  -- startproject "Generator"
  configuration "vs2013"
    framework "4.0"

  configuration "vs2012"
    framework "4.0"

  configuration "windows"
    defines { "WINDOWS" }
	
  configuration "x64"
    defines { "IS_64_BIT" }

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

    if string.starts(action, "vs") and os.is_windows() then
      include (srcdir .. "/Parser/Parser.lua")
    end