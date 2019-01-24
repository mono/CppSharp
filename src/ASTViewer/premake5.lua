require("premake-qt/qt")

-- this line is optional, but it avoids writting premake.extensions.qt to
-- call the plugin's methods.
local qt = premake.extensions.qt

project "CppSharp.ASTViewer"
    SetupNativeProject()
    kind "ConsoleApp"
    systemversion("latest")
    cppdialect "C++17"
    qt.enable()
    qtpath "C:\\Qt\\5.12.0\\msvc2017"
    qtmodules { "core", "gui", "widgets" }
    qtprefix "Qt5"
    files { "**.h", "**.cpp", "**.ui", "**.qrc" }
    SetupLLVMIncludes()
    SetupLLVMLibs()
    filter { "action:vs*" }
        buildoptions { "/wd4141", "/wd4146", "/wd4996" }