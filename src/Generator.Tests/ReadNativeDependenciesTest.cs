using System.Collections.Generic;
using CppSharp.Utils;
using NUnit.Framework;
using CppSharp.AST;
using System.IO;

namespace CppSharp.Generator.Tests
{
    [TestFixture]
    public class ReadNativeDependenciesTest
    {
        [Test]
        public void TestReadDependenciesWindows()
        {
            IList<string> dependencies = GetDependencies("windows", "libexpat");
            Assert.AreEqual("KERNEL32.dll", dependencies[0]);
            Assert.AreEqual("msvcrt.dll", dependencies[1]);
            Assert.AreEqual("USER32.dll", dependencies[2]);
        }

        [Test]
        public void TestReadDependenciesLinux()
        {
            IList<string> dependencies = GetDependencies("linux", "libexpat");
            Assert.AreEqual("libc.so.6", dependencies[0]);
        }

        [Test]
        public void TestReadDependenciesMacOS()
        {
            IList<string> dependencies = GetDependencies("macos", "libexpat");
            Assert.AreEqual("libexpat.1.dylib", dependencies[0]);
            Assert.AreEqual("libSystem.B.dylib", dependencies[1]);
        }

        private static IList<string> GetDependencies(string dir, string library)
        {
            var driverOptions = new DriverOptions();
            Module module = driverOptions.AddModule("Test");
            module.LibraryDirs.Add(Path.Combine(GeneratorTest.GetTestsDirectory("Native"), dir));
            module.Libraries.Add(library);
            using (var driver = new Driver(driverOptions))
            {
                driver.Setup();
                Assert.IsTrue(driver.ParseLibraries());
                return driver.Context.Symbols.Libraries[0].Dependencies;
            }
        }
    }
}