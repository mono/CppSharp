using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using NUnit.Framework;

namespace CppSharp.Generator.Tests.AST
{
    [TestFixture]
    public class TestAST : ASTTestFixture
    {
        public TestAST() : base("AST.h", "ASTExtensions.h")
        {
        }

        [Test]
        public void TestASTParameter()
        {
            var func = AstContext.FindFunction("TestParameterProperties").FirstOrDefault();
            Assert.IsNotNull(func);

            var paramNames = new[] { "a", "b", "c" };
            var paramTypes = new[]
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
            var @namespace = AstContext.FindDecl<Namespace>("Math").FirstOrDefault();
            Assert.IsNotNull(@namespace, "Couldn't find Math namespace.");
            var @class = @namespace.FindClass("Complex");
            Assert.IsNotNull(@class, "Couldn't find Math::Complex class.");
            var plusOperator = @class.FindOperator(CXXOperatorKind.Plus).FirstOrDefault();
            Assert.IsNotNull(plusOperator, "Couldn't find operator+ in Math::Complex class.");
            var typedef = @namespace.FindTypedef("Single");
            Assert.IsNotNull(typedef);
        }

        #region TestVisitor
        class TestVisitor : IDeclVisitor<bool>
        {
            public bool VisitDeclaration(Declaration decl)
            {
                throw new System.NotImplementedException();
            }

            public bool VisitTranslationUnit(TranslationUnit unit)
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

            public bool VisitTypedefNameDecl(TypedefNameDecl typedef)
            {
                throw new NotImplementedException();
            }

            public bool VisitUnresolvedUsingDecl(UnresolvedUsingTypename unresolvedUsingTypename)
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
            Assert.AreEqual(6, @class.Templates.Count());
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
            Assert.AreEqual(6, template.Specializations.Count);
            Assert.AreEqual(TemplateSpecializationKind.ExplicitInstantiationDefinition, template.Specializations[0].SpecializationKind);
            Assert.AreEqual(TemplateSpecializationKind.ExplicitInstantiationDefinition, template.Specializations[4].SpecializationKind);
            Assert.AreEqual(TemplateSpecializationKind.Undeclared, template.Specializations[5].SpecializationKind);
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
        public void TestASTClassTemplatePartialSpecialization()
        {
            var classTemplate = AstContext.TranslationUnits
                .SelectMany(u => u.Templates.OfType<ClassTemplate>())
                .FirstOrDefault(t => t.Name == "TestClassTemplatePartialSpecialization");

            var classTemplatePartialSpecializations = classTemplate.Specializations.Where(specialization => specialization is ClassTemplatePartialSpecialization).Cast<ClassTemplatePartialSpecialization>();
            Assert.IsTrue(classTemplatePartialSpecializations.Count() == 1);
            var classTemplatePartialSpecialization = classTemplatePartialSpecializations.First();
            Assert.AreEqual(TemplateSpecializationKind.ExplicitSpecialization, classTemplatePartialSpecialization.SpecializationKind);

            var classTemplatePartialSpecializationParameters = classTemplatePartialSpecialization.Parameters;
            Assert.AreEqual(1, classTemplatePartialSpecializationParameters.Count);
            Assert.AreEqual((classTemplatePartialSpecializationParameters[0] as TypeTemplateParameter).Name, "K");

            var classTemplatePartialSpecializationArguments = classTemplatePartialSpecialization.Arguments;
            Assert.AreEqual(2, classTemplatePartialSpecializationArguments.Count);
            Assert.AreEqual((classTemplatePartialSpecializationArguments[0].Type.Type as BuiltinType).Type, PrimitiveType.Int);
            Assert.AreEqual((classTemplatePartialSpecializationArguments[1].Type.Type as TemplateParameterType).Parameter, classTemplatePartialSpecializationParameters[0]);
        }

        [Test]
        public void TestDeprecatedAttrs()
        {
            var deprecated_func = AstContext.FindFunction("deprecated_func");
            Assert.IsNotNull(deprecated_func);
            Assert.IsTrue(deprecated_func.First().IsDeprecated);

            var non_deprecated_func = AstContext.FindFunction("non_deprecated_func");
            Assert.IsNotNull(non_deprecated_func);
            Assert.IsFalse(non_deprecated_func.First().IsDeprecated);
        }

        [Test]
        public void TestFindClassInNamespace()
        {
            Assert.IsNotNull(AstContext.FindClass("HiddenInNamespace").FirstOrDefault());
        }

