using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Utils;
using Attribute = CppSharp.AST.Attribute;

namespace CppSharp.Tests
{
    [TypeMap("QFlags")]
    public class QFlags : TypeMap
    {
        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            TemplateArgument templateArgument =
                ((TemplateSpecializationType) ctx.Type.Desugar()).Arguments[0];
            return templateArgument.Type.Type.ToString();
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
            if (!AlreadyVisited(function) && function.Name == "obsolete")
            {
                Attribute attribute = new Attribute
                {
                    Type = typeof(ObsoleteAttribute),
                    Value = string.Format("\"{0} is obsolete.\"", function.Name)
                };
                function.Attributes.Add(attribute);
            }
            return base.VisitFunctionDecl(function);
        }
    }

    public class CSharpTempTests : LibraryTest
    {
        public CSharpTempTests(GeneratorKind kind)
            : base("CSharpTemp", kind)
        {
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.GenerateInterfacesForMultipleInheritance = true;
            driver.Options.GenerateProperties = true;
            driver.Options.GenerateVirtualTables = true;
            driver.TranslationUnitPasses.AddPass(new TestAttributesPass());
        }

        public override void Postprocess(Driver driver, ASTContext lib)
        {
            new CaseRenamePass(
                RenameTargets.Function | RenameTargets.Method | RenameTargets.Property | RenameTargets.Delegate,
                RenameCasePattern.UpperCamelCase).VisitLibrary(driver.ASTContext);
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CSharpTempTests(GeneratorKind.CSharp));
        }
    }
}

