using System;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Utils;
using Attribute = CppSharp.AST.Attribute;
using Type = CppSharp.AST.Type;

namespace CppSharp.Tests
{
    [TypeMap("QFlags")]
    public class QFlags : TypeMap
    {
        public override string CSharpConstruct()
        {
            return string.Empty;
        }

        public override Type CSharpSignatureType(CSharpTypePrinterContext ctx)
        {
            return GetEnumType(ctx.Type);
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            return CSharpSignatureType(ctx).ToString();
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            if (ctx.Parameter.Type.Desugar().IsAddress())
                ctx.Return.Write("new global::System.IntPtr(&{0})", ctx.Parameter.Name);
            else
                ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            if (ctx.ReturnType.Type.Desugar().IsAddress())
            {
                var finalType = ctx.ReturnType.Type.GetFinalPointee() ?? ctx.ReturnType.Type;
                var enumType = GetEnumType(finalType);
                ctx.Return.Write("*({0}*) {1}", enumType, ctx.ReturnVarName);
            }
            else
            {
                ctx.Return.Write(ctx.ReturnVarName);
            }
        }

        private static Type GetEnumType(Type mappedType)
        {
            var type = mappedType.Desugar();
            ClassTemplateSpecialization classTemplateSpecialization;
            var templateSpecializationType = type as TemplateSpecializationType;
            if (templateSpecializationType != null)
                classTemplateSpecialization = templateSpecializationType.GetClassTemplateSpecialization();
            else
                classTemplateSpecialization = (ClassTemplateSpecialization) ((TagType) type).Declaration;
            return classTemplateSpecialization.Arguments[0].Type.Type;
        }
    }

    [TypeMap("QList")]
    public class QList : TypeMap
    {
        public override bool IsIgnored
        {
            get
            {
                var type = (TemplateSpecializationType) Type;
                var pointeeType = type.Arguments[0].Type;
                var checker = new TypeIgnoreChecker(TypeMapDatabase);
                pointeeType.Visit(checker);
                return checker.IsIgnored;
            }
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            if (ctx.CSharpKind == CSharpTypePrinterContextKind.Native)
                return string.Format("QList.{0}{1}", Helpers.InternalStruct,
                    Type.IsAddress() ? "*" : string.Empty);

            return string.Format("System.Collections.Generic.{0}<{1}>",
                ctx.MarshalKind == CSharpMarshalKind.DefaultExpression ? "List" : "IList",
                ctx.GetTemplateParameterList());
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            // pointless, put just so that the generated code compiles
            ctx.Return.Write("new QList.{0}()", Helpers.InternalStruct);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("TypeMappedWithOperator")]
    public class TypeMappedWithOperator : TypeMap
    {
        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            // doesn't matter, we just need it to compile
            return "int";
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    public class TestAttributesPass : TranslationUnitPass
    {
        public override bool VisitFunctionDecl(Function function)
        {
            if (AlreadyVisited(function) || function.Name != "obsolete")
                return false;

            var attribute = new Attribute
            {
                Type = typeof(ObsoleteAttribute),
                Value = string.Format("\"{0} is obsolete.\"", function.Name)
            };

            function.Attributes.Add(attribute);

            return base.VisitFunctionDecl(function);
        }
    }

    public class CSharpTestsGenerator : GeneratorTest
    {
        public CSharpTestsGenerator(GeneratorKind kind)
            : base("CSharp", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GeneratePropertiesAdvanced = true;
            // To ensure that calls to constructors in conversion operators
            // are not ambiguous with multiple inheritance pass enabled.
            driver.Options.GenerateConversionOperators = true;
            driver.Context.TranslationUnitPasses.AddPass(new TestAttributesPass());
            driver.Context.TranslationUnitPasses.AddPass(new CheckMacroPass());
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateDefaultValuesForArguments = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("TestCopyConstructorVal");
            ctx.SetClassAsValueType("QGenericArgument");
            ctx.SetClassAsValueType("StructWithPrivateFields");
            ctx.SetClassAsValueType("QPoint");
            ctx.SetClassAsValueType("QSize");
            ctx.SetClassAsValueType("QRect");

            ctx.IgnoreClassWithName("IgnoredTypeInheritingNonIgnoredWithNoEmptyCtor");
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
            new CaseRenamePass(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property | RenameTargets.Delegate | RenameTargets.Variable,
                RenameCasePattern.UpperCamelCase).VisitASTContext(driver.Context.ASTContext);
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CSharpTestsGenerator(GeneratorKind.CSharp));
        }
    }
}

