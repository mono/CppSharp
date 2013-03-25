group "Hello"
  SetupTestGeneratorProject("Hello", "Hello.cs")
  SetupTestNativeProject("Hello.Native", { "Hello.cpp", "Hello.h" })
  SetupTestProjects("Hello", { "Hello.Tests.cs"})


