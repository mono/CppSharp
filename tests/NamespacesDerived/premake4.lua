group "Tests/Namespaces"
  SetupTestGeneratorProject("NamespacesDerived", "NamespacesBase")
  SetupTestNativeProject("NamespacesDerived",    "NamespacesBase")
  SetupTestProjectsCSharp("NamespacesDerived",   "NamespacesBase")