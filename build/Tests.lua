-- Tests/examples helpers

function SetupExampleProject()
  SetupNativeProjects()
  location (path.join(builddir, "deps"))
end

function SetupTestProject(name, file, lib)
  SetupTestGeneratorProject(name)
  SetupTestNativeProject(name)  
  SetupTestProjectsCSharp(name, file, lib)
  SetupTestProjectsCLI(name, file, lib)
end

function SetupTestCSharp(name)
  SetupTestGeneratorProject(name)
  SetupTestNativeProject(name)
  SetupTestProjectsCSharp(name)
end

function SetupManagedTestProject()
    kind "SharedLib"
    language "C#"  
    flags { "Unsafe" }

    local c = configuration "vs*"
      location "."
    configuration(c)
end

function SetupTestGeneratorProject(name)
  project(name .. ".Gen")
    SetupManagedTestProject()
    kind "ConsoleApp"
    
    files { name .. ".cs" }

    dependson { name .. ".Native" }

    links
    {
      "CppSharp.AST",
      "CppSharp.Generator",
      "CppSharp.Parser",
    }
end

function SetupTestGeneratorBuildEvent(name)
  local exePath = SafePath("$(TargetDir)" .. name .. ".Gen.exe")
  prebuildcommands { exePath }
end

function SetupTestNativeProject(name)
  project(name .. ".Native")

    SetupNativeProject()

    kind "SharedLib"
    language "C++"

    flags { common_flags }
    files { "**.h", "**.cpp" }
end

function LinkNUnit()
  libdirs
  {
    depsdir .. "/NUnit",
    depsdir .. "/NSubstitute"
  }

  links
  {
    "NUnit.Framework",
    "NSubstitute"
  }
end

function SetupTestProjectsCSharp(name, file, lib)
  project(name .. ".CSharp")
    SetupManagedTestProject()

    dependson { name .. ".Gen", name .. ".Native" }
    SetupTestGeneratorBuildEvent(name)

    files
    {
      path.join(gendir, name, name .. ".cs"),
    }

    links { "CppSharp.Runtime" }

  project(name .. ".Tests.CSharp")
    SetupManagedTestProject()

    files { name .. ".Tests.cs" }
    links { name .. ".CSharp" }
    dependson { name .. ".Native" }

    LinkNUnit()
    links { "CppSharp.Runtime" }
end

function SetupTestProjectsCLI(name, file, lib)
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
    links { name .. ".CLI" }
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