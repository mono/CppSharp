using CppSharp.Utils;
using NUnit.Framework;

namespace CppSharp.Generator.Tests
{
    [TestFixture]
    public class ParseELFTest
    {
        [Test]
        public void TestParseELF()
        {
            var driverOptions = new DriverOptions();
            driverOptions.addLibraryDirs(GeneratorTest.GetTestsDirectory("Native"));
            driverOptions.Libraries.Add("ls");
            var driver = new Driver(driverOptions, new TextDiagnosticPrinter());
            Assert.IsTrue(driver.ParseLibraries());
            var dependencies = driver.Symbols.Libraries[0].Dependencies;
            Assert.AreEqual("libselinux.so.1", dependencies[0]);
            Assert.AreEqual("librt.so.1", dependencies[1]);
            Assert.AreEqual("libacl.so.1", dependencies[2]);
            Assert.AreEqual("libc.so.6", dependencies[3]);
        }
    }
}
