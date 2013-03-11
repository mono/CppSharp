using System;
using System.IO;
using Cxxi.Passes;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public class CLIGenerator : Generator
    {
        private readonly ITypePrinter typePrinter;

        public CLIGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CLITypePrinter(driver.TypeDatabase, driver.Library);
            Type.TypePrinter = typePrinter;
        }

        void WriteTemplate(TextTemplate template)
        {
            var file = Path.GetFileNameWithoutExtension(template.TranslationUnit.FileName)
                + Driver.Options.WrapperSuffix + "."
                + template.FileExtension;

            var path = Path.Combine(Driver.Options.OutputDir, file);

            template.Generate();

            Console.WriteLine("  Generated '" + file + "'.");
            File.WriteAllText(Path.GetFullPath(path), template.ToString());
        }

        public override bool Generate(TranslationUnit unit)
        {
            var header = new CLIHeadersTemplate(Driver, unit);
            WriteTemplate(header);

            var source = new CLISourcesTemplate(Driver, unit);
            WriteTemplate(source);

            return true;
        }
    }
}