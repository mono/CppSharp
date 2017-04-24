-- This module checks for the all the project dependencies.

newoption {
   trigger     = "arch",
   description = "Choose a particular architecture / bitness",
   allowed = {
      { "x86",  "x86 32-bits" },
      { "x64",  "x64 64-bits" },
   }
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
  if os.is("windows") then return "x86" end
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

function string.starts(str, start)
   return string.sub(str, 1, string.len(start)) == start
end

function SafePath(path)
  return "\"" .. path .. "\""
end

function SetupNativeProject()
  location ("%{wks.location}/projects")

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

  filter { "system:macosx", "language:C++" }
    buildoptions { gcc_buildflags, "-stdlib=libc++" }
    links { "c++" }

  filter { "system:not windows", "language:C++" }
    buildoptions { "-fpermissive -std=c++11" }
  
  -- OS-specific options
  
  configuration "Windows"
    defines { "WIN32", "_WINDOWS" }
  
  configuration(c)
end

function SetupManagedProject()
  language "C#"
  location ("%{wks.location}/projects")

  if not os.is("macosx") then
    local c = configuration { "vs*" }
      location "."
    configuration(c)
  end

  if action == "vs2017" then

        configuration "vs2017"
			framework "4.6"
    
    elseif action == "vs2015" then
        
        configuration "vs2015"
			framework "4.6"
    
    end

  configuration "vs2013"
    framework "4.5"

  configuration "vs2012"
    framework "4.5"

  configuration {}
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
