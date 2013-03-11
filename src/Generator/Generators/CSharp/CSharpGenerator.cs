using System;
using System.IO;
using Cxxi.Types;

namespace Cxxi.Generators.CSharp
{
    public class CSharpGenerator : Generator
    {
        private readonly ITypePrinter typePrinter;

        public CSharpGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CSharpTypePrinter(driver.TypeDatabase, driver.Library);
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
            var template = new CSharpTextTemplate(Driver, unit);
            WriteTemplate(template);

            return true;
        }
    }
}
