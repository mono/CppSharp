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

  local copy = os.is_windows() and "xcopy /Q /E /Y /I" or "cp -rf";
  local headers = path.getabsolute(path.join(LLVMRootDir, "lib/"))
  if os.isdir(path.join(headers, "clang")) then
    postbuildcommands { copy .. " " .. headers .. " %{cfg.targetdir}" }
  end

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
  
  SetupLLVMIncludes()
  SetupLLVMLibs()
  
  configuration "*"

end

include ("Bindings")
include ("Bootstrap")
