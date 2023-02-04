group "Tests/VTables"
  SetupTestCSharp("VTables")

  if EnableNativeProjects() then

    project("VTables.Native")
      rtti "On"

  end
