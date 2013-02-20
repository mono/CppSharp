using Cxxi;
using Cxxi.Passes;
using NUnit.Framework;

namespace Generator.Tests.Passes
{
    [TestFixture]
    public class TestPasses : HeaderTestFixture
    {
        private PassBuilder passBuilder;

        [TestFixtureSetUp]
        public void Init()
        {
            ParseLibrary("Passes.h");
        }

        [SetUp]
        public void Setup()
        {
            passBuilder = new PassBuilder(library);
        }

        [Test]
        public void TestCheckFlagEnumsPass()
        {
            var @enum = library.Enum("FlagEnum");
            Assert.IsFalse(@enum.IsFlags);

            var @enum2 = library.Enum("FlagEnum2");
            Assert.IsFalse(@enum2.IsFlags);

            passBuilder.CheckFlagEnums();
            passBuilder.RunPasses();

            Assert.IsFalse(@enum.IsFlags);
            Assert.IsTrue(@enum2.IsFlags);
        }

        [Test]
        public void TestFunctionToInstancePass()
        {
            var c = library.Class("C");

            Assert.IsNull(c.Methods.Find(m => m.Name == "DoSomethingC"));

            passBuilder.FunctionToInstanceMethod();
            passBuilder.RunPasses();

            Assert.IsNotNull(c.Methods.Find(m => m.Name == "DoSomethingC"));
        }

        [Test]
        public void TestCaseRenamePass()
        {
            var c = library.Class("TestRename");

            var method = c.Method("lowerCaseMethod");
            var field = c.Field("lowerCaseField");

            passBuilder.RenameDeclsUpperCase(RenameTargets.Any);
            passBuilder.RunPasses();

            Assert.That(method.Name, Is.EqualTo("LowerCaseMethod"));
            Assert.That(field.Name, Is.EqualTo("LowerCaseField"));
        }
    }
}
