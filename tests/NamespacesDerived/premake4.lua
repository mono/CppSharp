group "Tests/Namespaces"
  SetupTestNativeProject("NamespacesDerived", "NamespacesBase")

  if not EnabledManagedProjects() then
    return
  end

  SetupTestGeneratorProject("NamespacesDerived")
  SetupTestProjectsCSharp("NamespacesDerived", "NamespacesBase")

  project("NamespacesDerived.Tests.CSharp")
    links { "NamespacesBase.CSharp" }