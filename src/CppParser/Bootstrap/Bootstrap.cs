using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using CppSharp.AST;

namespace CppSharp.Parser.Bootstrap
{
    /// <summary>
    /// Generates parser bootstrap code.
    /// </summary>
    class Bootstrap : ILibrary
    {
        static string GetSourceDirectory (string dir)
        {
            var directory = Directory.GetParent (Directory.GetCurrentDirectory ());

            while (directory != null) {
                var path = Path.Combine (directory.FullName, dir);

                if (Directory.Exists (path) &&
                Directory.Exists (Path.Combine (directory.FullName, "patches")))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception ("Could not find build directory: " + dir);
        }

        public void Setup (Driver driver)
        {
            var options = driver.Options;
            options.LibraryName = "CppSharp";
            options.DryRun = true;
            options.Headers.AddRange (new string[] {
                //"clang/AST/AST.h",
                "clang/AST/Expr.h",
                "clang/AST/Stmt.h",
                "clang/Basic/Specifiers.h",
            });

            options.SetupXcode ();
            options.MicrosoftMode = false;
            options.TargetTriple = "i686-apple-darwin12.4.0";

            options.addDefines ("__STDC_LIMIT_MACROS");
            options.addDefines ("__STDC_CONSTANT_MACROS");

            var llvmPath = Path.Combine (GetSourceDirectory ("deps"), "llvm");
            var clangPath = Path.Combine (llvmPath, "tools", "clang");

            options.addIncludeDirs (Path.Combine (llvmPath, "include"));
            options.addIncludeDirs (Path.Combine (llvmPath, "build", "include"));
            options.addIncludeDirs (Path.Combine (llvmPath, "build", "tools", "clang", "include"));
            options.addIncludeDirs (Path.Combine (clangPath, "include"));
        }

        public void SetupPasses (Driver driver)
        {
        }


        public void Preprocess (Driver driver, ASTContext ctx)
        {
            ctx.RenameNamespace ("CppSharp::CppParser", "Parser");

            //exprs
            ASTGenerator gen = new CppASTGenerator (driver, ctx);

            var exprClass = ctx.FindCompleteClass ("clang::Expr");
            var visitor = new DeclarationVisitor<Class> (exprClass.isBaseOf);
            exprClass.TranslationUnit.Visit (visitor);
            visitor.decls.ToList ().ForEach (gen.WriteExprClass);
            Console.WriteLine (gen);


            //enums
            gen = new CppASTGenerator (driver, ctx);
            var enumTargets = new Dictionary<String, String[]> {
                { "clang", 			new[] { "CallingConv" } },
                { "clang::Stmt",	new[] { "StmtClass", "APFloatSemantics" } },
            };

            var qualifiedNames = enumTargets.SelectMany (kv => kv.Value.Select (name => kv.Key + "::" + name));
            var enums = qualifiedNames.Select (ctx.FindCompleteEnum);

            enums.ToList ().ForEach (gen.WriteEnum);
            Console.WriteLine (gen);
        }


        public void Postprocess (Driver driver, ASTContext ctx)
        {
        }

        public static void Main (string[] args)
        {
            Console.WriteLine ("Generating parser bootstrap code...");
            ConsoleDriver.Run (new Bootstrap ());
            Console.WriteLine ();
        }
    }

    class DeclarationVisitor<T> : AstVisitor where T : Declaration
    {
        public HashSet<T> decls = new HashSet<T> ();
        public Predicate<T> pred;

        public DeclarationVisitor (Predicate<T> p)
        {
            pred = p;
        }

        public override bool VisitDeclaration (Declaration decl)
        {
            var t = decl as T;
            if (!decl.IsIncomplete && t != null && pred (t))
                decls.Add (t);

            return base.VisitDeclaration (decl);
        }
    }
}
