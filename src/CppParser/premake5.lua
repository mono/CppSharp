clang_msvc_flags =
{
  "/wd4146", "/wd4244", "/wd4800", "/wd4345",
  "/wd4355", "/wd4996", "/wd4624", "/wd4291",
  "/wd4251",
  "/wd4141", -- 'inline' : used more than once
}

if not (string.starts(action, "vs") and not os.is("windows")) then

project "CppSharp.CppParser"
  
  kind "SharedLib"
  language "C++"
  SetupNativeProject()
  rtti "Off"

  filter "action:vs*"
    buildoptions { clang_msvc_flags }

  if os.getenv("APPVEYOR") then
    linkoptions { "/ignore:4099" } -- LNK4099: linking object as if no debug info
  end

  filter "system:linux"
    defines { "_GLIBCXX_USE_CXX11_ABI=0" }

  filter {}
  
  files
  {
    "*.h",
    "*.cpp",
    "*.lua"
  }

  SearchLLVM()
  SetupLLVMIncludes()
  SetupLLVMLibs()
  CopyClangIncludes()
  
  filter {}

project "Std-symbols"

  kind "SharedLib"
  language "C++"
  SetupNativeProject()
  rtti "Off"

  filter { "action:vs*" }
    buildoptions { clang_msvc_flags }

  filter {}

  if os.istarget("windows") then
      files { "Bindings/CSharp/i686-pc-win32-msvc/Std-symbols.cpp" }
  elseif os.istarget("macosx") then
      local file = io.popen("lipo -info `which mono`")
      local output = file:read('*all')
      if string.find(output, "x86_64") then
        files { "Bindings/CSharp/x86_64-apple-darwin12.4.0/Std-symbols.cpp" }
      else
        files { "Bindings/CSharp/i686-apple-darwin12.4.0/Std-symbols.cpp" }
      end

  elseif os.istarget("linux") then
      files { "Bindings/CSharp/x86_64-linux-gnu/Std-symbols.cpp" }
  else
      print "Unknown architecture"
  end

  filter {}

end
