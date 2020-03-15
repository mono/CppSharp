clang_msvc_flags =
{
  "/wd4146", "/wd4244", "/wd4800", "/wd4345",
  "/wd4355", "/wd4996", "/wd4624", "/wd4291",
  "/wd4251",
  "/wd4141", -- 'inline' : used more than once
}

if EnableNativeProjects() then

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
      if is_64_bits_mono_runtime() or _OPTIONS["arch"] == "x64" then
        files { "Bindings/CSharp/x86_64-apple-darwin12.4.0/Std-symbols.cpp" }
      else
        files { "Bindings/CSharp/i686-apple-darwin12.4.0/Std-symbols.cpp" }
      end
  elseif os.istarget("linux") then
      local abi = ""
      if UseCxx11ABI() then
          abi = "-cxx11abi"
      end
      files { "Bindings/CSharp/x86_64-linux-gnu"..abi.."/Std-symbols.cpp" }
  else
      print "Unknown architecture"
  end

  filter {}

end
