-- Examples helpers

function SetupExampleProject()
  SetupNativeProjects()
  location (path.join(builddir, "deps"))
end

function SetupTestGeneratorProject(name, file)
  project(name)

    kind "ConsoleApp"
    language "C#"
    location "."
    debugdir(path.join(examplesdir, name))

    files { file }

    links
    {
      path.join(depsdir, "cxxi", "build", action, "lib", "Bridge"),
      path.join(depsdir, "cxxi", "build", action, "lib", "Generator"),
    }
end

function SetupTestNativeProject(name, file)
  project(name)

    SetupNativeProject()
    kind "SharedLib"
    language "C++"

    flags { common_flags }

    files { file }
end

function SetupTestProject(name, file, lib)
  project(name)

    kind "ConsoleApp"
    language "C#"
    location "."

    files { file }

    links { lib }
end

function IncludeExamples()
  print("Searching for examples...")
  IncludeDir(examplesdir)
end

function IncludeTests()
  print("Searching for tests...")
  IncludeDir(testsdir)
end