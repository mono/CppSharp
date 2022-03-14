using System;
using System.IO;
using System.Reflection;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Utils
{
    /// <summary>
    /// The main base class for a generator-based tests project.
    /// </summary>
    public abstract class GeneratorTest : ILibrary
    {
        readonly string name;
        readonly GeneratorKind kind;

        protected GeneratorTest(string name, GeneratorKind kind)
        {
            this.name = name;
            this.kind = kind;
        }

        public virtual void Setup(Driver driver)
        {
            var options = driver.Options;
            options.GeneratorKind = kind;
            options.OutputDir = Path.Combine(GetOutputDirectory(), "build", "gen", name);
            options.Quiet = true;
            options.GenerateDebugOutput = true;
            options.CheckSymbols = true;
            var testModule = options.AddModule(name);

            Diagnostics.Message("");
            Diagnostics.Message("Generating bindings for {0} ({1})",
                testModule.LibraryName, options.GeneratorKind.ToString());

            if (Platform.IsMacOS)
                driver.ParserOptions.TargetTriple = Environment.Is64BitProcess ?
                    "x86_64-apple-darwin" : "i686-apple-darwin";

            var path = Path.GetFullPath(GetTestsDirectory(name));
            testModule.IncludeDirs.Add(path);
            testModule.LibraryDirs.Add(options.OutputDir);
            testModule.Libraries.Add($"{name}.Native");

            Diagnostics.Message("Looking for tests in: {0}", path);
            var files = Directory.EnumerateFiles(path, "*.h");
            foreach (var file in files)
                testModule.Headers.Add(Path.GetFileName(file));
        }

        public virtual void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public virtual void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public virtual void SetupPasses(Driver driver)
        {
        }

        #region Helpers
        public static string GetTestsDirectory(string name)
        {
            var directory = new DirectoryInfo(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "tests", name);

                if (Directory.Exists(path))
                    return path;

                path = Path.Combine(directory.FullName, "external", "CppSharp", "tests", name);

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception(string.Format(
                "Tests directory for project '{0}' was not found", name));
        }

        static string GetOutputDirectory()
        {
            string exePath = new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath;
            var directory = Directory.GetParent(exePath);

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "build");

                if (Directory.Exists(path))
                    return directory.FullName;

                directory = directory.Parent;
            }

            throw new Exception("Could not find tests output directory");
        }
        #endregion
    }
}
