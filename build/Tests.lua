-- Tests/examples helpers

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

function SetupTestCLI(name)
  SetupTestGeneratorProject(name)
  SetupTestNativeProject(name)
  SetupTestProjectsCLI(name)
end

function SetupManagedTestProject()
    SetupManagedProject()
    kind "SharedLib"
    language "C#"  
    clr "Unsafe"
end

function SetupTestGeneratorProject(name, depends)
  project(name .. ".Gen")
    SetupManagedTestProject()
    kind "ConsoleApp"
    
    files { name .. ".cs" }
    vpaths { ["*"] = "*" }

    dependson { name .. ".Native" }

    linktable = {
	  "System",
      "System.Core",
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
end

local function get_mono_exe()
  if target_architecture() == "x64" then
    local _, errorcode = os.outputof("mono64")
    return errorcode ~= 127 and "mono64" or "mono"
  end
  return "mono"
end

function SetupTestGeneratorBuildEvent(name)
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
  if string.starts(action, "vs") and not os.ishost("windows") then
    return
  end

  project(name .. ".Native")

    SetupNativeProject()

    kind "SharedLib"
    language "C++"

    files { "**.h", "**.cpp" }
    vpaths { ["*"] = "*" }

    if depends ~= nil then
      links { depends .. ".Native" }
    end
end

function LinkNUnit()
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

    dependson { name .. ".Gen", name .. ".Native" }
    SetupTestGeneratorBuildEvent(name)

    files
    {
      path.join(gendir, name, nm .. ".cs"),
      path.join(gendir, name, str .. ".cs")
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

    links { name .. ".CSharp", "CppSharp.Generator.Tests" }
    dependson { name .. ".Native" }

    LinkNUnit()
    links { "CppSharp.Runtime" }
end

function SetupTestProjectsCLI(name, extraFiles, suffix)
  if not os.ishost("windows") then
    return
  end

  project(name .. ".CLI")
    SetupNativeProject()

    kind "SharedLib"
    language "C++"
    clr "On"

    dependson { name .. ".Gen", name .. ".Native" }
    SetupTestGeneratorBuildEvent(name)

    if (suffix ~= nil) then 
      nm = name .. suffix
    else
      nm = name
    end

    files
    {
      path.join(gendir, name, nm .. ".cpp"),
      path.join(gendir, name, nm .. ".h")
    }
    if extraFiles ~= nil then
      for _, file in pairs(extraFiles) do
        if suffix ~= nil then
          file = file .. suffix  
        end
        files { path.join(gendir, name, file .. ".cpp") }
        files { path.join(gendir, name, file .. ".h") }
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
