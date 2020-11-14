-- Tests/examples helpers

require('vstudio')
  
function disableFastUpToDateCheck(prj, cfg)
  premake.vstudio.vc2010.element("DisableFastUpToDateCheck", nil, "true")
end
  
premake.override(premake.vstudio.cs2005.elements, "projectProperties",
  function(oldfn, prj)
    local elements = oldfn(prj)
    if (string.endswith(prj.filename, ".CSharp") and
        not string.endswith(prj.filename, ".Tests.CSharp") and
        not string.endswith(prj.filename, ".Parser.CSharp")) then 
      elements = table.join(elements, {disableFastUpToDateCheck})
    end
    return elements
  end)

premake.override(premake.vstudio.vc2010.elements, "globals",
  function(oldfn, prj, cfg)
    local elements = oldfn(prj, cfg)
    if (string.endswith(prj.filename, ".CLI") and
        not string.endswith(prj.filename, ".Tests.CLI") and
        not string.endswith(prj.filename, ".Parser.CLI") and
        prj.filename ~= "CppSharp.CLI") then 
      elements = table.join(elements, {disableFastUpToDateCheck})
    end
    return elements
  end)

-- HACK: work around gmake(2) ignoring tokens such as %{cfg.buildcfg}
-- https://github.com/premake/premake-core/issues/1557
if not os.istarget("windows") then

  premake.override(premake.modules.gmake2, "csSources",
    function(_, prj)
      for cfg in premake.project.eachconfig(prj) do
        _x('ifeq ($(config),%s)', cfg.shortname)
        _p('SOURCES += \\')
        premake.modules.gmake2.cs.listsources(prj, function(node)
          local fcfg = premake.fileconfig.getconfig(node, cfg)
          local info = premake.tools.dotnet.fileinfo(fcfg)
          if info.action == "Compile" then
            return node.relpath
          end
        end)
        _p('')
        _p('endif')
        _p('')
      end
    end)

  premake.override(premake.modules.gmake2, "csResponseRules",
    function(_, prj)
      local toolset = premake.tools.dotnet
      local ext = premake.modules.gmake2.getmakefilename(prj, true)
      local makefile = path.getname(premake.filename(prj, ext))
      local response = premake.modules.gmake2.cs.getresponsefilename(prj)

      _p('$(RESPONSE): %s', makefile)
      _p('\t@echo Generating response file', prj.name)

      _p('ifeq (posix,$(SHELLTYPE))')
          _x('\t$(SILENT) rm -f $(RESPONSE)')
      _p('else')
          _x('\t$(SILENT) if exist $(RESPONSE) del %s', path.translate(response, '\\'))
      _p('endif')
       _p('')

      local sep = os.istarget("windows") and "\\" or "/"
      for cfg in premake.project.eachconfig(prj) do
        _x('ifeq ($(config),%s)', cfg.shortname)
        premake.modules.gmake2.cs.listsources(prj, function(node)
          local fcfg = premake.fileconfig.getconfig(node, cfg)
          local info = premake.tools.dotnet.fileinfo(fcfg)
          if info.action == "Compile" then
            _x('\t@echo %s >> $(RESPONSE)', path.translate(node.relpath, sep))
          end
        end)
        _p('endif')
        _p('')
      end
    end)

end

function SetupExampleProject()
  kind "ConsoleApp"
  language "C#"  
  debugdir "."
  
  files { "**.cs", "./*.lua" }
  links
  {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Generator",
    "CppSharp.Parser"
  }

  SetupManagedProject()
  SetupParser()
end

function SetupTestProject(name, extraFiles, suffix)
  SetupTestGeneratorProject(name)
  SetupTestNativeProject(name)
  SetupTestProjectsCSharp(name, nil, extraFiles, suffix)
  SetupTestProjectsCLI(name, extraFiles, suffix)
end

function SetupTestCSharp(name)
  SetupTestGeneratorProject(name)
  SetupTestNativeProject(name)
  SetupTestProjectsCSharp(name)
end

function SetupTestCLI(name, extraFiles, suffix)
  SetupTestGeneratorProject(name)
  SetupTestNativeProject(name)
  SetupTestProjectsCLI(name, extraFiles, suffix)
end

function SetupManagedTestProject()
    SetupManagedProject()
    kind "SharedLib"
    language "C#"  
    clr "Unsafe"

    filter { "action:not netcore" }
      links
      {
        "System.Core"
      }
end

function SetupTestGeneratorProject(name, depends)
  project(name .. ".Gen")
    SetupManagedTestProject()
    kind "ConsoleApp"
    
    files { name .. ".cs" }
    vpaths { ["*"] = "*" }

    dependson { name .. ".Native" }

    linktable = {
      "CppSharp",
      "CppSharp.AST",
      "CppSharp.Generator",
      "CppSharp.Generator.Tests",
      "CppSharp.Parser"
    }

    if depends ~= nil then
      table.insert(linktable, depends .. ".Gen")
    end

    links(linktable)

    SetupParser()

    filter { "action:netcore" }
      dotnetframework "netcoreapp2.0"

    filter { "action:not netcore" }
      links
      {
        "System",
        "System.Core"
      }
