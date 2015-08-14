using System;
using System.IO;
using CppSharp.AST;
using CppSharp.Generators;
using CppAbi = CppSharp.Parser.AST.CppAbi;

namespace CppSharp
{
    /// <summary>
    /// Generates parser bootstrap code.
    /// </summary>
    class Bootstrap : ILibrary
    {

        const string LINUX_INCLUDE_BASE_DIR = "../../../../build/headers/x86_64-linux-gnu";

        internal readonly GeneratorKind Kind;
        internal readonly string Triple;
        internal readonly CppAbi Abi;

        public Bootstrap(GeneratorKind kind, string triple, CppAbi abi)
        {
            Kind = kind;
            Triple = triple;
            Abi = abi;
        }

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

            options.Abi = Abi;
            options.GeneratorKind = Kind;
            options.TargetTriple = Triple;

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

            options.OutputDir = Path.Combine(parserPath, "Bootstrap", "Bindings", Kind.ToString(), options.TargetTriple);
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

            if (Platform.IsWindows)
            {
                Console.WriteLine("Generating the C++/CLI parser bindings for Windows...");
                ConsoleDriver.Run(new Bootstrap(GeneratorKind.CLI, "i686-pc-win32-msvc",
                    CppAbi.Microsoft));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for Windows...");
                ConsoleDriver.Run(new Bootstrap(GeneratorKind.CSharp, "i686-pc-win32-msvc",
                    CppAbi.Microsoft));
                Console.WriteLine();
            }

            var osxHeadersPath = Path.Combine(GetSourceDirectory("build"), @"headers\osx");
            if (Directory.Exists(osxHeadersPath) || Platform.IsMacOS)
            {
                Console.WriteLine("Generating the C# parser bindings for OSX...");
                ConsoleDriver.Run(new Bootstrap(GeneratorKind.CSharp, "i686-apple-darwin12.4.0",
                    CppAbi.Itanium));
                Console.WriteLine();

                Console.WriteLine("Generating the C# parser bindings for OSX...");
                ConsoleDriver.Run(new Bootstrap(GeneratorKind.CSharp, "x86_64-apple-darwin12.4.0",
                    CppAbi.Itanium));
                Console.WriteLine();
            }
                
            if (Directory.Exists(LINUX_INCLUDE_BASE_DIR))
            {
                Console.WriteLine("Generating the C# parser bindings for Linux...");
                ConsoleDriver.Run(new Bootstrap(GeneratorKind.CSharp, "x86_64-linux-gnu",
                    CppAbi.Itanium));
                Console.WriteLine();
            }
        }
    }
}
