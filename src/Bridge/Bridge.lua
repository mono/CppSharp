project "Bridge"

	kind     "SharedLib"
	language "C#"
        location "."
	files    { "*.cs" }
        platforms { "x32" }

	configuration "Debug"
		targetdir "../../bin"
		
	configuration "Release"
		targetdir "../../bin"


	if _ACTION == "clean" then
		os.rmdir("lib")
	end

