
workspace "emscripten"
    configurations { "debug", "release" }
    location "gen"
    symbols "On"
    optimize "Off"

    project "test"
        kind "SharedLib"
        language "C++"
        files
        {
            "gen/**.cpp",
        }
        includedirs
        {
            "..",
            "../../include"
        }
        linkoptions { "--bind -sENVIRONMENT=node -sMODULARIZE=1 -sEXPORT_ALL -sEXPORT_ES6=1 -sUSE_ES6_IMPORT_META=1" }
        targetextension ".mjs"