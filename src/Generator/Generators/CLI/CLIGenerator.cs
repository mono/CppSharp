using System.Collections.Generic;
using System.IO;
using CppSharp.Types.Std;

namespace CppSharp.Generators.CLI
{
    public class CLIGenerator : Generator
    {
        private readonly CLITypePrinter typePrinter;

        public CLIGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CLITypePrinter(driver);
            Type.TypePrinterDelegate += type => type.Visit(typePrinter);
        }

        public override bool Generate(TranslationUnit unit,
            List<GeneratorOutput> outputs)
        {
            var header = new CLIHeadersTemplate(Driver, unit);
            outputs.Add(GenerateTemplateOutput(header));

            var source = new CLISourcesTemplate(Driver, unit);
            outputs.Add(GenerateTemplateOutput(source));

            return true;
        }

        public override bool SetupPasses(PassBuilder builder)
        {
            return true;
        }
    }
}