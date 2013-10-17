using System;
using System.IO;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Utils
{
    public abstract class LibraryTest : ILibrary
    {
        readonly string name;
        readonly GeneratorKind kind;

        protected LibraryTest(string name, GeneratorKind kind)
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
            options.SharedLibraryName = name + ".Native";
            options.GenerateLibraryNamespace = true;
            options.CheckSymbols = false;
            options.Quiet = true;
            options.IgnoreParseWarnings = true;

            Console.WriteLine("Generating bindings for {0} in {1} mode",
                options.LibraryName, options.GeneratorKind.ToString());

            // Workaround for CLR which does not check for .dll if the
            // name already has a dot.
            if (System.Type.GetType("Mono.Runtime") == null)
                options.SharedLibraryName += ".dll";

            var path = Path.GetFullPath("../../../tests/" + name);
            options.IncludeDirs.Add(path);

            if (!Directory.Exists(path))
            {
                Console.WriteLine("Path to tests does not exist: {0}", path);
                return;
            }

            Console.WriteLine("Looking for tests in: {0}", path);
            var files = Directory.EnumerateFiles(path, "*.h");
            foreach (var file in files)
                options.Headers.Add(Path.GetFileName(file));
        }

        public virtual void Preprocess(Driver driver, ASTContext lib)
        {
        }

        public virtual void Postprocess(Driver driver, ASTContext lib)
        {
        }

        public virtual void SetupPasses(Driver driver)
        {
        }
    }
}