        [Test]
        public void TestLineNumber()
        {
            Assert.AreEqual(70, AstContext.FindClass("HiddenInNamespace").First().LineNumberStart);
        }

        [Test]
        public void TestLineNumberOfFriend()
        {
            Assert.AreEqual(93, AstContext.FindFunction("operator+").First().LineNumberStart);
        }

        static string StripWindowsNewLines(string text)
        {
            return text.ReplaceLineBreaks(string.Empty);
        }

        [Test]
        public void TestSignature()
        {
            Assert.AreEqual("void testSignature()", AstContext.FindFunction("testSignature").Single().Signature);
            Assert.AreEqual("void testImpl()",
                StripWindowsNewLines(AstContext.FindFunction("testImpl").Single().Signature));
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
            new CheckAmbiguousFunctions { Context = Driver.Context }.VisitASTContext(AstContext);
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
            Assert.AreEqual(103, AstContext.FindClass("HasAmbiguousFunctions").First().Specifiers.Last().LineNumberStart);
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
            var cppTypePrinter = new CppTypePrinter(Context) { ResolveTypeMaps = false };
            cppTypePrinter.PushScope(TypePrintScopeKind.Qualified);
            var builtin = new BuiltinType(PrimitiveType.Char);
            var pointee = new QualifiedType(builtin, new TypeQualifiers { IsConst = true });
            var pointer = new QualifiedType(new PointerType(pointee), new TypeQualifiers { IsConst = true });
            string type = pointer.Visit(cppTypePrinter);
            Assert.That(type, Is.EqualTo("const char* const"));
        }

        [Test]
        public void TestPrintingSpecializationWithConstValue()
        {
            var template = AstContext.FindDecl<ClassTemplate>("TestSpecializationArguments").First();
            var cppTypePrinter = new CppTypePrinter(Context);
            cppTypePrinter.PushScope(TypePrintScopeKind.Qualified);
            Assert.That(template.Specializations.Last().Visit(cppTypePrinter).Type,
                Is.EqualTo("TestSpecializationArguments<const TestASTEnumItemByName>"));
        }

        [Test]
        public void TestLayoutBase()
        {
            var @class = AstContext.FindCompleteClass("HasConstFunction");
            Assert.That(@class.Layout.Bases.Count, Is.EqualTo(0));
        }

        [Test]
        public void TestFunctionSpecifications()
        {
            var constExpr = AstContext.FindFunction("constExpr").First();
            Assert.IsTrue(constExpr.IsConstExpr);

            var noExcept = AstContext.FindFunction("noExcept").First();
            Assert.That(((FunctionType)noExcept.FunctionType.Type).ExceptionSpecType,
                Is.EqualTo(ExceptionSpecType.BasicNoexcept));

            var noExceptTrue = AstContext.FindFunction("noExceptTrue").First();
            Assert.That(((FunctionType)noExceptTrue.FunctionType.Type).ExceptionSpecType,
                Is.EqualTo(ExceptionSpecType.NoexceptTrue));

            var noExceptFalse = AstContext.FindFunction("noExceptFalse").First();
            Assert.That(((FunctionType)noExceptFalse.FunctionType.Type).ExceptionSpecType,
                Is.EqualTo(ExceptionSpecType.NoexceptFalse));

            var regular = AstContext.FindFunction("testSignature").First();
            Assert.IsFalse(regular.IsConstExpr);
            var regularFunctionType = (FunctionType)regular.FunctionType.Type;
            Assert.That(regularFunctionType.ExceptionSpecType,
                Is.EqualTo(ExceptionSpecType.None));
        }

        [Test]
        public void TestFunctionSpecializationInfo()
        {
            var functionWithSpecInfo = AstContext.TranslationUnits.Find(
                t => t.IsValid && t.FileName == "AST.h").Functions.First(
                f => f.Name == "functionWithSpecInfo" && !f.IsDependent);
            var @float = new QualifiedType(new BuiltinType(PrimitiveType.Float));
            Assert.That(functionWithSpecInfo.SpecializationInfo.Arguments.Count, Is.EqualTo(2));
            foreach (var arg in functionWithSpecInfo.SpecializationInfo.Arguments)
                Assert.That(arg.Type, Is.EqualTo(@float));
        }

