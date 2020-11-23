function SetupWrapper(name)
  if not EnabledManagedProjects() then
    return
  end

  project(name .. ".CSharp")
    SetupManagedTestProject()

    dependson { name .. ".Native", "NamespacesDerived.Gen" }
    links { "CppSharp.Runtime" }
    SetupTestGeneratorBuildEvent("NamespacesDerived")

    files
    {
      path.join(gendir, "NamespacesDerived", name .. ".cs"),
    }
end

group "Tests/Namespaces"
  SetupTestNativeProject("NamespacesBase")
  SetupWrapper("NamespacesBase")