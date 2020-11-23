-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

config = {}

include "Helpers.lua"
include "LLVM.lua"

workspace "CppSharp"

  configurations { _OPTIONS["configuration"] }
  platforms { target_architecture() }
  dotnetframework "netcoreapp3.1"
  enabledefaultcompileitems "true"
  characterset "Unicode"
  symbols "On"

  location (actionbuilddir)
  objdir (prjobjdir)
  targetdir (bindircfg)
  debugdir (bindircfg)

  filter "action:vs*"
    location (rootdir)

  filter "system:windows"
    defines { "WINDOWS" }
  filter {}

  matches = os.matchfiles(path.join(rootdir, "*"))
  for _, file in ipairs(matches) do
    if not string.endswith(file, ".sln") then
      workspacefiles(file)
    end
  end

  group "Libraries"
    if EnableNativeProjects() then
      include (srcdir .. "/CppParser")
    end
    if EnabledManagedProjects() then
      include (srcdir .. "/Core")
      include (srcdir .. "/AST")
      --include (srcdir .. "/ASTViewer")
      include (srcdir .. "/CppParser/Bindings")
      include (srcdir .. "/CppParser/Bootstrap")
      include (srcdir .. "/CppParser/ParserGen")
      include (srcdir .. "/Parser")
      include (srcdir .. "/CLI")
      include (srcdir .. "/Generator")
      include (srcdir .. "/Generator.Tests")
      include (srcdir .. "/Runtime")
    end

if not _OPTIONS["disable-tests"] then
  dofile "Tests.lua"

  group "Tests"
      IncludeTests()
end

if not _OPTIONS["disable-tests"] then
  if string.starts(action, "vs") then

  group "Examples"
    IncludeExamples()
  
  end
end
