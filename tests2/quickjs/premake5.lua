qjs_inc_dir = path.getabsolute("../../deps/txiki.js/deps/quickjs/include")
qjs_lib_dir = path.getabsolute("../../deps/txiki.js/deps/quickjs/include")

workspace "qjs"
    configurations { "release" }
    location "gen"
    symbols "On"
    optimize "Off"

    project "test"
        kind "SharedLib"
        language "C++"
        files {"gen/**.cpp"}
        includedirs { qjs_inc_dir, ".." }
        libdirs { qjs_lib_dir }
        filter { "kind:StaticLib" }
            links { "quickjs" }
        filter { "kind:SharedLib" }
            defines { "JS_SHARED_LIBRARY" }
        filter { "kind:SharedLib", "system:macosx" }
            linkoptions { "-undefined dynamic_lookup" }
