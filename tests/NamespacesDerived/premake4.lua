group "Tests/Namespaces"
  SetupTestGeneratorProject("NamespacesDerived", "NamespacesBase")
  SetupTestNativeProject("NamespacesDerived",    "NamespacesBase")
  SetupTestProjectsCSharp("NamespacesDerived",   "NamespacesBase")
  
  project("NamespacesDerived.Tests.CSharp")
    links { "NamespacesBase.CSharp" }