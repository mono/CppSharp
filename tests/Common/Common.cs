using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    [TypeMap("TypeMappedIndex")]
    public class TypeMappedIndex : TypeMap
    {
        public override string CLISignature(CLITypePrinterContext ctx)
        {
            return "unsigned short";
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write("::TypeMappedIndex()");
        }

        public override string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            return "ushort";
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

            driver.Options.OutputNamespace = "CommonTest";
            driver.Options.UnityBuild = true;
        }

        public override void SetupPasses(Driver driver)
        {
            driver.Options.MarshalCharAsManagedChar = true;
            driver.Options.GenerateProperties = true;
            driver.Options.GenerateConversionOperators = true;
            driver.Options.GenerateDefaultValuesForArguments = true;
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            driver.AddTranslationUnitPass(new GetterSetterToPropertyPass());
            driver.AddTranslationUnitPass(new CheckMacroPass());
            ctx.SetClassAsValueType("Bar");
            ctx.SetClassAsValueType("Bar2");
            ctx.IgnoreClassWithName("IgnoredType");

            ctx.FindCompleteClass("Foo").Enums.First(
                e => string.IsNullOrEmpty(e.Name)).Name = "RenamedEmptyEnum";
        }

        public override void Postprocess(Driver driver, ASTContext ctx)
        {
            // HACK: as seen above, GetterSetterToPropertyPass is called before all other passes
            // that is a hack in order for the pass to generate properties in Common.h
            // it is incapable of generating them in the proper manner
            // so it generates a property in system type from a member which is later ignored
            // so let's ignore that property manually
            var @class = ctx.FindCompleteClass("basic_string");
            foreach (var property in @class.Specializations.SelectMany(c => c.Properties))
                property.ExplicitlyIgnore();
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CommonTestsGenerator(GeneratorKind.CLI));
            ConsoleDriver.Run(new CommonTestsGenerator(GeneratorKind.CSharp));
        }
    }
}
