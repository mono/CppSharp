using Cxxi.Generators.CLI;
using Cxxi.Types;
using System.IO;

namespace Cxxi.Generators
{
    public enum LanguageGeneratorKind
    {
        CPlusPlusCLI,
        CSharp
    }

    public interface ILanguageGenerator
    {
        Options Options { get; set; }
        Library Library { get; set; }
        ILibrary Transform { get; set; }
        Generator Generator { get; set; }

        bool Generate(TranslationUnit unit);
    }

    public partial class Generator
    {
        public Options Options;
        public Library Library;
        public ILibrary LibraryTransform;
        public TypeDatabase TypeDatabase;

        public Generator(Options options, Library library, ILibrary libraryTransform)
        {
            this.Options = options;
            this.Library = library;
            this.LibraryTransform = libraryTransform;

            TypeDatabase = new TypeDatabase();
            TypeDatabase.SetupTypeMaps();
        }

        public void Generate()
        {
            var generator = CreateLanguageGenerator(Options.Template);

            if (!Directory.Exists(Options.OutputDir))
                Directory.CreateDirectory(Options.OutputDir);

            // Process everything in the global namespace for now.
            foreach (var module in Library.TranslationUnits)
            {
                if (module.ExplicityIgnored || !module.HasDeclarations)
                    continue;

                if (module.IsSystemHeader)
                    continue;

                // Generate the target code.
                generator.Generate(module);
            }
        }

        ILanguageGenerator CreateLanguageGenerator(LanguageGeneratorKind kind)
        {
            ILanguageGenerator generator = null;

            switch (kind)
            {
                case LanguageGeneratorKind.CPlusPlusCLI:
                    generator = new CLIGenerator(this);
                    break;
                case LanguageGeneratorKind.CSharp:
                    //generator = new CSharpGenerator();
                    break;
            }

            generator.Options = Options;
            generator.Library = Library;
            generator.Transform = LibraryTransform;

            return generator;
        }

        ILanguageGenerator CreateLanguageGenerator(string kind)
        {
            switch (kind)
            {
                default:
                case "cli":
                    return CreateLanguageGenerator(LanguageGeneratorKind.CPlusPlusCLI);
                case "cs":
                    return CreateLanguageGenerator(LanguageGeneratorKind.CSharp);
            }
        }
    }
}