-- This module checks for the all the project dependencies.

newoption {
   trigger     = "arch",
   description = "Choose a particular architecture / bitness",
   allowed = {
      { "x86",  "x86 32-bits" },
      { "x64",  "x64 64-bits" },
   }
}

newoption {
   trigger     = "no-cxx11-abi",
   description = "disable cxx11 abi on gcc 4.9+"
}

explicit_target_architecture = _OPTIONS["arch"]

function is_64_bits_mono_runtime()
  result, errorcode = os.outputof("mono --version")
  local arch = string.match(result, "Architecture:%s*([%w]+)")
  return arch == "amd64"
end

function target_architecture()
  -- Default to 32-bit on Windows and Mono architecture otherwise.
  if explicit_target_architecture ~= nil then
    return explicit_target_architecture
  end
  if os.ishost("windows") then return "x86" end
  return is_64_bits_mono_runtime() and "x64" or "x86"
end

if not _OPTIONS["arch"] then
  _OPTIONS["arch"] = target_architecture()
end

action = _ACTION or ""

basedir = path.getdirectory(_PREMAKE_COMMAND)
depsdir = path.getabsolute("../deps");
srcdir = path.getabsolute("../src");
incdir = path.getabsolute("../include");
bindir = path.getabsolute("../bin");
examplesdir = path.getabsolute("../examples");
testsdir = path.getabsolute("../tests");

builddir = path.getabsolute("./" .. action);
if _ARGS[1] then
    builddir = path.getabsolute("./" .. _ARGS[1]);
end

objsdir = path.join(builddir, "obj", "%{cfg.buildcfg}_%{cfg.platform}");
libdir = path.join(builddir, "lib", "%{cfg.buildcfg}_%{cfg.platform}");
gendir = path.join(builddir, "gen");

msvc_buildflags = { "/wd4267" }

msvc_cpp_defines = { }

generate_build_config = true

function string.starts(str, start)
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

  filter { "system:macosx", "language:C++" }
    buildoptions { gcc_buildflags, "-stdlib=libc++" }
    links { "c++" }

  filter { "system:not windows", "language:C++" }
    buildoptions { "-fpermissive -std=c++11" }
  
  -- OS-specific options
  
  filter { "system:windows" }
    defines { "WIN32", "_WINDOWS" }
  
  filter {}

  if os.istarget("linux") then
    if not UseCxx11ABI() then
      defines { "_GLIBCXX_USE_CXX11_ABI=0" }
    end
  end
end

function SetupManagedProject()
  language "C#"
  location ("%{wks.location}/projects")
  buildoptions {"/platform:".._OPTIONS["arch"]}

  dotnetframework "4.6"

  if not os.istarget("macosx") then
    filter { "action:vs*" }
      location "."
    filter {}
  end

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

function GccVersion()
  local compiler = os.getenv("CXX")
  if compiler == nil then
    compiler = "gcc"
  end
  local out = os.outputof(compiler.." -v")
  local version = string.match(out, "gcc version [0-9\\.]+")
  return string.sub(version, 13)
end

function UseCxx11ABI()
  if os.istarget("linux") and GccVersion() >= '4.9.0' and _OPTIONS["no-cxx11-abi"] == nil then
    return true
  end
  return false
end