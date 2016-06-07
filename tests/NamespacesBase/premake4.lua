function SetupWrapper(name)
  project(name .. ".CSharp")
    SetupManagedTestProject()

    dependson { name .. ".Native", "NamespacesDerived.Gen" }
	local runtimeExe = os.is("windows") and "" or "mono --debug "
	if string.starts(action, "vs") then
	  local exePath = SafePath("$(TargetDir)NamespacesDerived.Gen.exe")
	  prebuildcommands { runtimeExe .. exePath }
	else
	  local exePath = SafePath("%{cfg.buildtarget.directory}/NamespacesDerived.Gen.exe")
	  prebuildcommands { runtimeExe .. exePath }
	end

    files
    {
      path.join(gendir, "NamespacesDerived", name .. ".cs"),
    }

    linktable = { "CppSharp.Runtime" }

    links(linktable)
end

group "Tests/Namespaces"
  SetupTestNativeProject("NamespacesBase")
  SetupWrapper("NamespacesBase")