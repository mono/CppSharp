-- This module checks for the all the project dependencies.

action = _ACTION or ""

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

libdir = path.join(builddir, "lib", "%{cfg.buildcfg}_%{cfg.platform}");
gendir = path.join(builddir, "gen");

common_flags = { "Unicode", "Symbols" }
msvc_buildflags = { "/wd4267" }
gcc_buildflags = { "-std=c++11 -fpermissive" }

msvc_cpp_defines = { }

function os.is_osx()
  return os.is("macosx")
end

function os.is_windows()
  return os.is("windows")
end

function os.is_linux()
  return os.is("linux")
end

function string.starts(str, start)
   return string.sub(str, 1, string.len(start)) == start
end

function SafePath(path)
  return "\"" .. path .. "\""
end

function SetupNativeProject()
  location (path.join(builddir, "projects"))

  local c = configuration "Debug"
    defines { "DEBUG" }
    
  configuration "Release"
    defines { "NDEBUG" }
    optimize "On"
    
  -- Compiler-specific options
  
  configuration "vs*"
    buildoptions { msvc_buildflags }
    defines { msvc_cpp_defines }

  configuration { "gmake" }
    buildoptions { gcc_buildflags }
    
  configuration { "macosx" }
    buildoptions { gcc_buildflags, "-stdlib=libc++" }
    links { "c++" }
  
  -- OS-specific options
  
  configuration "Windows"
    defines { "WIN32", "_WINDOWS" }
  
  configuration(c)
end

function SetupManagedProject()
  language "C#"
  location (path.join(builddir, "projects"))

  if not os.is_osx() then
    local c = configuration { "vs*" }
      location "."
    configuration(c)
  end
end

function IncludeDir(dir)
  local deps = os.matchdirs(dir .. "/*")
  
  for i,dep in ipairs(deps) do
    local fp = path.join(dep, "premake4.lua")
    fp = path.join(os.getcwd(), fp)
    
    if os.isfile(fp) then
      print(string.format(" including %s", dep))
      include(dep)
    end
  end
end

function StaticLinksOpt(libnames)
  local cc = configuration()
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
