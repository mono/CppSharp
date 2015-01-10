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
            ParseLibrary("AST.h", "ASTExtensions.h");
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
                            new BuiltinType(PrimitiveType.Short), 
                            new TypeQualifiers() { IsConst = true })
                    }),
                new QualifiedType(
                    new PointerType()
                    {
                        Modifier = PointerType.TypeModifier.Pointer,
                        QualifiedPointee = new QualifiedType(new BuiltinType(PrimitiveType.Int))
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

            public bool VisitFriend(Friend friend)
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

        [Test]
        public void TestASTFunctionTemplates()
        {
            var @class = AstContext.FindClass("TestTemplateFunctions").FirstOrDefault();
            Assert.IsNotNull(@class, "Couldn't find TestTemplateFunctions class.");
            Assert.AreEqual(6, @class.Templates.Count);
            var twoParamMethodTemplate = @class.Templates.OfType<FunctionTemplate>()
                .FirstOrDefault(t => t.Name == "MethodTemplateWithTwoTypeParameter");
            Assert.IsNotNull(twoParamMethodTemplate);
            Assert.AreEqual(2, twoParamMethodTemplate.Parameters.Count);
            Assert.AreEqual("T", twoParamMethodTemplate.Parameters[0].Name);
            Assert.AreEqual("S", twoParamMethodTemplate.Parameters[1].Name);
            Assert.IsTrue(twoParamMethodTemplate.Parameters[0].IsTypeParameter);
            Assert.IsTrue(twoParamMethodTemplate.Parameters[1].IsTypeParameter);
            var twoParamMethod = twoParamMethodTemplate.TemplatedFunction as Method;
            Assert.IsNotNull(twoParamMethod);
            Assert.IsInstanceOf<TemplateParameterType>(twoParamMethod.Parameters[0].Type);
            Assert.IsInstanceOf<TemplateParameterType>(twoParamMethod.Parameters[1].Type);
            Assert.AreEqual(twoParamMethodTemplate.Parameters[0], 
                ((TemplateParameterType)twoParamMethod.Parameters[0].Type).Parameter);
            Assert.AreEqual(twoParamMethodTemplate.Parameters[1],
                ((TemplateParameterType)twoParamMethod.Parameters[1].Type).Parameter);
            Assert.AreEqual(0, ((TemplateParameterType)twoParamMethod.Parameters[0].Type).Index);
            Assert.AreEqual(1, ((TemplateParameterType)twoParamMethod.Parameters[1].Type).Index);
        }

        [Test]
        public void TestASTClassTemplates()
        {
            var template = AstContext.TranslationUnits
                .SelectMany(u => u.Templates.OfType<ClassTemplate>())
                .FirstOrDefault(t => t.Name == "TestTemplateClass");
            Assert.IsNotNull(template, "Couldn't find TestTemplateClass class.");
            Assert.AreEqual(1, template.Parameters.Count);
            var templateTypeParameter = template.Parameters[0];
            Assert.AreEqual("T", templateTypeParameter.Name);
            var ctor = template.TemplatedClass.Constructors
                .FirstOrDefault(c => c.Parameters.Count == 1 && c.Parameters[0].Name == "v");
            Assert.IsNotNull(ctor);
            var paramType = ctor.Parameters[0].Type as TemplateParameterType;
            Assert.IsNotNull(paramType);
            Assert.AreEqual(templateTypeParameter, paramType.Parameter);
            Assert.AreEqual(3, template.Specializations.Count);
            Assert.AreEqual(TemplateSpecializationKind.ExplicitInstantiationDefinition, template.Specializations[0].SpecializationKind);
            Assert.AreEqual(TemplateSpecializationKind.ExplicitInstantiationDefinition, template.Specializations[1].SpecializationKind);
            Assert.AreEqual(TemplateSpecializationKind.Undeclared, template.Specializations[2].SpecializationKind);
            var typeDef = AstContext.FindTypedef("TestTemplateClassInt").FirstOrDefault();
            Assert.IsNotNull(typeDef, "Couldn't find TestTemplateClassInt typedef.");
            var integerInst = typeDef.Type as TemplateSpecializationType;
            Assert.AreEqual(1, integerInst.Arguments.Count);
            var intArgument = integerInst.Arguments[0];
            Assert.AreEqual(new BuiltinType(PrimitiveType.Int), intArgument.Type.Type);
            Class classTemplate;
            Assert.IsTrue(typeDef.Type.TryGetClass(out classTemplate));
            Assert.AreEqual(classTemplate, template.TemplatedClass);
        }

        [Test]
        public void TestFindClassInNamespace()
        {
            Assert.IsNotNull(AstContext.FindClass("HiddenInNamespace").FirstOrDefault());
        }
    }
}
