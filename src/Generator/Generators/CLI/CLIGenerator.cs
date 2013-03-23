using System;
using System.IO;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public class CLIGenerator : Generator
    {
        private readonly ITypePrinter typePrinter;
        private readonly FileHashes fileHashes;

        public CLIGenerator(Driver driver) : base(driver)
        {
            typePrinter = new CLITypePrinter(driver);
            Type.TypePrinter = typePrinter;
            fileHashes = FileHashes.Load("hashes.ser");
        }

        void WriteTemplate(TextTemplate template)
        {
            var file = Path.GetFileNameWithoutExtension(template.TranslationUnit.FileName)
                + Driver.Options.WrapperSuffix + "."
                + template.FileExtension;

            var path = Path.Combine(Driver.Options.OutputDir, file);
            var fullPath = Path.GetFullPath(path);

            template.Generate();

            Console.WriteLine("  Generated '" + file + "'.");

            var str = template.ToString();

            if(Driver.Options.WriteOnlyWhenChanged)
            {
                var updated = fileHashes.UpdateHash(path, str.GetHashCode());
                if(File.Exists(fullPath) && !updated)
                    return;
            } 

            File.WriteAllText(fullPath,str);
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