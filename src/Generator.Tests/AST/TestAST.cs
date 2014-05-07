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

        [Test]
        public void TestASTHelperMethods()
        {
            var @class = AstContext.FindClass("Math::Complex").FirstOrDefault();
            Assert.IsNotNull(@class, "Couldn't find Math::Complex class.");
            var plusOperator = @class.FindOperator(CXXOperatorKind.Plus).FirstOrDefault();
            Assert.IsNotNull(plusOperator, "Couldn't find operator+ in Math::Complex class.");
            var typedef = AstContext.FindTypedef("Math::Single").FirstOrDefault();
            Assert.IsNotNull(typedef);
        }

        #region TestVisitor
        class TestVisitor : IDeclVisitor<bool>
        {
            public bool VisitDeclaration(Declaration decl)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitClassDecl(Class @class)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitFieldDecl(Field field)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitFunctionDecl(Function function)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitMethodDecl(Method method)
            {
                return true;
            }

            public bool VisitParameterDecl(Parameter parameter)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitTypedefDecl(TypedefDecl typedef)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitEnumDecl(Enumeration @enum)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitVariableDecl(Variable variable)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitClassTemplateDecl(ClassTemplate template)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitFunctionTemplateDecl(FunctionTemplate template)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitMacroDefinition(MacroDefinition macro)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitNamespace(Namespace @namespace)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitEvent(Event @event)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitProperty(Property property)
            {
                throw new System.NotImplementedException();
            }
        }
        #endregion

        [Test]
        public void TestASTVisitor()
        {
            var testVisitor = new TestVisitor();
            var plusOperator = AstContext.TranslationUnits
                .SelectMany(u => u.Namespaces.Where(n => n.Name == "Math"))
                .SelectMany(n => n.Classes.Where(c => c.Name == "Complex"))
                .SelectMany(c => c.Methods.Where(m => m.OperatorKind == CXXOperatorKind.Plus))
                .First();
            Assert.IsTrue(plusOperator.Visit(testVisitor));
        }

        [Test]
        public void TestASTEnumItemByName()
        {
            var @enum = AstContext.FindEnum("TestASTEnumItemByName").Single();
            Assert.NotNull(@enum);
            Assert.IsTrue(@enum.ItemsByName.ContainsKey("TestItemByName"));
        }
    }
}
