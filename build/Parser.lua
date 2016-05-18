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
  SetupCSharpParser()
end