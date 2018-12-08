using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using CppSharp.Types;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    [TypeMap("TypeMappedIndex")]
    public class TypeMappedIndex : TypeMap
    {
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new BuiltinType(PrimitiveType.UShort);
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("::TypeMappedIndex()");
        }

        public override Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new BuiltinType(PrimitiveType.UShort);
        }

        public override void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }

        public override void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write("IntPtr.Zero");
        }
    }

    public class CommonTestsGenerator : GeneratorTest
    {
        public CommonTestsGenerator(GeneratorKind kind)
            : base("Common", kind)
        {

        }

        public override void Setup(Driver driver)
        {
            base.Setup(driver);

            driver.Options.GenerateName = file =>
                file.FileNameWithoutExtension + "_GenerateName";

            driver.Options.Modules[1].OutputNamespace = "CommonTest";
            driver.ParserOptions.UnityBuild = true;
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateDefaultValuesForArguments = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.SetClassAsValueType("Bar");
            ctx.SetClassAsValueType("Bar2");
            ctx.IgnoreClassWithName("IgnoredType");

            ctx.FindCompleteClass("Foo").Enums.First(
                e => string.IsNullOrEmpty(e.Name)).Name = "RenamedEmptyEnum";
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CommonTestsGenerator(GeneratorKind.CLI));
            ConsoleDriver.Run(new CommonTestsGenerator(GeneratorKind.CSharp));
        }
    }
}
