-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

dofile "Helpers.lua"
dofile "Tests.lua"

solution "CppSharp"

  configurations { "Debug", "Release" }
  platforms { "x32" }
  flags { common_flags }
  
  location (builddir)
  objdir (builddir .. "/obj/")
  targetdir (libdir)
  libdirs { libdir }
  debugdir (bindir)

  -- startproject "Generator"
  
  configuration "Release"
    flags { "Optimize" }

  configuration "vs2012"
    framework "4.0"

  configuration {}
    
  group "Examples"
    IncludeExamples()
  
  group "Tests"
    IncludeTests()
  
  group "Libraries"
    include (srcdir .. "/AST/AST.lua")
    include (srcdir .. "/Generator/Generator.lua")
    include (srcdir .. "/Generator.Tests/Generator.Tests.lua")
    include (srcdir .. "/Parser/Parser.lua")
    include (srcdir .. "/Runtime/Runtime.lua")
    --include (srcdir .. "/CppParser/CppParser.lua")
    --include (srcdir .. "/ParserBridge/ParserBridge.lua")
    --include (srcdir .. "/ParserGen/ParserGen.lua")    

