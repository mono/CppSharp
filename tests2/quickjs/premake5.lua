local qjs_dir = path.getabsolute("../../deps/quickjs")
local runtime = "../../src/Generator/Generators/QuickJS/Runtime"

workspace "qjs"
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
            runtime .. "/*.cpp",
            runtime .. "/*.c"
        }
        includedirs
        {
            qjs_dir,
            runtime,
            "..",
            "../../include"
        }
        libdirs { qjs_lib_dir }
        filter { "kind:StaticLib" }
            links { "quickjs" }
        filter { "kind:SharedLib" }
            defines { "JS_SHARED_LIBRARY" }
        filter { "kind:SharedLib", "system:macosx" }
            linkoptions { "-undefined dynamic_lookup" }
