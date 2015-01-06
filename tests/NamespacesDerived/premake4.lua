group "Tests/Namespaces"
  SetupTestGeneratorProject("NamespacesDerived")
  SetupTestNativeProject("NamespacesDerived", "NamespacesBase.Native")
  SetupTestProjectsCSharp("NamespacesDerived", "NamespacesBase.CSharp")