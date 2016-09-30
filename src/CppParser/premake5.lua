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

  configuration "vs*"
    buildoptions { clang_msvc_flags }

  if os.getenv("APPVEYOR") then
    linkoptions { "/ignore:4099" } -- LNK4099: linking object as if no debug info
  end    

  configuration "*"
  
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
  
  configuration "*"

end