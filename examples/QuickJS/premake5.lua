qjs_inc_dir = path.getabsolute("../../deps/QuickJS")
qjs_lib_dir = path.getabsolute("../../deps/QuickJS")

workspace "qjs"
    configurations { "release" }

    project "test-native"
    kind "StaticLib"
    files { "*-native.cpp" }

    include "gen/js_premake5.lua"

    project "test"
        includedirs { "." }
        links { "test-native" }