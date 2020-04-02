using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
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

    public class CompleteIgnoredClassTemplateForEmployeeTypedefPass : TranslationUnitPass
    {
        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            var templateType = GetDesugaredFinalPointeeElseType(typedef?.Type?.Desugar()) as TemplateSpecializationType;
            bool isTemplateTypedef = IsTemplateTypedef(templateType?.Template?.OriginalName);
            if (isTemplateTypedef)
            {
                Class @class;
                if (templateType.TryGetClass(out @class))
                {
                    @class.IsIncomplete = false;
                    return true;
                }
            }

            return base.VisitTypedefDecl(typedef);
        }

        private bool IsTemplateTypedef(string templateName)
        {
            return !string.IsNullOrEmpty(templateName) && "IgnoredClassTemplateForEmployee" == templateName;
        }

        public Type GetDesugaredFinalPointeeElseType(Type t)
        {
            Type finalPointee = t.GetFinalPointee();

            return finalPointee != null ? finalPointee.Desugar() : t;
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
        }

        public override void SetupPasses(Driver driver)
        {
            driver.AddTranslationUnitPass(new CompleteIgnoredClassTemplateForEmployeeTypedefPass());
        }

        public static void Main(string[] args)
        {
            ConsoleDriver.Run(new CLITestsGenerator(GeneratorKind.CLI));
        }
    }
}
