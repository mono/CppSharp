using System.Linq;
using CppSharp.Passes;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using NUnit.Framework;
using CppSharp.Generators.CSharp;
using System;
using CppSharp.Generators;
using CppSharp.Parser;

namespace CppSharp.Generator.Tests.AST
{
    [TestFixture]
    public class TestAST : ASTTestFixture
    {
        [TestFixtureSetUp]
        public void Init()
        {
            CppSharp.AST.Type.TypePrinterDelegate = type =>
            {
                PrimitiveType primitiveType;
                return type.IsPrimitiveType(out primitiveType) ? primitiveType.ToString() : string.Empty;
            };
            ParseLibrary("AST.h", "ASTExtensions.h");
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
                            new TypeQualifiers { IsConst = true })
                    }),
                new QualifiedType(
                    new PointerType
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

            public bool VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization)
            {
                throw new NotImplementedException();
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

            public bool VisitTypeAliasDecl(TypeAlias typeAlias)
            {
                throw new NotImplementedException();
            }

            public bool VisitEnumDecl(Enumeration @enum)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitEnumItemDecl(Enumeration.Item item)
            {
                throw new NotImplementedException();
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

            public bool VisitTemplateParameterDecl(TypeTemplateParameter templateParameter)
            {
                throw new NotImplementedException();
            }

            public bool VisitNonTypeTemplateParameterDecl(NonTypeTemplateParameter nonTypeTemplateParameter)
            {
                throw new NotImplementedException();
            }

            public bool VisitTemplateTemplateParameterDecl(TemplateTemplateParameter templateTemplateParameter)
            {
                throw new NotImplementedException();
            }

            public bool VisitTypeAliasTemplateDecl(TypeAliasTemplate typeAliasTemplate)
            {
                throw new NotImplementedException();
            }

            public bool VisitFunctionTemplateSpecializationDecl(FunctionTemplateSpecialization specialization)
            {
                throw new NotImplementedException();
            }

            public bool VisitVarTemplateDecl(VarTemplate template)
            {
                throw new NotImplementedException();
            }

            public bool VisitVarTemplateSpecializationDecl(VarTemplateSpecialization template)
            {
                throw new NotImplementedException();
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
            ClassTemplateSpecialization classTemplateSpecialization;
            Assert.IsTrue(typeDef.Type.TryGetDeclaration(out classTemplateSpecialization));
            Assert.AreSame(classTemplateSpecialization.TemplatedDecl.TemplatedClass, template.TemplatedClass);
        }

        [Test]
        public void TestFindClassInNamespace()
        {
            Assert.IsNotNull(AstContext.FindClass("HiddenInNamespace").FirstOrDefault());
        }

        [Test]
        public void TestLineNumber()
        {
            Assert.AreEqual(65, AstContext.FindClass("HiddenInNamespace").First().LineNumberStart);
        }

        [Test]
        public void TestLineNumberOfFriend()
        {
            Assert.AreEqual(88, AstContext.FindFunction("operator+").First().LineNumberStart);
        }

        [Test]
        public void TestSignature()
        {
            Assert.AreEqual("void testSignature()", AstContext.FindFunction("testSignature").Single().Signature);
            Assert.AreEqual("void testImpl()", AstContext.FindFunction("testImpl").Single().Signature);
            Assert.AreEqual("void testConstSignature() const",
                AstContext.FindClass("HasConstFunction").Single().FindMethod("testConstSignature").Signature);
            Assert.AreEqual("void testConstSignatureWithTrailingMacro() const",
                AstContext.FindClass("HasConstFunction").Single().FindMethod("testConstSignatureWithTrailingMacro").Signature);
            // TODO: restore when the const of a return type is fixed properly
            //Assert.AreEqual("const int& testConstRefSignature()", AstContext.FindClass("HasConstFunction").Single().FindMethod("testConstRefSignature").Signature);
            //Assert.AreEqual("const int& testStaticConstRefSignature()", AstContext.FindClass("HasConstFunction").Single().FindMethod("testStaticConstRefSignature").Signature);
        }

        [Test]
        public void TestAmbiguity()
        {
            var bindingContext = new BindingContext(new TextDiagnosticPrinter(), new DriverOptions(),
                new ParserOptions());
            new CleanUnitPass { Context = bindingContext }.VisitASTContext(AstContext);
            new CheckAmbiguousFunctions { Context = bindingContext }.VisitASTContext(AstContext);
            Assert.IsTrue(AstContext.FindClass("HasAmbiguousFunctions").Single().FindMethod("ambiguous").IsAmbiguous);
        }

        [Test]
        public void TestAtomics()
        {
            var type = AstContext.FindClass("Atomics").Single().Fields
                .Find(f => f.Name == "AtomicInt").Type as BuiltinType;
            Assert.IsTrue(type != null && type.IsPrimitiveType(PrimitiveType.Int));
        }

        [Test]
        public void TestMacroLineNumber()
        {
            Assert.AreEqual(98, AstContext.FindClass("HasAmbiguousFunctions").First().Specifiers.Last().LineNumberStart);
        }

        [Test]
        public void TestImplicitDeclaration()
        {
            Assert.IsTrue(AstContext.FindClass("ImplicitCtor").First().Constructors.First(
                c => c.Parameters.Count == 0).IsImplicit);
        }

        [Test]
        public void TestSpecializationArguments()
        {
            var classTemplate = AstContext.FindDecl<ClassTemplate>("TestSpecializationArguments").FirstOrDefault();
            Assert.IsTrue(classTemplate.Specializations[0].Arguments[0].Type.Type.IsPrimitiveType(PrimitiveType.Int));
        }

        [Test]
        public void TestFunctionInstantiatedFrom()
        {
            var classTemplate = AstContext.FindDecl<ClassTemplate>("TestSpecializationArguments").FirstOrDefault();
            Assert.AreEqual(classTemplate.Specializations[0].Constructors.First(
                c => !c.IsCopyConstructor && !c.IsMoveConstructor).InstantiatedFrom,
                classTemplate.TemplatedClass.Constructors.First(c => !c.IsCopyConstructor && !c.IsMoveConstructor));
        }

        [Test]
        public void TestComments()
        {
            var @class = AstContext.FindCompleteClass("TestComments");
            var commentClass = @class.Comment.FullComment.CommentToString(Options.CommentPrefix);
            Assert.AreEqual(@"/// <summary>
/// <para>Hash set/map base class.</para>
/// <para>Note that to prevent extra memory use due to vtable pointer, %HashBase intentionally does not declare a virtual destructor</para>
/// <para>and therefore %HashBase pointers should never be used.</para>
/// </summary>".Replace("\r", string.Empty), commentClass.Replace("\r", string.Empty));

            var method = @class.Methods.First(m => m.Name == "GetIOHandlerControlSequence");
            var commentMethod = method.Comment.FullComment.CommentToString(Options.CommentPrefix);
            Assert.AreEqual(@"/// <summary>
/// <para>Get the string that needs to be written to the debugger stdin file</para>
/// <para>handle when a control character is typed.</para>
/// </summary>
/// <remarks>
/// <para>Some GUI programs will intercept &quot;control + char&quot; sequences and want</para>
/// <para>to have them do what normally would happen when using a real</para>
/// <para>terminal, so this function allows GUI programs to emulate this</para>
/// <para>functionality.</para>
/// </remarks>
/// <param name=""ch"">
/// <para>The character that was typed along with the control key</para>
/// </param>
/// <returns>
/// <para>The string that should be written into the file handle that is</para>
/// <para>feeding the input stream for the debugger, or NULL if there is</para>
/// <para>no string for this control key.</para>
/// </returns>".Replace("\r", string.Empty), commentMethod.Replace("\r", string.Empty));
        }

        [Test]
        public void TestCompletionOfClassTemplates()
        {
            var templates = AstContext.FindDecl<ClassTemplate>("ForwardedTemplate").ToList();
            var template = templates.Single(t => t.DebugText.Replace("\r", string.Empty) ==
                "template <typename T>\r\nclass ForwardedTemplate\r\n{\r\n}".Replace("\r", string.Empty));
            Assert.IsFalse(template.IsIncomplete);
        }

        [Test]
        public void TestOriginalNamesOfSpecializations()
        {
            var template = AstContext.FindDecl<ClassTemplate>("TestSpecializationArguments").First();
            Assert.That(template.Specializations[0].Constructors.First().QualifiedName,
                Is.Not.EqualTo(template.Specializations[1].Constructors.First().QualifiedName));
        }

        [Test]
        public void TestPrintingConstPointerWithConstType()
        {
            var template = AstContext.FindDecl<ClassTemplate>("TestSpecializationArguments").First();
            var cppTypePrinter = new CppTypePrinter { PrintScopeKind = CppTypePrintScopeKind.Qualified };
            var builtin = new BuiltinType(PrimitiveType.Char);
            var pointee = new QualifiedType(builtin, new TypeQualifiers { IsConst = true });
            var pointer = new QualifiedType(new PointerType(pointee), new TypeQualifiers { IsConst = true });
            var type = pointer.Visit(cppTypePrinter);
            Assert.AreEqual(type, "const char* const");
        }

        [Test]
        public void TestPrintingSpecializationWithConstValue()
        {
            var template = AstContext.FindDecl<ClassTemplate>("TestSpecializationArguments").First();
            var cppTypePrinter = new CppTypePrinter { PrintScopeKind = CppTypePrintScopeKind.Qualified };
            Assert.That(template.Specializations.Last().Visit(cppTypePrinter),
                Is.EqualTo("TestSpecializationArguments<const TestASTEnumItemByName>"));
        }

        [Test]
        public void TestLayoutBase()
        {
            var @class = AstContext.FindCompleteClass("TestComments");
            Assert.That(@class.Layout.Bases.Count, Is.EqualTo(0));
        }
	}
}
