using System.IO;
using Cxxi.Generators;

namespace Cxxi.Utils
{
    public abstract class LibraryTest : ILibrary
    {
        readonly string name;
        readonly LanguageGeneratorKind kind;

        public LibraryTest(string name, LanguageGeneratorKind kind)
        {
            this.name = name;
            this.kind = kind;
        }

        public virtual void Setup(DriverOptions options)
        {
            options.LibraryName = name + ".Native";
            options.GeneratorKind = kind;
            options.OutputDir = "../gen/" + name;
            options.GenerateLibraryNamespace = false;

            var path = "../../../tests/" + name;
            options.IncludeDirs.Add(path);

            var files = Directory.EnumerateFiles(path, "*.h");
            foreach(var file in files)
                options.Headers.Add(Path.GetFileName(file));
        }

        public virtual void Preprocess(Library lib)
        {
        }

        public virtual void Postprocess(Library lib)
        {
        }

        public virtual void SetupPasses(Driver driver, PassBuilder passes)
        {
        }

        public virtual void GenerateStart(TextTemplate template)
        {
        }

        public virtual void GenerateAfterNamespaces(TextTemplate template)
        {
        }
    }
}
