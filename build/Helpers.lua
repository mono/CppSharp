-- This module checks for the all the project dependencies.
require('vstudio')
require('premake/premake.fixes')
require('premake/premake.extensions')

newoption {
   trigger     = "arch",
   description = "Choose a particular architecture / bitness",
   default = "x64",
   allowed = {
      { "x86",  "x86 32-bits" },
      { "x64",  "x64 64-bits" },
   }
}

newoption {
   trigger     = "no-cxx11-abi",
   description = "disable C++-11 ABI on GCC 4.9+"
}

newoption {
   trigger = "disable-tests",
   description = "disable tests from being included"
}

newoption {
    trigger = "disable-examples",
    description = "disable examples from being included"
 }

newoption {
  trigger = "configuration",
  description = "Choose a configuration",
  default = "Release",
  allowed = {
    { "Release",  "Release" },
    { "Debug",  "Debug" },
 }
}

newoption {
  trigger = "config_only",
  description = "Only generate configuration file",
}

newoption {
  trigger = "target-framework",
  description = ".NET target framework version",
}

rootdir = path.getabsolute("../")
srcdir = path.join(rootdir, "src");
incdir = path.join(rootdir, "include");
examplesdir = path.join(rootdir, "examples");
testsdir = path.join(rootdir, "tests/dotnet");
builddir = path.join(rootdir, "build")
bindir = path.join(rootdir, "bin")
objsdir = path.join(builddir, "obj");
gendir = path.join(builddir, "gen");
actionbuilddir = path.join(builddir, _ACTION == "gmake2" and "gmake" or (_ACTION and _ACTION or ""));
bindircfg = path.join(bindir, "%{cfg.buildcfg}_%{cfg.platform}");
prjobjdir = path.join(objsdir, "%{prj.name}", "%{cfg.buildcfg}")

msvc_buildflags = { "/MP", "/wd4267" }
msvc_cpp_defines = { }
default_gcc_version = "9.0.0"
generate_build_config = true
premake.path = premake.path .. ";" .. path.join(builddir, "modules")

function string.isempty(s)
  return s == nil or s == ''
end

local function target_framework()
  local value =  _OPTIONS["target-framework"]
  return string.isempty(value) and "net6.0" or value
end

targetframework = target_framework()

function string.starts(str, start)
   if str == nil then return end
   return string.sub(str, 1, string.len(start)) == start
end

function SafePath(path)
  return "\"" .. path .. "\""
end

function target_architecture()
  return _OPTIONS["arch"]
end

function SetupNativeProject()
  location (path.join(actionbuilddir, "projects"))
  files { "*.lua" }
  cppdialect "c++17"

  if os.getenv("CPPSHARP_RELEASE") == "true" then
    symbols "off"
  end

  filter { "configurations:Debug" }
    defines { "DEBUG" }

  filter { "configurations:Release" }
    defines { "NDEBUG" }
    optimize "On"

  -- Compiler-specific options

  filter { "toolset:msc*" }
    buildoptions { msvc_buildflags }
    defines { msvc_cpp_defines }

  filter { "system:linux" }
    buildoptions { gcc_buildflags }
    links { "stdc++" }

  filter { "toolset:clang", "system:not macosx" }
    linkoptions { "-fuse-ld=/usr/bin/ld.lld" }

  filter { "toolset:clang" }
    buildoptions { "-fstandalone-debug" }

  filter { "toolset:clang", "language:C++", "system:macosx" }
    buildoptions { gcc_buildflags, "-stdlib=libc++" }
    links { "c++" }

  filter { "toolset:not msc*", "language:C++" }
    buildoptions { "-fpermissive" }
  
  -- OS-specific options
  
  filter { "system:windows" }
    defines { "WIN32", "_WINDOWS" }
  
  -- For context: https://github.com/premake/premake-core/issues/935
  filter {"system:windows", "toolset:msc*"}
    systemversion("latest")

  filter {}
end

function SetupManagedProject()
  kind "SharedLib"
  language "C#"
  location "."
  filter {}
end

function SetupExternalManagedProject(name)
  externalproject (name)
    SetupManagedProject()
end

