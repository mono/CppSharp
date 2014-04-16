newoption {
     trigger = "parser",
     description = "Controls which version of the parser is enabled.",
     value = "version",
     allowed = {
          { "cpp", "Cross-platform C++ parser."},
          { "cli", "VS-only C++/CLI parser."},
     }
}

function SetupCLIParser()
  local parser = _OPTIONS["parser"]
  if not parser or parser == "cli" then
    defines { "OLD_PARSER" }
    links { "CppSharp.Parser" }
  else
    links { "CppSharp.Parser.CLI" }
  end
end

function SetupCSharpParser()
  links
  {
    "CppSharp.Parser.CSharp",
    "CppSharp.Runtime"
  }
end

function SetupParser()
  if string.match(action, "vs*") then
    SetupCLIParser()
  else
    SetupCSharpParser()
  end
end