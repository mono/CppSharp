using System;
using System.IO;

namespace CppSharp.Generators.CLI
{
    public class CLIGenerator : Generator
    {
        private readonly CLITypePrinter typePrinter;
        private readonly FileHashes fileHashes;

        public CLIGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CLITypePrinter(driver);
            Type.TypePrinterDelegate += type => type.Visit(typePrinter);
            fileHashes = FileHashes.Load("hashes.ser");
        }

        void WriteTemplate(TextTemplate template)
        {
            var path = GetOutputPath(template.TranslationUnit)
                + "." + template.FileExtension;

            template.Generate();
            var text = template.ToString();

            if(Driver.Options.WriteOnlyWhenChanged)
            {
                var updated = fileHashes.UpdateHash(path, text.GetHashCode());
                if (File.Exists(path) && !updated)
                    return;
            }

            Driver.Diagnostics.EmitMessage(DiagnosticId.FileGenerated,
                "  Generated '{0}'.", Path.GetFileName(path));
            File.WriteAllText(path, text);
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