clang_msvc_flags =
{
  "/wd4146", "/wd4244", "/wd4800", "/wd4345",
  "/wd4355", "/wd4996", "/wd4624", "/wd4291",
  "/wd4251"
}

if not (string.starts(action, "vs") and not os.is_windows()) then

project "CppSharp.CppParser"
  
  kind "SharedLib"
  language "C++"
  SetupNativeProject()
  flags { common_flags }
  flags { "NoRTTI" }

  configuration "vs*"
    buildoptions { clang_msvc_flags }
    files { "VSLookup.cpp" }    

  configuration "*"
  
  files
  {
    "*.h",
    "*.cpp",
    "*.lua"
  }
  
  SetupLLVMIncludes()
  SetupLLVMLibs()
  
  configuration "*"

end

include ("Bindings")
