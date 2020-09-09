-- This is the starting point of the build scripts for the project.
-- It defines the common build settings that all the projects share
-- and calls the build scripts of all the sub-projects.

config = {}

include "Helpers.lua"
include "LLVM.lua"

workspace "CppSharp"

  configurations { "Debug", "Release" }
  platforms { "x64" }

  characterset "Unicode"
  symbols "On"

  location (builddir)
  objdir (objsdir)
  targetdir (libdir)
  debugdir (bindir)

  filter "system:windows"
    defines { "WINDOWS" }

  filter {}

  group "Libraries"
    include (srcdir .. "/Core")
    include (srcdir .. "/AST")
    --include (srcdir .. "/ASTViewer")
    include (srcdir .. "/CppParser")
    include (srcdir .. "/CppParser/Bindings")
    include (srcdir .. "/CppParser/Bootstrap")
    include (srcdir .. "/CppParser/ParserGen")
    include (srcdir .. "/Parser")
    include (srcdir .. "/CLI")
    include (srcdir .. "/Generator")
    include (srcdir .. "/Generator.Tests")
    include (srcdir .. "/Runtime")

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
