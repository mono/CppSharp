-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

action = _ACTION or ""
common_flags = { "Unicode", "Symbols" }

common_msvc_copts = 
{ 
  "/wd4146", "/wd4244", "/wd4800", "/wd4345",
  "/wd4355", "/wd4996", "/wd4624", "/wd4291"
}

solution "Cxxi"

	configurations
	{ 
		"Debug",
		"Release"
	}
	
	objdir (  "../obj/" .. action)
	targetdir ("../bin/")
	debugdir ( "../bin/")
        platforms { "x32" }
	
	configuration "Debug"
		defines { "DEBUG" }
	
	configuration "Release"
		defines { "NDEBUG" }
		flags { "Optimize" }
	
	include "Parser.lua"
        include "../src/Bridge/Bridge.lua"
        include "../src/Generator/Generator.lua"

