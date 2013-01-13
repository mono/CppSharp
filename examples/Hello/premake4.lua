common_flags = {  }         -- Otherwise /clr won't be enabled - bug ?

solution "Hello"
    platforms { "x32" }
    configurations { "Debug", "Release" }
	
	objdir (  "./obj/" )
	targetdir ("./bin/")
	debugdir ( "./bin/")
	
	configuration "Debug"
		defines { "DEBUG" }
	
	configuration "Release"
		defines { "NDEBUG" }
		flags { "Optimize" }

project "Hello"
	kind     "SharedLib"
	language "C++"
    location "."
    platforms { "x32" }

	flags { common_flags, "Managed" }

	configuration "*"
		buildoptions { common_msvc_copts, "/clr" }

    files { "**.h", "**.cpp", "./*.lua" }

    configuration "hello.h"
	    buildrule {
		    description = "Compiling $(InputFile)...",
		    commands = { 
			    '..\\..\\bin\\generator.exe -ns=CppCsBind -outdir=CppCsBind -I. hello.h', 
		    },
		    outputs = { "CppCsBind\\hello.cpp" }
	    }


project "SayHello"
	kind     "ConsoleApp"
	language "C#"
    location "."
    platforms { "x32" }

    files { "**.cs", "./*.lua" }

	links { "Hello" }


