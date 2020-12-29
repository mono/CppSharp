-- Tests/examples helpers

require('vstudio')

function SetupExampleProject()
  kind "ConsoleApp"
  language "C#"
  debugdir "."

  links { "CppSharp.Parser" }

  SetupManagedProject()
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

function SetupExternalManagedTestProject(name)
  externalproject (name)
  SetupManagedTestProject()
end

function SetupTestGeneratorProject(name, depends)
  if EnabledManagedProjects() then
    SetupExternalManagedTestProject(name .. ".Gen")
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
    targetdir (path.join(gendir, name))

    files { "**.h", "**.cpp" }
    defines { "DLL_EXPORT" }

    if depends ~= nil then
      links { depends .. ".Native" }
    end
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
  
  if name ~= "NamespacesDerived" then
    SetupExternalManagedTestProject(name .. ".CSharp")
  end
  SetupExternalManagedTestProject(name .. ".Tests.CSharp")
end

function SetupTestProjectsCLI(name, extraFiles, suffix)
  if not EnabledCLIProjects() then
    return
  end

  project(name .. ".CLI")
    SetupNativeProject()

    kind "SharedLib"
    language "C++"
    clr "NetCore"
    targetdir (path.join(gendir, name))

    dependson { name .. ".Gen" }

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
    files { path.join(objsdir, name .. ".Native") }

  SetupExternalManagedTestProject(name .. ".Tests.CLI")
end

function IncludeExamples()
  --print("Searching for examples...")
  IncludeDir(examplesdir)
end

function IncludeTests()
  --print("Searching for tests...")
  IncludeDir(testsdir)
end
