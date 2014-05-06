using System.Linq;
using CppSharp.Passes;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using NUnit.Framework;

namespace CppSharp.Generator.Tests.AST
{
    [TestFixture]
    public class TestAST : ASTTestFixture
    {
        private PassBuilder<TranslationUnitPass> passBuilder;

        [TestFixtureSetUp]
        public void Init()
        {
        }

        [SetUp]
        public void Setup()
        {
            ParseLibrary("AST.h");
            passBuilder = new PassBuilder<TranslationUnitPass>(Driver);
        }

        [Test]
        public void TestASTParameter()
        {
            var func = AstContext.FindFunction("TestParameterProperties").FirstOrDefault();
            Assert.IsNotNull(func);

            var paramNames = new [] { "a", "b", "c" };
            var paramTypes = new [] 
            { 
                new QualifiedType(new BuiltinType(PrimitiveType.Bool)),
                new QualifiedType(
                    new PointerType()
                    {
                        Modifier = PointerType.TypeModifier.LVReference,
                        QualifiedPointee = new QualifiedType(
                            new BuiltinType(PrimitiveType.Int16), 
                            new TypeQualifiers() { IsConst = true })
                    }),
                new QualifiedType(
                    new PointerType()
                    {
                        Modifier = PointerType.TypeModifier.Pointer,
                        QualifiedPointee = new QualifiedType(new BuiltinType(PrimitiveType.Int32))
                    })
            };
            for (int i = 0; i < func.Parameters.Count; i++)
            {
                var param = func.Parameters[i];
                Assert.AreEqual(paramNames[i], param.Name, "Parameter.Name");
                Assert.AreEqual(paramTypes[i], param.QualifiedType, "Parameter.QualifiedType");
                Assert.AreEqual(i, param.Index, "Parameter.Index");
            }
            Assert.IsTrue(func.Parameters[2].HasDefaultValue, "Parameter.HasDefaultValue");
        }
    }
}
