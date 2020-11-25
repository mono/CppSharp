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

rootdir = path.getabsolute("../")
srcdir = path.join(rootdir, "src");
incdir = path.join(rootdir, "include");
examplesdir = path.join(rootdir, "examples");
testsdir = path.join(rootdir, "tests");
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
    cppdialect "C++14"
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
  language "C#"
  location "."
  filter {}
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
  local version = string.match(out, "gcc version [0-9\\.]+")
  if version == nil then
    version = string.match(out, "clang version [0-9\\.]+")
  end
  return string.sub(version, 13)
end

function UseCxx11ABI()
  if os.istarget("linux") and GccVersion() >= '4.9.0' and _OPTIONS["no-cxx11-abi"] == nil then
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