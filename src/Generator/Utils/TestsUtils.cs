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

        public static string GetTestsDirectory(string name)
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "tests", name);

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception(string.Format(
                "Tests directory for project '{0}' was not found", name));
        }

        static string GetOutputDirectory()
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "obj");

                if (Directory.Exists(path))
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new Exception("Could not find tests output directory");
        }

        public virtual void Setup(Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = name;
            options.GeneratorKind = kind;
            options.OutputDir = Path.Combine(GetOutputDirectory(), "gen", name);
            options.SharedLibraryName = name + ".Native";
            options.GenerateLibraryNamespace = true;
            options.Quiet = true;
            options.IgnoreParseWarnings = true;

            Console.WriteLine("Generating bindings for {0} in {1} mode",
                options.LibraryName, options.GeneratorKind.ToString());

            // Workaround for CLR which does not check for .dll if the
            // name already has a dot.
            if (System.Type.GetType("Mono.Runtime") == null)
                options.SharedLibraryName += ".dll";

            var path = Path.GetFullPath(GetTestsDirectory(name));

            options.IncludeDirs.Add(path);

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
