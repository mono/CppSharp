if not EnabledManagedProjects() then
  return
end

SetupExternalManagedProject("CppSharp.Parser.CSharp")

CppSharpParserBindings = "CppSharp.Parser.CSharp"
