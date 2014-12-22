-- Tests/examples helpers

function SetupExampleProject()
  kind "ConsoleApp"
  language "C#"  
  debugdir "."
  
  files { "**.cs", "./*.lua" }
  links { "CppSharp.AST", "CppSharp.Generator" }

  SetupManagedProject()
  SetupParser()
end

function SetupTestProject(name, file, lib)
  SetupTestGeneratorProject(name)
  SetupTestNativeProject(name)  
  SetupTestProjectsCSharp(name)
  SetupTestProjectsCLI(name)
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
    kind "SharedLib"
    language "C#"  
    flags { "Unsafe" }
    SetupManagedProject()
end

function SetupTestGeneratorProject(name)
  project(name .. ".Gen")
    SetupManagedTestProject()
    kind "ConsoleApp"
    
    files { name .. ".cs" }

    dependson { name .. ".Native" }

    links
    {
      "System.Core",
      "CppSharp.AST",
      "CppSharp.Generator",
      "CppSharp.Generator.Tests"
    }

    SetupParser()
end

function SetupTestGeneratorBuildEvent(name)
  if string.starts(action, "vs") then
    local exePath = SafePath("$(TargetDir)" .. name .. ".Gen.exe")
    prebuildcommands { exePath }
  else
    local exePath = SafePath("%{cfg.buildtarget.directory}/" .. name .. ".Gen.exe")
    prebuildcommands { "mono --debug " .. exePath }
  end
end

function SetupTestNativeProject(name, depends)
  if string.starts(action, "vs") and not os.is_windows() then
    return
  end

  project(name .. ".Native")

    SetupNativeProject()

    kind "SharedLib"
    language "C++"

    flags { common_flags }
    files { "**.h", "**.cpp" }

    if depends ~= nil then
      links { depends }
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

function SetupTestProjectsCSharp(name, depends)
  project(name .. ".CSharp")
    SetupManagedTestProject()

    dependson { name .. ".Gen", name .. ".Native" }
    SetupTestGeneratorBuildEvent(name)

    files
    {
      path.join(gendir, name, name .. ".cs"),
    }

    linktable = { "CppSharp.Runtime" }

    if depends ~= nil then
      table.insert(linktable, depends)
    end

    links(linktable)

  project(name .. ".Tests.CSharp")
    SetupManagedTestProject()

    files { name .. ".Tests.cs" }
    links { name .. ".CSharp", "CppSharp.Generator.Tests" }
    dependson { name .. ".Native" }

    LinkNUnit()
    links { "CppSharp.Runtime" }
end

function SetupTestProjectsCLI(name)
  if not os.is_windows() then
    return
  end

  project(name .. ".CLI")
    SetupNativeProject()

    kind "SharedLib"
    language "C++"
    flags { "Managed" }

    dependson { name .. ".Gen", name .. ".Native" }
    SetupTestGeneratorBuildEvent(name)

    files
    {
      path.join(gendir, name, name .. ".cpp"),
      path.join(gendir, name, name .. ".h"),
    }

    includedirs { path.join(testsdir, name), incdir }
    links { name .. ".Native" }    

  project(name .. ".Tests.CLI")
    SetupManagedTestProject()

    files { name .. ".Tests.cs" }
    links { name .. ".CLI", "CppSharp.Generator.Tests" }
    dependson { name .. ".Native" }

    LinkNUnit()
end

function IncludeExamples()
  print("Searching for examples...")
  IncludeDir(examplesdir)
end

function IncludeTests()
  print("Searching for tests...")
  IncludeDir(testsdir)
end
