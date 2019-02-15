using System;
using System.Collections.Generic;
using System.IO;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.C;

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

                if (Directory.Exists(path))
                    return path;

                directory = directory.Parent;
            }

            throw new Exception("Could not find build directory: " + dir);
        }

        public void Setup(Driver driver)
        {
            driver.Options.GeneratorKind = GeneratorKind.CSharp;
            driver.Options.DryRun = true;
            driver.ParserOptions.EnableRTTI = true;
            driver.ParserOptions.SkipLayoutInfo = true;

            var module = driver.Options.AddModule("CppSharp");

            module.Defines.Add("__STDC_LIMIT_MACROS");
            module.Defines.Add("__STDC_CONSTANT_MACROS");

            var basePath = Path.Combine(GetSourceDirectory("build"), "scripts");
            var llvmPath = Path.Combine(basePath, "llvm-981341-windows-vs2017-x86-RelWithDebInfo");
            var clangPath = Path.Combine(llvmPath, "tools", "clang");

            module.IncludeDirs.AddRange(new[]
            {
                Path.Combine(llvmPath, "include"),
                Path.Combine(llvmPath, "build", "include"),
                Path.Combine(llvmPath, "build", "tools", "clang", "include"),
                Path.Combine(clangPath, "include")
            });

            module.Headers.AddRange(new[]
            {
                "clang/AST/Stmt.h",
                "clang/AST/StmtCXX.h",
                "clang/AST/Expr.h",
                "clang/AST/ExprCXX.h",
            });

            module.LibraryDirs.Add(Path.Combine(llvmPath, "lib"));
        }

        public void SetupPasses(Driver driver)
        {
        }

        public void Preprocess(Driver driver, ASTContext ctx)
        {
            ctx.RenameNamespace("CppSharp::CppParser", "Parser");

            var preprocessDecls = new PreprocessDeclarations();
            foreach (var unit in ctx.TranslationUnits)
                unit.Visit(preprocessDecls);

            GenerateStmt(driver.Context);
            GenerateExpr(driver.Context);
        }

        private void GenerateExpr(BindingContext ctx)
        {
            var exprUnit = ctx.ASTContext.TranslationUnits.Find(unit =>
                unit.FileName.Contains("Expr.h"));
            var exprClass = exprUnit.FindNamespace("clang").FindClass("Expr");

            var exprSubclassVisitor = new SubclassVisitor(exprClass);
            exprUnit.Visit(exprSubclassVisitor);

            ctx.Options.GeneratorKind = GeneratorKind.CPlusPlus;
            var nativeCodeGen = new NativeParserCodeGenerator(ctx);
            nativeCodeGen.CTypePrinter.PrintScopeKind = TypePrintScopeKind.Local;
            nativeCodeGen.GenerateFilePreamble(CommentKind.BCPL);
            nativeCodeGen.NewLine();

            nativeCodeGen.WriteLine("#pragma once");
            nativeCodeGen.NewLine();

            nativeCodeGen.WriteInclude(new CInclude { File = "Stmt.h", Kind = CInclude.IncludeKind.Quoted });
            nativeCodeGen.NewLine();

            nativeCodeGen.WriteLine("namespace CppSharp { namespace CppParser { namespace AST {");
            nativeCodeGen.NewLine();

            foreach (var @class in exprSubclassVisitor.Classes)
            {
                RemoveNamespace(@class);
                @class.Visit(nativeCodeGen);
            }

            nativeCodeGen.NewLine();
            nativeCodeGen.WriteLine("} } }");

            WriteFile(nativeCodeGen, "Expr.h");
        }

        private void GenerateStmt(BindingContext ctx)
        {
            var stmtUnit = ctx.ASTContext.TranslationUnits.Find(unit =>
                unit.FileName.Contains("Stmt.h"));
            var stmtClass = stmtUnit.FindNamespace("clang").FindClass("Stmt");

            var stmtClassEnum = stmtClass.FindEnum("StmtClass");
            stmtClassEnum.Name = "StmtKind";
            stmtClass.Declarations.Remove(stmtClassEnum);

            CleanupEnumItems(stmtClassEnum);

            var stmtSubclassVisitor = new SubclassVisitor(stmtClass);
            stmtUnit.Visit(stmtSubclassVisitor);

            ctx.Options.GeneratorKind = GeneratorKind.CPlusPlus;

            var nativeCodeGen = new NativeParserCodeGenerator(ctx);
            nativeCodeGen.CTypePrinter.PrintScopeKind = TypePrintScopeKind.Local;
            nativeCodeGen.GenerateFilePreamble(CommentKind.BCPL);
            nativeCodeGen.NewLine();

            nativeCodeGen.WriteLine("#pragma once");
            nativeCodeGen.NewLine();

            nativeCodeGen.WriteLine("namespace CppSharp { namespace CppParser { namespace AST {");
            nativeCodeGen.NewLine();

            stmtClassEnum.Visit(nativeCodeGen);
            foreach (var @class in stmtSubclassVisitor.Classes)
            {
                RemoveNamespace(@class);
                @class.Visit(nativeCodeGen);
            }

            nativeCodeGen.NewLine();
            nativeCodeGen.WriteLine("} } }");

            WriteFile(nativeCodeGen, "Stmt.h");
        }

        static void CleanupEnumItems(Enumeration exprClassEnum)
        {
            foreach (var item in exprClassEnum.Items)
            {
                var suffix = "Class";
                if (item.Name.EndsWith(suffix))
                    item.Name = item.Name.Remove(item.Name.LastIndexOf(suffix), suffix.Length);
            }
        }

        static void RemoveNamespace(Declaration decl)
        {
            if (decl.Namespace != null && !string.IsNullOrWhiteSpace(decl.Namespace.Name))
            {
                decl.Namespace.Declarations.Remove(decl);
                decl.Namespace = decl.TranslationUnit;
            }
        }

        static void WriteFile(CodeGenerator codeGenerator, string fileName)
        {
            var srcDir = GetSourceDirectory("src");
            var path = Path.Combine(srcDir, "CppParser", fileName);
            File.WriteAllText(path, codeGenerator.Generate());
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

    class NativeParserCodeGenerator : CCodeGenerator
    {
        public NativeParserCodeGenerator(BindingContext context)
            : base(context)
        {
        }

        public override string FileExtension => throw new NotImplementedException();

        public override void Process()
        {
            throw new NotImplementedException();
        }
    }

    class PreprocessDeclarations : AstVisitor
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (string.IsNullOrWhiteSpace(@class.Name))
                @class.ExplicitlyIgnore();

            if (@class.Name.EndsWith("Bitfields"))
                @class.ExplicitlyIgnore();

            if (@class.Name.EndsWith("Iterator"))
                @class.ExplicitlyIgnore();

            if (@class.Name == "EmptyShell")
                @class.ExplicitlyIgnore();

            if (@class.Name == "APIntStorage" || @class.Name == "APFloatStorage")
                @class.ExplicitlyIgnore();

            foreach (var @base in @class.Bases)
            {
                if (@base.Class == null)
                    continue;

                if (@base.Class.Name.Contains("TrailingObjects"))
                    @base.ExplicitlyIgnore();
            }

            return base.VisitClassDecl(@class);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.Name == "APFloatSemantics")
                @enum.ExplicitlyIgnore();

            if (@enum.IsAnonymous || string.IsNullOrWhiteSpace(@enum.Name))
                @enum.ExplicitlyIgnore();

            @enum.SetScoped();
            return base.VisitEnumDecl(@enum);
        }
    }

    class SubclassVisitor : AstVisitor
    {
        public HashSet<Class> Classes;
        readonly Class @class;

        public SubclassVisitor (Class @class)
        {
            this.@class = @class;
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
            if (!VisitDeclaration(@class))
                return false;

            if (!@class.IsIncomplete && IsDerivedFrom(@class, this.@class))
                Classes.Add (@class);

            return base.VisitClassDecl (@class);
        }
    }
}
