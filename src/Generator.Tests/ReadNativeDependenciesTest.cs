using System.Collections.Generic;
using CppSharp.Utils;
using NUnit.Framework;
using CppSharp.Parser;

namespace CppSharp.Generator.Tests
{
    [TestFixture]
    public class ReadNativeDependenciesTest
    {
        [Test]
        public void TestReadDependenciesWindows()
        {
            var dependencies = GetDependencies("libexpat-windows");
            Assert.AreEqual("KERNEL32.dll", dependencies[0]);
            Assert.AreEqual("msvcrt.dll", dependencies[1]);
            Assert.AreEqual("USER32.dll", dependencies[2]);

        }

        [Test]
        public void TestReadDependenciesLinux()
        {
            var dependencies = GetDependencies("libexpat-linux");
            Assert.AreEqual("libc.so.6", dependencies[0]);
        }

        [Test]
        public void TestReadDependenciesOSX()
        {
            var dependencies = GetDependencies("libexpat.1.dylib");
            Assert.AreEqual("libexpat.1.dylib", dependencies[0]);
            Assert.AreEqual("libSystem.B.dylib", dependencies[1]);
        }

        private static IList<string> GetDependencies(string library)
        {
            var parserOptions = new ParserOptions();
            parserOptions.AddLibraryDirs(GeneratorTest.GetTestsDirectory("Native"));
            var driverOptions = new DriverOptions();
            var module = driverOptions.AddModule("Test");
            module.Libraries.Add(library);
            var driver = new Driver(driverOptions)
            {
                ParserOptions = parserOptions
            };
            driver.Setup();
            Assert.IsTrue(driver.ParseLibraries());
            var dependencies = driver.Context.Symbols.Libraries[0].Dependencies;
            return dependencies;
        }
    }
}
