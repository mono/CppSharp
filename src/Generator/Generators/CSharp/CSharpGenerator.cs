using System;
using System.IO;

namespace CppSharp.Generators.CSharp
{
    public class CSharpGenerator : Generator
    {
        private readonly CSharpTypePrinter typePrinter;

        public CSharpGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CSharpTypePrinter(driver.TypeDatabase, driver.Library);
            Type.TypePrinterDelegate += type => type.Visit(typePrinter).Type;
        }

        void WriteTemplate(TextTemplate template)
        {
            var path = GetOutputPath(template.TranslationUnit)
                + "." + template.FileExtension;

            template.Generate();

            File.WriteAllText(path, template.ToString());
            Driver.Diagnostics.EmitMessage(DiagnosticId.FileGenerated,
                "  Generated '{0}'.", Path.GetFileName(path));
        }

        public override bool Generate(TranslationUnit unit)
        {
            var template = new CSharpTextTemplate(Driver, unit, typePrinter);
            WriteTemplate(template);

            return true;
        }
    }
}
