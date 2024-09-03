local cppsharp_qjs_runtime = "../../src/Generator/Generators/QuickJS/Runtime"

workspace "qjs"
    configurations { "debug", "release" }
    location "gen"
    symbols "On"
    optimize "Off"

    project "test"
        kind "SharedLib"
        language "C++"
        cppdialect "C++11"
        files
        {
            "gen/**.cpp",
            cppsharp_qjs_runtime .. "/*.cpp",
            cppsharp_qjs_runtime .. "/*.c"
        }
        includedirs
        {
            "runtime",
            cppsharp_qjs_runtime,
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
            targetextension ("so")
