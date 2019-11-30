using CppSharp.AST;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using NUnit.Framework;
using System.Linq;

namespace CppSharp.Generator.Tests.Passes
{
    [TestFixture]
    public class TestPasses : ASTTestFixture
    {
        private PassBuilder<TranslationUnitPass> passBuilder;

        [SetUp]
        public void Setup()
        {
            ParseLibrary("Passes.h");
            passBuilder = new PassBuilder<TranslationUnitPass>(Driver.Context);
        }

        [Test]
        public void TestCheckFlagEnumsPass()
        {
            var @enum = AstContext.Enum("FlagEnum");
            Assert.IsFalse(@enum.IsFlags);

            var @enum2 = AstContext.Enum("FlagEnum2");
            Assert.IsFalse(@enum2.IsFlags);

            passBuilder.AddPass(new CheckFlagEnumsPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.IsTrue(@enum.IsFlags);
            Assert.IsFalse(@enum2.IsFlags);
        }

        [Test]
        public void TestFunctionToInstancePass()
        {
            var c = AstContext.Class("Foo");

            Assert.IsNull(c.Method("Start"));

            passBuilder.AddPass(new FunctionToInstanceMethodPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.IsNotNull(c.Method("Start"));
        }

        [Test]
        public void TestFunctionToStaticPass()
        {
            var c = AstContext.Class("Foo");

            Assert.IsTrue(AstContext.Function("FooStart").IsGenerated);
            Assert.IsNull(c.Method("Start"));

            passBuilder.AddPass(new FunctionToStaticMethodPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.IsFalse(AstContext.Function("FooStart").IsGenerated);
            Assert.IsNotNull(c.Method("Start"));
        }

        [Test]
        public void TestCleanCommentsPass()
        {
            var c = AstContext.FindClass("TestCommentsPass").FirstOrDefault();

            passBuilder.AddPass(new CleanCommentsPass());
            passBuilder.RunPasses(pass => pass.VisitDeclaration(c));

            var para = (ParagraphComment) c.Comment.FullComment.Blocks[0];
            var textGenerator = new TextGenerator();
            textGenerator.Print(para, CommentKind.BCPLSlash);

            Assert.That(textGenerator.StringBuilder.ToString().Trim(),
                Is.EqualTo("/// <summary>A simple test.</summary>"));
        }

        [Test]
        public void TestCaseRenamePass()
        {
            Type.TypePrinterDelegate += TypePrinterDelegate;

            var c = AstContext.Class("TestRename");

            passBuilder.AddPass(new FieldToPropertyPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var method = c.Method("lowerCaseMethod");
            var property = c.Properties.Find(p => p.Name == "lowerCaseField");

            passBuilder.RenameDeclsUpperCase(RenameTargets.Any);
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.That(method.Name, Is.EqualTo("LowerCaseMethod"));
            Assert.That(property.Name, Is.EqualTo("LowerCaseField"));

            Type.TypePrinterDelegate -= TypePrinterDelegate;
        }

        [Test]
        public void TestCleanEnumItemNames()
        {
            AstContext.GenerateEnumFromMacros("TestEnumItemName", "TEST_ENUM_ITEM_NAME_(.*)");

            var @enum = AstContext.Enum("TestEnumItemName");
            Assert.IsNotNull(@enum);

            // Testing the values read for the enum
            Assert.AreEqual(0, @enum.Items[0].Value); // Decimal literal
            Assert.AreEqual(1, @enum.Items[1].Value); // Hex literal
            Assert.AreEqual(2, @enum.Items[2].Value); // Hex literal with suffix

            passBuilder.RemovePrefix("TEST_ENUM_ITEM_NAME_", RenameTargets.EnumItem);
            passBuilder.AddPass(new CleanInvalidDeclNamesPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            Assert.That(@enum.Items[0].Name, Is.EqualTo("_0"));
        }

        [Test]
        public void TestUnnamedEnumSupport()
        {
            passBuilder.AddPass(new CleanInvalidDeclNamesPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var unnamedEnum1 = AstContext.FindEnum("Unnamed_Enum_1").Single();
            var unnamedEnum2 = AstContext.FindEnum("Unnamed_Enum_2").Single();
            Assert.IsNotNull(unnamedEnum1);
            Assert.IsNotNull(unnamedEnum2);

            Assert.AreEqual(2, unnamedEnum1.Items.Count);
            Assert.AreEqual(2, unnamedEnum2.Items.Count);

            Assert.AreEqual(1, unnamedEnum1.Items[0].Value);
            Assert.AreEqual(2, unnamedEnum1.Items[1].Value);
            Assert.AreEqual(3, unnamedEnum2.Items[0].Value);
            Assert.AreEqual(4, unnamedEnum2.Items[1].Value);
        }

        [Test, Ignore("Nameless enums are no longer uniquely named.")]
        public void TestUniqueNamesAcrossTranslationUnits()
        {
            passBuilder.AddPass(new CleanInvalidDeclNamesPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var unnamedEnum1 = AstContext.GetEnumWithMatchingItem("UnnamedEnumA1");
            var unnamedEnum2 = AstContext.GetEnumWithMatchingItem("UnnamedEnumB1");
            Assert.IsNotNull(unnamedEnum1);
            Assert.IsNotNull(unnamedEnum2);

            Assert.AreNotEqual(unnamedEnum1.Name, unnamedEnum2.Name);
        }

        [Test]
        public void TestSkippedPrivateMethod()
        {
            AstContext.IgnoreClassMethodWithName("Foo", "toIgnore");
            Assert.That(AstContext.FindClass("Foo").First().Methods.Find(
                m => m.Name == "toIgnore"), Is.Null);
        }

        [Test]
        public void TestSetPropertyAsReadOnly()
        {
            const string className = "TestReadOnlyProperties";
            passBuilder.AddPass(new FieldToPropertyPass());
            passBuilder.AddPass(new GetterSetterToPropertyPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));
            AstContext.SetPropertyAsReadOnly(className, "readOnlyProperty");
            Assert.IsFalse(AstContext.FindClass(className).First().Properties.Find(
                m => m.Name == "readOnlyProperty").HasSetter);
            AstContext.SetPropertyAsReadOnly(className, "readOnlyPropertyMethod");
            Assert.IsFalse(AstContext.FindClass(className).First().Properties.Find(
                m => m.Name == "readOnlyPropertyMethod").HasSetter);
        }

        [Test]
        public void TestCheckAmbiguousFunctionsPass()
        {
            passBuilder.AddPass(new CheckAmbiguousFunctions());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));
            var @class = AstContext.FindClass("TestCheckAmbiguousFunctionsPass").FirstOrDefault();
            Assert.IsNotNull(@class);
            var overloads = @class.Methods.Where(m => m.Name == "Method");
            var constMethod = overloads
                .Where(m => m.IsConst && m.Parameters.Count == 0)
                .FirstOrDefault();
            var nonConstMethod = overloads
                .Where(m => !m.IsConst && m.Parameters.Count == 0)
                .FirstOrDefault();
            Assert.IsNotNull(constMethod);
            Assert.IsNotNull(nonConstMethod);
            Assert.IsTrue(constMethod.GenerationKind == GenerationKind.None);
            Assert.IsTrue(nonConstMethod.GenerationKind == GenerationKind.Generate);
            var constMethodWithParam = overloads
                .Where(m => m.IsConst && m.Parameters.Count == 1)
                .FirstOrDefault();
            var nonConstMethodWithParam = overloads
                .Where(m => !m.IsConst && m.Parameters.Count == 1)
                .FirstOrDefault();
            Assert.IsNotNull(constMethodWithParam);
            Assert.IsNotNull(nonConstMethodWithParam);
            Assert.IsTrue(constMethodWithParam.GenerationKind == GenerationKind.None);
            Assert.IsTrue(nonConstMethodWithParam.GenerationKind == GenerationKind.Generate);
        }

        [Test]
        public void TestSetMethodAsInternal()
        {
            var c = AstContext.Class("TestMethodAsInternal");
            var method = c.Method("beInternal");
            Assert.AreEqual(method.Access, AccessSpecifier.Public);
            passBuilder.AddPass(new CheckMacroPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));
            Assert.AreEqual(method.Access, AccessSpecifier.Internal);
        }

        private string TypePrinterDelegate(Type type)
        {
            return type.Visit(new CSharpTypePrinter(Driver.Context)).Type;
        }

        [Test]
        public void TestAbstractOperator()
        {
            passBuilder.AddPass(new CheckOperatorsOverloadsPass());
            passBuilder.RunPasses(pass => pass.VisitASTContext(AstContext));

            var @class = AstContext.FindDecl<Class>("ClassWithAbstractOperator").First();
            Assert.AreEqual(@class.Operators.First().GenerationKind, GenerationKind.None);
        }
    }
}
