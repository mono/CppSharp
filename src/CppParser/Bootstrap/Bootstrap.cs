using System;
using System.Collections.Generic;
using System.IO;
using CppSharp.AST;

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
            options.DryRun = true;
            options.Headers.AddRange(new string[]
            {
                "clang/AST/Expr.h",
            });

            options.SetupXcode();
            options.MicrosoftMode = false;
            options.TargetTriple = "i686-apple-darwin12.4.0";

            options.addDefines ("__STDC_LIMIT_MACROS");
            options.addDefines ("__STDC_CONSTANT_MACROS");

            var llvmPath = Path.Combine (GetSourceDirectory ("deps"), "llvm");
            var clangPath = Path.Combine(llvmPath, "tools", "clang");

            options.addIncludeDirs(Path.Combine(llvmPath, "include"));
            options.addIncludeDirs(Path.Combine(llvmPath, "build", "include"));
            options.addIncludeDirs (Path.Combine (llvmPath, "build", "tools", "clang", "include"));
            options.addIncludeDirs(Path.Combine(clangPath, "include"));
        }

        public void SetupPasses(Driver driver)
        {
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.RenameNamespace("CppSharp::CppParser", "Parser");

            var exprClass = ctx.FindCompleteClass ("clang::Expr");

            var exprUnit = ctx.TranslationUnits [0];
            var subclassVisitor = new SubclassVisitor (exprClass);
            exprUnit.Visit (subclassVisitor);

            var subclasses = subclassVisitor.Classes;
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

    class SubclassVisitor : AstVisitor
    {
        public HashSet<Class> Classes;
        Class expressionClass;

        public SubclassVisitor (Class expression)
        {
            expressionClass = expression;
            Classes = new HashSet<Class> ();
        }

        static bool IsDerivedFrom(Class subclass, Class superclass)
        {
            if (subclass == null)
                return false;

            if (subclass == superclass)
                return true;

            return IsDerivedFrom (subclass.BaseClass, superclass);
        }

        public override bool VisitClassDecl (Class @class)
        {
            if (!@class.IsIncomplete && IsDerivedFrom (@class, expressionClass))
                Classes.Add (@class);

            return base.VisitClassDecl (@class);
        }
    }
}
