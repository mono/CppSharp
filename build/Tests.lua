-- Tests/examples helpers

require('vstudio')

function SetupExampleProject()
  kind "ConsoleApp"
  language "C#"
  debugdir "."

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
  enabledefaultcompileitems "false"
  kind "SharedLib"
  language "C#"
  clr "Unsafe"
  files { "*.lua" }
end

function SetupTestGeneratorProject(name, depends)
  if not EnabledManagedProjects() then
    return
  end
  project(name .. ".Gen")
    SetupManagedTestProject()
    SetupParser()
    kind "ConsoleApp"
    enabledefaultnoneitems "false"

    files { name .. ".cs" }
    dependson { name .. ".Native" }

    links {
      "CppSharp",
      "CppSharp.AST",
      "CppSharp.Generator",
      "CppSharp.Generator.Tests",
      "CppSharp.Parser"
    }

    if depends ~= nil then
      links { depends .. ".Gen" }
    end

    local command = os.ishost("windows") and "type nul >" or "touch"
    postbuildcommands { command .. " " .. SafePath(path.join(objsdir, name .. ".Native", "timestamp.cs")) }
    postbuildcommands { command .. " " .. SafePath(path.join(objsdir, name .. ".Native", "timestamp.cpp")) }
end

function SetupTestGeneratorBuildEvent(name)
  local cmd = os.ishost("windows") and "" or "dotnet "
  local ext = os.ishost("windows") and "exe" or "dll"
  prebuildcommands { cmd .. SafePath(path.join("%{cfg.buildtarget.directory}", name .. ".Gen." .. ext)) }
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
    defines { "DLL_EXPORT" }

    if depends ~= nil then
      links { depends .. ".Native" }
    end

    local command = os.ishost("windows") and "type nul >" or "touch"
    postbuildcommands { command .. " " .. SafePath(path.join(objsdir, name .. ".Native", "timestamp.cs")) }
    postbuildcommands { command .. " " .. SafePath(path.join(objsdir, name .. ".Native", "timestamp.cpp")) }
end

function SetupTestProjectsCSharp(name, depends, extraFiles, suffix)
  if not EnabledManagedProjects() then
    return
  end
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
    enabledefaultnoneitems "false"

    files
    {
      path.join(gendir, name, nm .. ".cs"),
      path.join(gendir, name, str .. ".cs")
    }

    links { "CppSharp.Runtime" }

    if depends ~= nil then
      links { depends .. ".CSharp" }
    end

  project(name .. ".Tests.CSharp")
    SetupManagedTestProject()

    enabledefaultnoneitems "false"
    files { name .. ".Tests.cs" }

    links { name .. ".CSharp", "CppSharp.Generator.Tests", "CppSharp.Runtime" }
    nuget { "Microsoft.NET.Test.Sdk:16.8.0" }
    dependson { name .. ".Native" }
end

function SetupTestProjectsCLI(name, extraFiles, suffix)
  if not EnabledCLIProjects() then
    return
  end

  project(name .. ".CLI")
    SetupNativeProject()

    enabledefaultcompileitems "false"
    enabledefaultnoneitems "false"
    kind "SharedLib"
    language "C++"
    clr "NetCore"

    dependson { name .. ".Gen", name .. ".Native", "CppSharp.Generator" }
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

    includedirs { path.join(testsdir, name), incdir }
    links { name .. ".Native" }    
    files { path.join(objsdir, name .. ".Native", "timestamp.cpp") }

  project(name .. ".Tests.CLI")
    SetupManagedTestProject()
    enabledefaultnoneitems "false"
    files { name .. ".Tests.cs" }

    links { name .. ".CLI", "CppSharp.Generator.Tests" }
    dependson { name .. ".Native" }
    nuget { "Microsoft.NET.Test.Sdk:16.8.0" }
end

function IncludeExamples()
  --print("Searching for examples...")
  IncludeDir(examplesdir)
end

function IncludeTests()
  --print("Searching for tests...")
  IncludeDir(testsdir)
end