        [Test]
        public void TestVolatile()
        {
            var cppTypePrinter = new CppTypePrinter(Context);
            cppTypePrinter.PushScope(TypePrintScopeKind.Qualified);
            var builtin = new BuiltinType(PrimitiveType.Char);
            var pointee = new QualifiedType(builtin, new TypeQualifiers { IsConst = true, IsVolatile = true });
            var type = pointee.Visit(cppTypePrinter).Type;
            Assert.That(type, Is.EqualTo("const volatile char"));
        }

        [Test]
        public void TestFindFunctionInNamespace()
        {
            var function = AstContext.FindFunction("Math::function").FirstOrDefault();
            Assert.That(function, Is.Not.Null);
        }

        [Test]
        public void TestPrintNestedInSpecialization()
        {
            var template = AstContext.FindDecl<ClassTemplate>("TestTemplateClass").First();
            var cppTypePrinter = new CppTypePrinter(Context);
            cppTypePrinter.PushScope(TypePrintScopeKind.Qualified);
            Assert.That(template.Specializations[4].Classes.First().Visit(cppTypePrinter).Type,
                Is.EqualTo("TestTemplateClass<Math::Complex>::NestedInTemplate"));
        }

        [Test]
        public void TestPrintQualifiedSpecialization()
        {
            var functionWithSpecializationArg = AstContext.FindFunction("functionWithSpecializationArg").First();
            var cppTypePrinter = new CppTypePrinter(Context);
            cppTypePrinter.PushScope(TypePrintScopeKind.Qualified);
            Assert.That(functionWithSpecializationArg.Parameters[0].Visit(cppTypePrinter).Type,
                Is.EqualTo("const TestTemplateClass<int>"));
        }

        [Test]
        public void TestDependentNameType()
        {
            var template = AstContext.FindDecl<ClassTemplate>(
                "TestSpecializationArguments").First();
            var type = template.TemplatedClass.Fields[0].Type.Visit(
                new CSharpTypePrinter(Driver.Context));
            Assert.That($"{type}",
                Is.EqualTo("global::Test.TestTemplateClass<T>.NestedInTemplate"));
        }

        [Test]
        public void TestTemplateConstructorName()
        {
            new CleanInvalidDeclNamesPass { Context = Driver.Context }.VisitASTContext(AstContext);
            var template = AstContext.FindClass("TestTemplateClass").First();
            foreach (var constructor in template.Constructors)
                Assert.That(constructor.Name, Is.EqualTo("TestTemplateClass<T>"));
        }

        [Test]
        public void TestClassContext()
        {
            var @classA = AstContext.FindClass("ClassA").First();
            Assert.That(@classA.Classes.Count, Is.EqualTo(0));
            Assert.That(@classA.Redeclarations.Count, Is.EqualTo(0));

            var @classB = AstContext.FindClass("ClassB").First();
            Assert.That(@classB.Redeclarations.Count, Is.EqualTo(1));

            var @classC = AstContext.FindClass("ClassC").First();
            Assert.That(@classC.Redeclarations.Count, Is.EqualTo(2));
        }

        [Test]
        public void TestPrivateCCtorCopyAssignment()
        {
            Class @class = AstContext.FindCompleteClass("HasPrivateCCtorCopyAssignment");
            Assert.That(@class.Constructors.Any(c => c.Parameters.Count == 0), Is.True);
            Assert.That(@class.Constructors.Any(c => c.IsCopyConstructor), Is.True);
            Assert.That(@class.Methods.Any(o => o.OperatorKind == CXXOperatorKind.Equal), Is.True);
        }

        [Test]
        public void TestCompletionSpecializationInFunction()
        {
            Function function = AstContext.FindFunction("returnIncompleteTemplateSpecialization").First();
            function.ReturnType.Type.TryGetClass(out Class specialization);
            Assert.That(specialization.IsIncomplete, Is.False);
        }

        [Test]
        public void TestPreprocessedEntities()
        {
            var unit = AstContext.TranslationUnits.First(u => u.FileName == "AST.h");
            var macro = unit.PreprocessedEntities.OfType<MacroDefinition>()
                .FirstOrDefault(exp => exp.Name == "MACRO");
            Assert.NotNull(macro);
            Assert.AreEqual("(x, y, z) x##y##z", macro.Expression);
        }

        [Test]
        public void TestMethods()
        {
            var hasMethodsClass = AstContext.FindClass("HasMethods").First();
            var isVolatileMethod = hasMethodsClass.FindMethod("isVolatileMethod");
            Assert.That(isVolatileMethod.IsVolatile, Is.EqualTo(true));
        }
    }
}