function IncludeDir(dir)
  local deps = os.matchdirs(dir .. "/*")
  
  for i,dep in ipairs(deps) do
    local fp = path.join(dep, "premake5.lua")
    fp = path.join(os.getcwd(), fp)
    
    if os.isfile(fp) then
      include(dep)
      return
    end

    fp = path.join(dep, "premake4.lua")
    fp = path.join(os.getcwd(), fp)
    
    if os.isfile(fp) then
      --print(string.format(" including %s", dep))
      include(dep)
    end
  end
end

function StaticLinksOpt(libnames)
  local path = table.concat(cc.configset.libdirs, ";")

  local formats
  if os.is("windows") then
    formats = { "%s.lib" }
  else
    formats = { "lib%s.a" }
  end
  table.insert(formats, "%s");

  local existing_libnames = {}
  for _, libname in ipairs(libnames) do
    for _, fmt in ipairs(formats) do
      local name = string.format(fmt, libname)
      if os.pathsearch(name, path) then
        table.insert(existing_libnames, libname)
      end
    end
  end

  links(existing_libnames)
end

function UseClang()
  local cc = _OPTIONS.cc or ""
  local env = os.getenv("CXX") or ""
  return string.match(cc, "clang") or string.match(env, "clang")
end

function GccVersion()
  local compiler = os.getenv("CXX")
  if compiler == nil then
    compiler = "gcc"
  end
  local out = os.outputof(compiler.." -v")
  if out == nil then
    return default_gcc_version
  end
  local version = string.match(out, "gcc[ -][Vv]ersion [0-9\\.]+")
  if version ~= nil then
    return string.sub(version, 13)
  end
  version = string.match(out, "clang[ -][Vv]ersion [0-9\\.]+")
  return string.sub(version, 15)
end

function UseCxx11ABI()
  if os.istarget("linux") and premake.checkVersion(GccVersion(), '4.9.0') and _OPTIONS["no-cxx11-abi"] == nil then
    return true
  end
  return false
end

function EnableNativeProjects()
  return not (string.starts(_ACTION, "vs") and not os.ishost("windows"))
end

function EnabledManagedProjects()
  return string.starts(_ACTION, "vs")
end

function EnabledCLIProjects()
  return EnabledManagedProjects() and os.istarget("windows")
end

function AddPlatformSpecificFiles(folder, filename)

  if os.istarget("windows") then
    filter { "toolset:msc*", "architecture:x86_64" }
      files { path.join(folder, "x86_64-pc-win32-msvc", filename) }
    filter { "toolset:msc*", "architecture:x86" }
      files { path.join(folder, "i686-pc-win32-msvc", filename) }
  elseif os.istarget("macosx") then
    filter { "architecture:x86_64" }
      files { path.join(folder, "x86_64-apple-darwin12.4.0", filename) }
    filter {"architecture:x86" }
      files { path.join(folder, "i686-apple-darwin12.4.0", filename) }
  elseif os.istarget("linux") then
    filter { "architecture:x86_64" }
      files { path.join(folder, "x86_64-linux-gnu" .. (UseCxx11ABI() and "-cxx11abi" or ""), filename) }
  else
    print "Unknown architecture"
  end
end

function WriteConfigForMSBuild()
  local file = io.open("config.props", "w+")
  function writeProperty(name, value)
    file:write("    <" .. name .. ">" .. (value ~= nil and value or "") .. "</" .. name .. ">\n")
  end
  function writeBooleanProperty(name, value)
    writeProperty(name, value and "true" or "false")
  end

  file:write("<!-- GENERATED FILE -->\n")
  file:write("<Project>\n")
  file:write("  <PropertyGroup>\n")
  writeProperty("PlatformTarget", target_architecture())
  writeProperty("TargetFramework", targetframework)
  writeProperty("Configuration", _OPTIONS["configuration"])
  writeBooleanProperty("IsWindows", os.istarget("windows"))
  writeBooleanProperty("IsLinux", os.istarget("linux"))
  writeBooleanProperty("IsMacOSX", os.istarget("macosx")) 
  writeBooleanProperty("CI", os.getenv("CI") == "true")
  writeBooleanProperty("GenerateBuildConfig", generate_build_config == true)
  writeBooleanProperty("UseCXX11ABI", UseCxx11ABI())
  writeProperty("PremakeAction", _ACTION) 
  file:write("  </PropertyGroup>\n")
  file:write("</Project>")
  file:close()
end
