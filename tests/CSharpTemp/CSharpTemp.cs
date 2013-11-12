using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Utils;

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

