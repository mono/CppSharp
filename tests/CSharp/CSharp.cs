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
            var templateArgument = ((TemplateSpecializationType) ctx.Type.Desugar()).Arguments[0];
            return templateArgument.Type.Type;
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            return this.CSharpSignatureType(ctx).ToString();
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }
    }

    [TypeMap("QList")]
    public class QList : TypeMap
    {
        public override bool IsIgnored
        {
            get
            {
                var type = (TemplateSpecializationType) this.Type;
                var pointeeType = type.Arguments[0].Type;
                var checker = new TypeIgnoreChecker(TypeMapDatabase);
                pointeeType.Visit(checker);
                return checker.IsIgnored;
            }
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            if (ctx.CSharpKind == CSharpTypePrinterContextKind.Native)
                return Type.IsAddress() ? "QList.Internal*" : "QList.Internal";

            return string.Format("System.Collections.Generic.{0}<{1}>",
                ctx.MarshalKind == CSharpMarshalKind.DefaultExpression ? "List" : "IList",
                ctx.GetTemplateParameterList());
        }

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            // pointless, put just so that the generated code compiles
            ctx.Return.Write("new QList.Internal()");
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
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

        public override void CSharpMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public override void CSharpMarshalToManaged(MarshalContext ctx)
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
            driver.Options.GenerateInterfacesForMultipleInheritance = true;
            driver.Options.GeneratePropertiesAdvanced = true;
            // To ensure that calls to constructors in conversion operators
            // are not ambiguous with multiple inheritance pass enabled.
            driver.Options.GenerateConversionOperators = true;
            driver.TranslationUnitPasses.AddPass(new TestAttributesPass());
            driver.TranslationUnitPasses.AddPass(new CheckMacroPass());
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateDefaultValuesForArguments = true;
            driver.Options.GenerateSingleCSharpFile = true;
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
                RenameCasePattern.UpperCamelCase).VisitLibrary(driver.ASTContext);
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CSharpTestsGenerator(GeneratorKind.CSharp));
        }
    }
}

