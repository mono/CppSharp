function SetupWrapper(name)
  project(name .. ".CSharp")
    SetupManagedTestProject()

    dependson { name .. ".Native", "NamespacesDerived.Gen" }

    SetupTestGeneratorBuildEvent("NamespacesDerived")

    files
    {
      path.join(gendir, name .. ".cs"),
    }

    linktable = { "CppSharp.Runtime" }

    links(linktable)
end

group "Tests/Namespaces"
  SetupTestNativeProject("NamespacesBase")
  SetupWrapper("NamespacesBase")