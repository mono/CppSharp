project "Generator"
	kind     "ConsoleApp"
	language "C#"
        location "."
	files   { "**.cs", "**.bmp", "**.resx", "**.config" }
        excludes { "Filter.cs" }
	links { "Bridge", "System", "System.Core", "Parser" }
        platforms { "x32" }

	configuration "Debug"
		targetdir "../../bin"
		
	configuration "Release"
		targetdir "../../bin"

