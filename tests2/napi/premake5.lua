workspace "test"
    configurations {"Debug", "Release"}
    location "gen"
    symbols "On"
    optimize "Off"
    project "tests"
        kind "SharedLib"
        files {"gen/**.cpp"}
        includedirs {".."}
        targetprefix ""
        targetextension ".node"