end

local function get_mono_exe()
  if target_architecture() == "x64" then
    local _, errorcode = os.outputof("mono64")
    return errorcode ~= 127 and "mono64" or "mono"
  end
  return "mono"
end

function SetupTestGeneratorBuildEvent(name)
  if _ACTION == "netcore" then
    return
  end
  local monoExe = get_mono_exe()
  local runtimeExe = os.ishost("windows") and "" or monoExe .. " --debug "
  if string.starts(action, "vs") then
    local exePath = SafePath("$(TargetDir)" .. name .. ".Gen.exe")
    prebuildcommands { runtimeExe .. exePath }
  else
    local exePath = SafePath("%{cfg.buildtarget.directory}/" .. name .. ".Gen.exe")
    prebuildcommands { runtimeExe .. exePath }
  end
end

function SetupTestNativeProject(name, depends)
  if not EnableNativeProjects() then
    return
  end

  project(name .. ".Native")

    SetupNativeProject()

    kind "SharedLib"
    language "C++"

    files { "**.h", "**.cpp" }
    vpaths { ["*"] = "*" }
    defines { "DLL_EXPORT" }

    if depends ~= nil then
      links { depends .. ".Native" }
    end
end

function LinkNUnit()
  local c = filter()
  
  filter { "action:not netcore"}
    libdirs
    {
      depsdir .. "/NUnit",
      depsdir .. "/NSubstitute"
    }

    links
    {
      "nunit.framework",
      "NSubstitute"
    }

  filter { "action:netcore"}
    nuget
    {
      "NUnit:3.11.0",
      "NSubstitute:4.0.0-rc1"
    }

  nuget
  {
    "NUnit3TestAdapter:3.17.0",
  }
    
  filter(c)
end

function SetupTestProjectsCSharp(name, depends, extraFiles, suffix)
    if suffix ~= nil then
      nm = name .. suffix 
      str = "Std" .. suffix
    else
      nm = name
      str = "Std"
    end
  project(name .. ".CSharp")
    SetupManagedTestProject()

    dependson { name .. ".Gen", name .. ".Native", "CppSharp.Generator" }
    SetupTestGeneratorBuildEvent(name)

    files
    {
      path.join(gendir, nm .. ".cs"),
      path.join(gendir, str .. ".cs")
    }
    vpaths { ["*"] = "*" }

    linktable = { "CppSharp.Runtime" }

    if depends ~= nil then
      table.insert(linktable, depends .. ".CSharp")
    end

    links(linktable)

  project(name .. ".Tests.CSharp")
    SetupManagedTestProject()

    files { name .. ".Tests.cs" }
    vpaths { ["*"] = "*" }

    links { name .. ".CSharp", "CppSharp.Generator.Tests", "CppSharp.Runtime" }
    dependson { name .. ".Native" }

    LinkNUnit()

    filter { "action:netcore" }
      dotnetframework "netcoreapp2.0"
end

function SetupTestProjectsCLI(name, extraFiles, suffix)
  if (not os.ishost("windows")) or (_ACTION == "netcore") then
    return
  end

  project(name .. ".CLI")
    SetupNativeProject()

    kind "SharedLib"
    language "C++"
    clr "On"

    dependson { name .. ".Gen", name .. ".Native", "CppSharp.Generator" }
    SetupTestGeneratorBuildEvent(name)

    if (suffix ~= nil) then 
      nm = name .. suffix
    else
      nm = name
    end

    files
    {
      path.join(gendir, nm .. ".cpp"),
      path.join(gendir, nm .. ".h")
    }
    if extraFiles ~= nil then
      for _, file in pairs(extraFiles) do
        if suffix ~= nil then
          file = file .. suffix  
        end
        files { path.join(gendir, file .. ".cpp") }
        files { path.join(gendir, file .. ".h") }
      end
    end
    vpaths { ["*"] = "*" }

    includedirs { path.join(testsdir, name), incdir }
    links { name .. ".Native" }    

  project(name .. ".Tests.CLI")
    SetupManagedTestProject()

    files { name .. ".Tests.cs" }
    vpaths { ["*"] = "*" }

    links { name .. ".CLI", "CppSharp.Generator.Tests" }
    dependson { name .. ".Native" }

    LinkNUnit()
end

function IncludeExamples()
  --print("Searching for examples...")
  IncludeDir(examplesdir)
end

function IncludeTests()
  --print("Searching for tests...")
  IncludeDir(testsdir)
end
