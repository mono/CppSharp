function SetupCLIParser()
  links { "CppSharp.Parser.CLI" }
end

function SetupCSharpParser()
  links
  {
    "CppSharp.Parser.CSharp",
    "CppSharp.Runtime"
  }
end

function SetupParser()
  if string.match(action, "vs*") and os.is_windows() then
    SetupCLIParser()
  else
    SetupCSharpParser()
  end
end