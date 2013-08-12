using System.IO;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Utils
{
    public abstract class LibraryTest : ILibrary
    {
        readonly string name;
        readonly LanguageGeneratorKind kind;

        protected LibraryTest(string name, LanguageGeneratorKind kind)
        {
            this.name = name;
            this.kind = kind;
        }

        public virtual void Setup(Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = name;
            options.GeneratorKind = kind;
            options.OutputDir = "../gen/" + name;
            options.GenerateLibraryNamespace = true;
            options.SharedLibraryName = name + ".Native";

            // Workaround for CLR which does not check for .dll if the
            // name already has a dot.
            if (System.Type.GetType("Mono.Runtime") == null)
                options.SharedLibraryName += ".dll";

            options.CheckSymbols = false;

            var path = "../../../tests/" + name;
            options.IncludeDirs.Add(path);

            var files = Directory.EnumerateFiles(path, "*.h");
            foreach(var file in files)
                options.Headers.Add(Path.GetFileName(file));
        }

        public virtual void Preprocess(Driver driver, Library lib)
        {
        }

        public virtual void Postprocess(Library lib)
        {
        }

        public virtual void SetupPasses(Driver driver)
        {
        }
    }
}
