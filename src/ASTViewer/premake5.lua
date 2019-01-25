require("premake-qt/qt")
local qt = premake.extensions.qt

project "CppSharp.ASTViewer"
    SetupNativeProject()
    kind "ConsoleApp"
    systemversion("latest")
    cppdialect "C++17"

    qt.enable()

    filter { "system:linux" }
        buildoptions { "-fPIC" }
        links { "pthread" }
        qtincludepath "/usr/include/x86_64-linux-gnu/qt5"
        qtlibpath "/usr/lib/x86_64-linux-gnu"
        qtbinpath "/usr/lib/qt5/bin"

    filter { "system:windows" }
        qtpath "C:\\Qt\\5.12.0\\msvc2017"

    filter {}

    qtmodules { "core", "gui", "widgets" }
    qtprefix "Qt5"
    files { "**.h", "**.cpp", "**.ui", "**.qrc" }

    SetupLLVMIncludes()
    SetupLLVMLibs()

    filter { "action:vs*" }
        buildoptions { "/wd4141", "/wd4146", "/wd4996" }
