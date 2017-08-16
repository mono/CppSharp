local buildconfig = path.join(builddir, "BuildConfig.cs")

local function GenerateBuildConfig()
  print("Generating CppSharp build configuration file 'BuildConfig.cs'")

  local file = io.open(buildconfig, "w+")
  file:write("namespace CppSharp.Parser", "\n{\n    ")
  file:write("public static class BuildConfig", "\n    {\n        ")
  file:write("public const string Choice = \"" .. _ACTION .. "\";\n")
  file:write("    }\n}")
  file:close()
end

if generate_build_config == true then
  GenerateBuildConfig()
end

project "CppSharp.Parser"

  SetupManagedProject()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"

  files { "**.cs" }
  removefiles { "BuildConfig.cs" }

  if generate_build_config == true then
    files { buildconfig }
  else
    files { "BuildConfig.cs" }
  end

  vpaths { ["*"] = "*" }

  links
  {
    "System",
    "System.Core",
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Runtime"
  }

  SetupParser()
