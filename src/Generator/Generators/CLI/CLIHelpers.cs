using System;
using System.IO;
using Cxxi.Types;

namespace Cxxi.Generators.CLI
{
    public class CLIGenerator : ILanguageGenerator
    {
        public Options Options { get; set; }
        public Library Library { get; set; }
        public ILibrary Transform { get; set; }
        public ITypeMapDatabase TypeMapDatabase { get; set; }
        public Generator Generator { get; set; }

        private readonly CLITypePrinter typePrinter;

        public CLIGenerator(Generator generator)
        {
            Generator = generator;
            typePrinter = new CLITypePrinter(TypeMapDatabase, Library);
            Type.TypePrinter = typePrinter;
        }

        T CreateTemplate<T>(TranslationUnit unit) where T : CLITextTemplate, new()
        {
            var template = new T
            {
                Generator = Generator,
                Options = Options,
                Library = Library,
                Transform = Transform,
                Module = unit,
                TypeSig = typePrinter
            };

            return template;
        }

        public static String WrapperSuffix = "_wrapper";
        
        void WriteTemplate(TextTemplate template)
        {
            var file = Path.GetFileNameWithoutExtension(template.Module.FileName) + WrapperSuffix + "."
                + template.FileExtension;

            var path = Path.Combine(Options.OutputDir, file);

            Console.WriteLine("  Generated '" + file + "'.");
            File.WriteAllText(Path.GetFullPath(path), template.ToString());
        }

        public bool Generate(TranslationUnit unit)
        {
            typePrinter.Library = Library;

            var header = CreateTemplate<CLIHeadersTemplate>(unit);
            WriteTemplate(header);

            var source = CreateTemplate<CLISourcesTemplate>(unit);
            WriteTemplate(source);

            return true;
        }
    }
}