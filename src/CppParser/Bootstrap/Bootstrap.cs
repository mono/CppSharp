using System;
using System.IO;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp
{
    /// <summary>
    /// Generates parser bootstrap code.
    /// </summary>
    class Bootstrap : ILibrary
    {
        static string GetSourceDirectory(string dir)
        {
            var directory = Directory.GetParent(Directory.GetCurrentDirectory());

            while (directory != null)
            {
                var path = Path.Combine(directory.FullName, dir);

                if (Directory.Exists(path) &&
                    Directory.Exists(Path.Combine(directory.FullName, "patches")))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception("Could not find build directory: " + dir);
        }

        public void Setup(Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = "CppSharp";

            options.Headers.AddRange(new string[]
            {
                "CppParser.h",
                "clang/AST/Expr.h",
            });
            options.SetupXcode();
            options.MicrosoftMode = false;
            options.TargetTriple = "i686-apple-darwin12.4.0";

            options.addDefines ("__STDC_LIMIT_MACROS");
            options.addDefines ("__STDC_CONSTANT_MACROS");

            var parserPath = Path.Combine(GetSourceDirectory("src"), "CppParser");
            var llvmPath = Path.Combine(GetSourceDirectory("deps"), "llvm");
            var clangPath = Path.Combine(llvmPath, "tools", "clang");

            options.addIncludeDirs(parserPath);
            options.addIncludeDirs(Path.Combine(llvmPath, "include"));
            options.addIncludeDirs(Path.Combine(llvmPath, "build", "include"));
            options.addIncludeDirs(Path.Combine(llvmPath, "build", "tools", "clang", "include"));
            options.addIncludeDirs(Path.Combine(clangPath, "include"));
                       
            options.OutputDir = Path.Combine(parserPath, "Bootstrap",
                "Bindings", GeneratorKind.CSharp.ToString(), options.TargetTriple);
        }

        public void SetupPasses(Driver driver)
        {
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Generating parser bootstrap code...");
            ConsoleDriver.Run(new Bootstrap());
            Console.WriteLine();
        }
    }
}
