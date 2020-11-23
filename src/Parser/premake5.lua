local buildconfig = path.join(actionbuilddir, "BuildConfig.cs")

local function GenerateBuildConfig()
  print("Generating CppSharp build configuration file 'BuildConfig.cs'")

  local file = io.open(buildconfig, "w+")
  file:write("namespace CppSharp.Parser", "\n{\n    ")
  file:write("public static class BuildConfig", "\n    {\n        ")
  file:write("public const string Choice = \"" .. _ACTION .. "\";\n")
  file:write("    }\n}")
  file:close()
end

if generate_build_config == true and _ACTION then
  GenerateBuildConfig()
end

project "CppSharp.Parser"

  SetupManagedProject()
  SetupParser()

  kind "SharedLib"
  language "C#"
  clr "Unsafe"

  if generate_build_config == true then
    files { buildconfig }
    removecompilefiles { "BuildConfig.cs" }
  end

  links
  {
    "CppSharp",
    "CppSharp.AST",
    "CppSharp.Runtime"
  }
