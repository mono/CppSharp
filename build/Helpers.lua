-- This module checks for the all the project dependencies.

newoption {
   trigger     = "arch",
   description = "Choose a particular architecture / bitness",
   allowed = {
      { "x86",  "x86 32-bits" },
      { "x64",  "x64 64-bits" },
      { "AnyCPU",  "Any CPU (.NET)" },
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

explicit_target_architecture = _OPTIONS["arch"]

function is_64_bits_mono_runtime()
  result, errorcode = os.outputof("mono --version")
  local arch = string.match(result, "Architecture:%s*([%w]+)")
  return arch == "amd64"
end

function target_architecture()
  if _ACTION == "netcore" then
    return "AnyCPU"
  end

  -- Default to 64-bit on Windows and Mono architecture otherwise.
  if explicit_target_architecture ~= nil then
    return explicit_target_architecture
  end
  if os.ishost("windows") then return "x64" end
  return is_64_bits_mono_runtime() and "x64" or "x86"
end

if not _OPTIONS["arch"] then
  _OPTIONS["arch"] = target_architecture()
end

-- Uncomment to enable Roslyn compiler.
--[[
premake.override(premake.tools.dotnet, "gettoolname", function(base, cfg, tool)
  if tool == "csc" then
    return "csc"
  end
  return base(cfg, tool)
end)
]]

basedir = path.getdirectory(_PREMAKE_COMMAND)
premake.path = premake.path .. ";" .. path.join(basedir, "modules")

depsdir = path.getabsolute("../deps");
srcdir = path.getabsolute("../src");
incdir = path.getabsolute("../include");
bindir = path.getabsolute("../bin");
examplesdir = path.getabsolute("../examples");
testsdir = path.getabsolute("../tests");

builddir = path.getabsolute("./" .. _ACTION);
if _ARGS[1] then
    builddir = path.getabsolute("./" .. _ARGS[1]);
end

if _ACTION ~= "netcore" then
  objsdir = path.join(builddir, "obj", "%{cfg.buildcfg}_%{cfg.platform}");
  libdir = path.join(builddir, "lib", "%{cfg.buildcfg}_%{cfg.platform}");
else
  objsdir = path.join(builddir, "obj", "%{cfg.buildcfg}");
  libdir = path.join(builddir, "lib", "%{cfg.buildcfg}");
end

gendir = path.join(builddir, "gen");

msvc_buildflags = { "/wd4267" }

msvc_cpp_defines = { }

generate_build_config = true

function string.starts(str, start)
   if str == nil then return end
   return string.sub(str, 1, string.len(start)) == start
end

function SafePath(path)
  return "\"" .. path .. "\""
end

function SetupNativeProject()
  location ("%{wks.location}/projects")

  filter { "configurations:Debug" }
    defines { "DEBUG" }
    
  filter { "configurations:Release" }
    defines { "NDEBUG" }
    optimize "On"
    
  -- Compiler-specific options
  
  filter { "action:vs*" }
    buildoptions { msvc_buildflags }
    defines { msvc_cpp_defines }

  filter { "system:linux" }
    buildoptions { gcc_buildflags }
    links { "stdc++" }

  filter { "toolset:clang", "system:not macosx" }
    linkoptions { "-fuse-ld=/usr/bin/ld.lld" }

  filter { "system:macosx", "language:C++" }
    buildoptions { gcc_buildflags, "-stdlib=libc++" }
    links { "c++" }

  filter { "system:not windows", "language:C++" }
    cppdialect "C++14"
    buildoptions { "-fpermissive" }
  
  -- OS-specific options
  
  filter { "system:windows" }
    defines { "WIN32", "_WINDOWS" }
  
  -- For context: https://github.com/premake/premake-core/issues/935
  filter {"system:windows", "action:vs*"}
    systemversion("latest")

  filter {}
end

function SetupManagedProject()
  language "C#"
  location ("%{wks.location}/projects")
  buildoptions {"/platform:".._OPTIONS["arch"]}

  dotnetframework "4.7"

  if not os.istarget("macosx") then
    filter { "action:vs* or netcore" }
      location "."
    filter {}
  end

  filter { "action:netcore" }
    dotnetframework "netstandard2.0"

  filter { "action:vs2013" }
    dotnetframework "4.5"

  filter { "action:vs2012" }
    dotnetframework "4.5"

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
  local compiler = os.getenv("CXX") or ""
  return string.match(compiler, "clang")
end

function GccVersion()
  local compiler = os.getenv("CXX")
  if compiler == nil then
    compiler = "gcc"
  end
  local out = os.outputof(compiler.." -v")
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
  if _ACTION == "netcore" then
    return false
  end

  if string.starts(_ACTION, "vs") and not os.ishost("windows") then
    return false
  end

  return true
end
