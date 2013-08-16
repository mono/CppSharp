-- Examples helpers

function SetupExampleProject()
  SetupNativeProjects()
  location (path.join(builddir, "deps"))
end

function SetupTestProject(name)
  SetupTestGeneratorProject(name)
  SetupTestNativeProject(name)
  SetupTestProjects(name)
end

function SetupTestGeneratorProject(name)
  project(name .. ".Gen")

    kind "ConsoleApp"
    language "C#"
    location "."

    files { name .. ".cs" }

    dependson { name .. ".Native" }

    links
    {
      "CppSharp.AST",
      "CppSharp.Generator",
      "CppSharp.Parser",
    }
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

function SetupTestGen(name)
  local exePath = SafePath("$(TargetDir)" .. name .. ".Gen.exe")
  prebuildcommands { exePath }
end

function SetupTestProjects(name, file, lib)
  project(name .. ".CSharp")

    kind "SharedLib"
    language "C#"
    location "."
    flags { "Unsafe" }
    
    dependson { name .. ".Gen" }
    SetupTestGen(name)

    files
    {
      path.join(gendir, name, name .. ".cs"),
    }

  project(name .. ".CLI")

    kind "SharedLib"
    language "C++"
    flags { "Managed" }

    dependson { name .. ".Gen" }
    SetupTestGen(name)

    files
    {
      path.join(gendir, name, name .. ".cpp"),
      path.join(gendir, name, name .. ".h"),
    }

    includedirs { path.join(testsdir, name), incdir }
    links { name .. ".Native" }

  project(name .. ".Tests.CSharp")

    kind "SharedLib"
    language "C#"
    location "."

    files { name .. ".Tests.cs" }
    links { name .. ".CSharp" }
    dependson { name .. ".Native" }

    LinkNUnit()

  project(name .. ".Tests.CLI")

    kind "SharedLib"
    language "C#"
    location "."

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