SetupTestGeneratorProject("Hello", "Hello.cs")
SetupTestNativeProject("Hello.Native", { "Hello.cpp", "Hello.h" })
SetupTestProject("Hello.Tests", { "Hello.Tests.cs"}, "Hello")




