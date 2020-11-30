function SetupWrapper(name)
  if not EnabledManagedProjects() then
    return
  end

  SetupExternalManagedTestProject(name .. ".CSharp")
end

group "Tests/Namespaces"
  SetupTestNativeProject("NamespacesBase")
  SetupWrapper("NamespacesBase")