using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Passes;
using CppSharp.Types;
using CppSharp.Utils;

namespace CppSharp.Tests
{
    [TypeMap("IgnoredClassTemplateForEmployee")]
    public class IgnoredClassTemplateForEmployeeMap : TypeMap
    {
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CustomType("CLI::Employee^");
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write($"gcnew CLI::Employee({ctx.ReturnVarName}.m_employee)");
        }
    }

    [TypeMap("TestMappedTypeNonConstRefParam")]
    public class TestMappedTypeNonConstRefParamTypeMap : TypeMap
    {
        public override Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(string));
        }

        public override Type CppSignatureType(TypePrinterContext ctx)
        {
            var tagType = ctx.Type as TagType;
            var typePrinter = new CppTypePrinter(Context);
            return new CustomType(tagType.Declaration.Visit(typePrinter));
        }

        public override void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write("clix::marshalString<clix::E_UTF8>({0}.m_str)", ctx.ReturnVarName);
        }

        public override void CLIMarshalToNative(MarshalContext ctx)
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
