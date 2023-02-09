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
                driver.ParserOptions.TargetTriple = "x86_64-apple-darwin";

            var path = Path.GetFullPath(GetTestsDirectory(name));
            testModule.IncludeDirs.Add(path);
            testModule.LibraryDirs.Add(options.OutputDir);
            testModule.Libraries.Add($"{name}.Native");

            var files = Directory.EnumerateFiles(path, "*.h", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var includeDir = Path.GetDirectoryName(file);

                if (!testModule.IncludeDirs.Contains(includeDir))
                    testModule.IncludeDirs.Add(includeDir);

                testModule.Headers.Add(Path.GetFileName(file));
            }
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
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty);

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, "tests", "dotnet", name);

                if (Directory.Exists(path))
                    return path;

                path = Path.Combine(directory.FullName, "external", "CppSharp", "tests", "dotnet", name);

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception($"Tests directory for project '{name}' was not found");
        }

        static string GetOutputDirectory()
        {
            var directory = Directory.GetParent(Assembly.GetExecutingAssembly().Location);
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
