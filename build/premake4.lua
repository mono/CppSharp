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

solution "Cxxi2"

	configurations
	{ 
		"Debug",
		"Release"
	}
	
	location (action)
	objdir (action .. "/obj/")
	targetdir (action .. "/lib/")
	targetdir (action .. "/bin/")
	debugdir (action .. "/bin/")
	
	configuration "Debug"
		defines { "DEBUG" }
		targetsuffix "_d"
	
	configuration "Release"
		defines { "NDEBUG" }
		flags { "Optimize" }
	
	dofile "Parser.lua"