using CppSharp;
using CppSharp.Passes;
using NUnit.Framework;

namespace Generator.Tests.Passes
{
    [TestFixture]
    public class TestPasses : HeaderTestFixture
    {
        private PassBuilder<TranslationUnitPass> passBuilder;

        [TestFixtureSetUp]
        public void Init()
        {
        }

        [SetUp]
        public void Setup()
        {
            ParseLibrary("Passes.h");
            passBuilder = new PassBuilder<TranslationUnitPass>(Driver);
        }

        [Test]
        public void TestCheckFlagEnumsPass()
        {
            var @enum = AstContext.Enum("FlagEnum");
            Assert.IsFalse(@enum.IsFlags);

            var @enum2 = AstContext.Enum("FlagEnum2");
            Assert.IsFalse(@enum2.IsFlags);

            passBuilder.AddPass(new CheckFlagEnumsPass());
            passBuilder.RunPasses(pass => pass.VisitLibrary(AstContext));

            Assert.IsTrue(@enum.IsFlags);
            Assert.IsFalse(@enum2.IsFlags);
        }

        [Test]
        public void TestFunctionToInstancePass()
        {
            var c = AstContext.Class("Foo");

            Assert.IsNull(c.Method("Start"));

            passBuilder.AddPass( new FunctionToInstanceMethodPass());
            passBuilder.RunPasses(pass => pass.VisitLibrary(AstContext));

            Assert.IsNotNull(c.Method("Start"));
        }

        [Test]
        public void TestFunctionToStaticPass()
        {
            var c = AstContext.Class("Foo");

            Assert.IsFalse(AstContext.Function("FooStart").ExplicityIgnored);
            Assert.IsNull(c.Method("Start"));

            passBuilder.AddPass(new FunctionToStaticMethodPass());
            passBuilder.RunPasses(pass => pass.VisitLibrary(AstContext));

            Assert.IsTrue(AstContext.Function("FooStart").ExplicityIgnored);
            Assert.IsNotNull(c.Method("Start"));
        }

        [Test]
        public void TestCaseRenamePass()
        {
            var c = AstContext.Class("TestRename");

            var method = c.Method("lowerCaseMethod");
            var field = c.Field("lowerCaseField");

            passBuilder.RenameDeclsUpperCase(RenameTargets.Any);
            passBuilder.RunPasses(pass => pass.VisitLibrary(AstContext));

            Assert.That(method.Name, Is.EqualTo("LowerCaseMethod"));
            Assert.That(field.Name, Is.EqualTo("LowerCaseField"));
        }

        [Test]
        public void TestCleanEnumItemNames()
        {
            AstContext.GenerateEnumFromMacros("TestEnumItemName", "TEST_ENUM_ITEM_NAME_(.*)");

            var @enum = AstContext.Enum("TestEnumItemName");
            Assert.IsNotNull(@enum);

            passBuilder.RemovePrefix("TEST_ENUM_ITEM_NAME_", RenameTargets.EnumItem);
            passBuilder.AddPass(new CleanInvalidDeclNamesPass());
            passBuilder.RunPasses(pass => pass.VisitLibrary(AstContext));

            Assert.That(@enum.Items[0].Name, Is.EqualTo("_0"));
        }

        [Test]
        public void TestStructInheritance()
        {

        }
    }
}
