using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    [TypeMap("IgnoredClassTemplateForEmployee", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class IgnoredClassTemplateForEmployeeMap : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx, GeneratorKind kind)
        {
            return new CustomType("CLI::Employee^");
        }

        public override void MarshalToManaged(MarshalContext ctx, GeneratorKind kind)
        {
            ctx.Return.Write($"gcnew CLI::Employee({ctx.ReturnVarName}.m_employee)");
        }
    }

    [TypeMap("TestMappedTypeNonConstRefParam", GeneratorKindID = GeneratorKind.CLI_ID)]
    public class TestMappedTypeNonConstRefParamTypeMap : TypeMap
    {
        public override Type SignatureType(TypePrinterContext ctx, GeneratorKind kind)
        {
            return new CILType(typeof(string));
        }

        public override void MarshalToManaged(MarshalContext ctx, GeneratorKind kind)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF8>({0}.m_str)", ctx.ReturnVarName);
        }

        public override void MarshalToNative(MarshalContext ctx, GeneratorKind kind)
        {
            if (ctx.Parameter.Usage == ParameterUsage.InOut)
            {
                ctx.Before.WriteLine($"System::String^ _{ctx.Parameter.Name} = {ctx.Parameter.Name};");
            }

            string paramName = ctx.Parameter.Usage == ParameterUsage.InOut ? $"_{ctx.Parameter.Name}" : ctx.Parameter.Name;

            ctx.Before.WriteLine(
                $"::TestMappedTypeNonConstRefParam _{ctx.ArgName} = clix::marshalString<clix::E_UTF8>({paramName});");

            ctx.Return.Write("_{0}", ctx.ArgName);
        }
    }

    public class CLITestsGenerator : GeneratorTest
    {
        public CLITestsGenerator(GeneratorKind kind)
            : base("CLI", kind)
        {
        }

        public override void Setup(Driver driver)
        {
            driver.Options.GenerateFinalizers = true;
            driver.Options.GenerateObjectOverrides = true;
            driver.Options.GenerateFreeStandingFunctionsClassName = tu => tu.FileNameWithoutExtension + "Cool";
            base.Setup(driver);
        }

        public override void Preprocess(Driver driver, ASTContext ctx)
        {
            LibraryHelpers.SetMethodParameterUsage(ctx, "TestMappedTypeNonConstRefParamConsumer",
                "ChangePassedMappedTypeNonConstRefParam", 1, ParameterUsage.InOut);
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CLITestsGenerator(GeneratorKind.CLI));
        }
    }
}
