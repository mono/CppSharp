using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Generators.CSharp;
using CppSharp.Passes;
using static CppSharp.CodeGeneratorHelpers;

namespace CppSharp
{
    /// <summary>
    /// Generates parser bootstrap code.
    /// </summary>
    class Bootstrap : ILibrary
    {
        private static string GetSourceDirectory(string dir)
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

        private static string GetLLVMRevision(string llvmDir)
            => File.ReadAllText(Path.Combine(llvmDir, "LLVM-commit"));

        private static string GetLLVMBuildDirectory()
        {
            var llvmDir = Path.Combine(GetSourceDirectory("build"), "llvm");
            var llvmRevision = GetLLVMRevision(llvmDir).Substring(0, 6);

            return Directory.EnumerateDirectories(llvmDir, $"*{llvmRevision}*").FirstOrDefault();
        }

        public void Setup(Driver driver)
        {
            driver.Options.GeneratorKind = GeneratorKind.CSharp;
            driver.Options.DryRun = true;
            driver.ParserOptions.EnableRTTI = true;
            driver.ParserOptions.SkipLayoutInfo = true;
            driver.ParserOptions.UnityBuild = true;

            var module = driver.Options.AddModule("CppSharp");

            module.Defines.Add("__STDC_LIMIT_MACROS");
            module.Defines.Add("__STDC_CONSTANT_MACROS");

            var llvmPath = GetLLVMBuildDirectory();

            if (llvmPath == null)
                throw new Exception("Could not find LLVM build directory");

            module.IncludeDirs.AddRange(new[]
            {
                Path.Combine(llvmPath, "include"),
                Path.Combine(llvmPath, "build", "include"),
                Path.Combine(llvmPath, "build", "clang", "include"),
                Path.Combine(llvmPath, "clang", "include")
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
            new IgnoreMethodsWithParametersPass { Context = driver.Context }
                .VisitASTContext(ctx);
            new GetterSetterToPropertyPass { Context = driver.Context }
                .VisitASTContext(ctx);

            var preprocessDecls = new PreprocessDeclarations();
            foreach (var unit in ctx.TranslationUnits)
                unit.Visit(preprocessDecls);

            var exprUnit = ctx.TranslationUnits.Find(unit =>
                unit.FileName.Contains("Expr.h"));
            var exprCxxUnit = ctx.TranslationUnits.Find(unit =>
                unit.FileName.Contains("ExprCXX.h"));

            var exprClass = exprUnit.FindNamespace("clang").FindClass("Expr");
            var exprSubclassVisitor = new SubclassVisitor(exprClass);
            exprUnit.Visit(exprSubclassVisitor);
            exprCxxUnit.Visit(exprSubclassVisitor);
            ExprClasses = exprSubclassVisitor.Classes;

            CodeGeneratorHelpers.CppTypePrinter = new CppTypePrinter(driver.Context);
            CodeGeneratorHelpers.CppTypePrinter.PushScope(TypePrintScopeKind.Local);

            GenerateStmt(driver.Context);
            GenerateExpr(driver.Context);
        }

        public void Postprocess(Driver driver, ASTContext ctx)
        {
        }

        public IEnumerable<Class> ExprClasses;

        private void GenerateExpr(BindingContext ctx)
        {
            var operationKindsUnit = ctx.ASTContext.TranslationUnits.Find(unit =>
                unit.FileName.Contains("OperationKinds.h"));
            var operatorKindsUnit = ctx.ASTContext.TranslationUnits.Find(unit =>
                unit.FileName.Contains("OperatorKinds.h"));
            var typeTraitsUnit = ctx.ASTContext.TranslationUnits.Find(unit =>
                unit.FileName == "TypeTraits.h");
            var unaryExprOrTypeTrait = typeTraitsUnit.FindNamespace("clang")
                .FindEnum("UnaryExprOrTypeTrait");

            var decls = new Declaration[] { operationKindsUnit, operatorKindsUnit,
                unaryExprOrTypeTrait }.Union(ExprClasses);

            // Write the native declarations headers
            var declsCodeGen = new ExprDeclarationsCodeGenerator(ctx, decls);
            declsCodeGen.GenerateDeclarations();
            WriteFile(declsCodeGen, Path.Combine("CppParser", "Expr.h"));

            var defsCodeGen = new ExprDefinitionsCodeGenerator(ctx, decls);
            defsCodeGen.GenerateDefinitions();
            WriteFile(defsCodeGen, Path.Combine("CppParser", "Expr.cpp"));

            // Write the native parsing routines
            var parserCodeGen = new ExprParserCodeGenerator(ctx, decls);
            parserCodeGen.GenerateParser();
            WriteFile(parserCodeGen, Path.Combine("CppParser", "ParseExpr.cpp"));

            // Write the managed declarations
            var managedCodeGen = new ManagedParserCodeGenerator(ctx, decls);
            managedCodeGen.GenerateDeclarations();
            WriteFile(managedCodeGen, Path.Combine("AST", "Expr.cs"));

            managedCodeGen = new ExprASTConverterCodeGenerator(ctx, decls);
            managedCodeGen.Process();
            WriteFile(managedCodeGen, Path.Combine("Parser", "ASTConverter.Expr.cs"));
        }

        private void GenerateStmt(BindingContext ctx)
        {
            var stmtUnit = ctx.ASTContext.TranslationUnits.Find(unit =>
                unit.FileName.Contains("Stmt.h"));
            var stmtCxxUnit = ctx.ASTContext.TranslationUnits.Find(unit =>
                unit.FileName.Contains("StmtCXX.h"));

            var stmtClass = stmtUnit.FindNamespace("clang").FindClass("Stmt");

            var stmtClassEnum = stmtClass.FindEnum("StmtClass");
            stmtClass.Declarations.Remove(stmtClassEnum);
            CleanupEnumItems(stmtClassEnum);

            var stmtSubclassVisitor = new SubclassVisitor(stmtClass);
            stmtUnit.Visit(stmtSubclassVisitor);
            stmtCxxUnit.Visit(stmtSubclassVisitor);

            var decls = new Declaration[] { stmtClassEnum }
                .Union(stmtSubclassVisitor.Classes);

            // Write the native declarations headers
            var declsCodeGen = new StmtDeclarationsCodeGenerator(ctx, decls);
            declsCodeGen.GenerateDeclarations();
            WriteFile(declsCodeGen, Path.Combine("CppParser", "Stmt.h"));

            var stmtClasses = stmtSubclassVisitor.Classes;
            var defsCodeGen = new StmtDefinitionsCodeGenerator(ctx, stmtClasses);
            defsCodeGen.GenerateDefinitions();
            WriteFile(defsCodeGen, Path.Combine("CppParser", "Stmt.cpp"));

            // Write the native parsing routines
            var parserCodeGen = new StmtParserCodeGenerator(ctx, stmtClasses, ExprClasses);
            parserCodeGen.GenerateParser();
            WriteFile(parserCodeGen, Path.Combine("CppParser", "ParseStmt.cpp"));

            // Write the managed declarations
            var managedCodeGen = new ManagedParserCodeGenerator(ctx, decls);
            managedCodeGen.GenerateDeclarations();
            WriteFile(managedCodeGen, Path.Combine("AST", "Stmt.cs"));

            managedCodeGen = new ManagedVisitorCodeGenerator(ctx, decls.Union(ExprClasses));
            managedCodeGen.Process();
            WriteFile(managedCodeGen, Path.Combine("AST", "StmtVisitor.cs"));

            managedCodeGen = new StmtASTConverterCodeGenerator(ctx, decls, stmtClassEnum);
            managedCodeGen.Process();
            WriteFile(managedCodeGen, Path.Combine("Parser", "ASTConverter.Stmt.cs"));
        }

        static void CleanupEnumItems(Enumeration exprClassEnum)
        {
            foreach (var item in exprClassEnum.Items)
            {
                if (item.Name.StartsWith("first", StringComparison.InvariantCulture) ||
                    item.Name.StartsWith("last", StringComparison.InvariantCulture))
                    item.ExplicitlyIgnore();

                if (item.Name.StartsWith("OMP") || item.Name.StartsWith("ObjC"))
                    item.ExplicitlyIgnore();

                item.Name = RemoveFromEnd(item.Name, "Class");
            }
        }

        static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        static string CalculateMD5(string text)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = GenerateStreamFromString(text))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "")
                        .ToLowerInvariant();
                }
            }
        }

        static bool WriteFile(CodeGenerator codeGenerator, string basePath)
        {
            var srcDir = GetSourceDirectory("src");
            var path = Path.Combine(srcDir, basePath);

            string oldHash = string.Empty;
            if (File.Exists(path))
                oldHash = CalculateMD5(File.ReadAllText(path));

            var sourceCode = codeGenerator.Generate();
            var newHash = CalculateMD5(sourceCode);

            if (oldHash == newHash)
                return false;

            File.WriteAllText(path, sourceCode);
            Console.WriteLine($"Writing '{Path.GetFileName(path)}'.");
            return true;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Generating parser bootstrap code...");
            ConsoleDriver.Run(new Bootstrap());
            Console.WriteLine();
        }
    }

    class PreprocessDeclarations : AstVisitor
    {
        private static void Check(Declaration decl)
        {
            if (string.IsNullOrWhiteSpace(decl.Name))
                decl.ExplicitlyIgnore();

            if (decl.Name.EndsWith("Bitfields", StringComparison.Ordinal))
                decl.ExplicitlyIgnore();

            if (decl.Name.EndsWith("Iterator", StringComparison.Ordinal))
                decl.ExplicitlyIgnore();

            if (decl.Name == "AssociationTy" ||
                decl.Name == "AssociationIteratorTy")
                decl.ExplicitlyIgnore();

            if (decl.Name == "EmptyShell")
                decl.ExplicitlyIgnore();

            if (decl.Name == "APIntStorage" || decl.Name == "APFloatStorage")
                decl.ExplicitlyIgnore();
        }

        public override bool VisitClassDecl(Class @class)
        {
            //
            // Statements
            //

            if (CodeGeneratorHelpers.IsAbstractStmt(@class))
                @class.IsAbstract = true;

            Check(@class);

            foreach (var @base in @class.Bases)
            {
                if (@base.Class == null)
                    continue;

                if (@base.Class.Name.Contains("TrailingObjects"))
                    @base.ExplicitlyIgnore();

                if (@base.Class.Name == "APIntStorage")
                {
                    @base.ExplicitlyIgnore();

                    var property = new Property
                    {
                        Access = AccessSpecifier.Public,
                        Name = "value",
                        Namespace = @class,
                        QualifiedType = new QualifiedType(
                            new BuiltinType(PrimitiveType.ULongLong))
                    };

                    if (!@class.Properties.Exists(p => p.Name == property.Name))
                        @class.Properties.Add(property);
                }

                if (@base.Class.Name == "APFloatStorage")
                {
                    @base.ExplicitlyIgnore();

                    var property = new Property
                    {
                        Access = AccessSpecifier.Public,
                        Name = "value",
                        Namespace = @class,
                        QualifiedType = new QualifiedType(
                            new BuiltinType(PrimitiveType.LongDouble))
                    };

                    if (!@class.Properties.Exists(p => p.Name == property.Name))
                        @class.Properties.Add(property);
                }
            }

            //
            // Expressions
            //

            if (@class.Name.EndsWith("EvalStatus") || @class.Name.EndsWith("EvalResult"))
                @class.ExplicitlyIgnore();

            if (@class.Name == "Expr")
            {
                foreach (var property in @class.Properties)
                {
                    switch (property.Name)
                    {
                        case "isObjCSelfExpr":
                        case "refersToVectorElement":
                        case "refersToGlobalRegisterVar":
                        case "isKnownToHaveBooleanValue":
                        case "isDefaultArgument":
                        case "isImplicitCXXThis":
                        case "bestDynamicClassTypeExpr":
                        case "refersToBitField":
                            property.ExplicitlyIgnore();
                            break;
                    }
                }
            }

            return base.VisitClassDecl(@class);
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            Check(template);
            return base.VisitClassTemplateDecl(template);
        }

        public override bool VisitTypeAliasTemplateDecl(TypeAliasTemplate template)
        {
            Check(template);
            return base.VisitTypeAliasTemplateDecl(template);
        }

        public override bool VisitProperty(Property property)
        {
            if (property.Name == "stripLabelLikeStatements")
                property.ExplicitlyIgnore();

            return base.VisitProperty(property);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (AlreadyVisited(@enum))
                return false;

            if (@enum.Name == "APFloatSemantics")
                @enum.ExplicitlyIgnore();

            if (@enum.IsAnonymous || string.IsNullOrWhiteSpace(@enum.Name))
                @enum.ExplicitlyIgnore();

            @enum.SetScoped();

            RemoveEnumItemsPrefix(@enum);

            return base.VisitEnumDecl(@enum);
        }

        private void RemoveEnumItemsPrefix(Enumeration @enum)
        {
            var enumItem = @enum.Items.FirstOrDefault();
            if (enumItem == null)
                return;

            var underscoreIndex = enumItem.Name.IndexOf('_');
            if (underscoreIndex == -1)
                return;

            if (enumItem.Name[underscoreIndex + 1] == '_')
                underscoreIndex++;

            var prefix = enumItem.Name.Substring(0, ++underscoreIndex);
            if (@enum.Items.Count(item => item.Name.StartsWith(prefix)) < 3)
                return;

            foreach (var item in @enum.Items)
            {
                if (!item.Name.StartsWith(prefix))
                {
                    item.ExplicitlyIgnore();
                    continue;
                }

                item.Name = item.Name.Substring(prefix.Length);
                item.Name = CaseRenamePass.ConvertCaseString(item,
                    RenameCasePattern.UpperCamelCase);
            }
        }
    }

    class SubclassVisitor : AstVisitor
    {
        public HashSet<Class> Classes;
        readonly Class @class;

        public SubclassVisitor(Class @class)
        {
            this.@class = @class;
            Classes = new HashSet<Class>();
        }

        static bool IsDerivedFrom(Class subclass, Class superclass)
        {
            if (subclass == null)
                return false;

            if (subclass == superclass)
                return true;

            if (!subclass.HasBaseClass)
                return false;

            return IsDerivedFrom(subclass.BaseClass, superclass);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            if (!@class.IsIncomplete && IsDerivedFrom(@class, this.@class))
                Classes.Add(@class);

            return base.VisitClassDecl(@class);
        }
    }

    #region Managed code generators

    class ManagedParserCodeGenerator : CSharpSources
    {
        internal readonly IEnumerable<Declaration> Declarations;

        public ManagedParserCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context)
        {
            Declarations = declarations;
            TypePrinter.PushScope(TypePrintScopeKind.Local);
            TypePrinter.PrintModuleOutputNamespace = false;
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);
            NewLine();
        }

        public void GenerateDeclarations()
        {
            Process();

            GenerateUsings();
            NewLine();

            WriteLine("namespace CppSharp.AST");
            WriteOpenBraceAndIndent();

            foreach (var decl in Declarations)
            {
                PushBlock();
                decl.Visit(this);
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            UnindentAndWriteCloseBrace();
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            return base.VisitDeclContext(@namespace);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.IsGenerated)
                return false;

            GenerateClassSpecifier(@class);
            NewLine();

            WriteOpenBraceAndIndent();

            PushBlock();
            VisitDeclContext(@class);
            PopBlock(NewLineKind.Always);

            WriteLine($"public {@class.Name}()");
            WriteOpenBraceAndIndent();
            UnindentAndWriteCloseBrace();
            NewLine();

            foreach (var method in @class.Methods)
            {
                if (SkipMethod(method))
                    continue;

                var iteratorType = GetIteratorType(method);
                var iteratorTypeName = GetIteratorTypeName(iteratorType, TypePrinter);
                var declName = GetDeclName(method, GeneratorKind.CSharp);

                WriteLine($@"public List<{iteratorTypeName}> {
                    declName} {{ get; private set; }} = new List<{iteratorTypeName}>();");
            }

            foreach (var property in @class.Properties)
            {
                if (SkipProperty(property))
                    continue;

                string typeName = RemoveClangNamespacePrefix(GetDeclTypeName(
                    property.Type, TypePrinter));
                string propertyName = GetDeclName(property, GeneratorKind.CSharp);

                WriteLine($"public {typeName} {propertyName} {{ get; set; }}");
            }

            var rootBase = @class.GetNonIgnoredRootBase();
            var isStmt = rootBase != null && rootBase.Name == "Stmt";

            if (isStmt && !(@class.IsAbstract && @class.Name != "Stmt"))
            {
                NewLine();
                GenerateVisitMethod(@class);
            }

            UnindentAndWriteCloseBrace();

            return true;
        }

        private void GenerateVisitMethod(Class @class)
        {
            if (@class.IsAbstract)
            {
                WriteLine("public abstract T Visit<T>(IStmtVisitor<T> visitor);");
                return;
            }

            WriteLine("public override T Visit<T>(IStmtVisitor<T> visitor) =>");
            WriteLineIndent("visitor.Visit{0}(this);", @class.Name);
        }

        public override string GetBaseClassTypeName(BaseClassSpecifier @base)
        {
            var type = base.GetBaseClassTypeName(@base);
            return RemoveClangNamespacePrefix(type);
        }

        private static string RemoveClangNamespacePrefix(string type)
        {
            return type.StartsWith("clang.") ?
                type.Substring("clang.".Length) : type;
        }

        public override void GenerateUsings()
        {
            WriteLine("using System;");
            WriteLine("using System.Collections.Generic;");
        }

        public override void GenerateDeclarationCommon(Declaration decl)
        {
        }

        public override void GenerateNamespaceFunctionsAndVariables(
            DeclarationContext context)
        {

        }
    }

    class ManagedVisitorCodeGenerator : ManagedParserCodeGenerator
    {
        public ManagedVisitorCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);
            NewLine();

            WriteLine("namespace CppSharp.AST");
            WriteOpenBraceAndIndent();

            GenerateVisitor();
            NewLine();

            GenerateVisitorInterface();

            UnindentAndWriteCloseBrace();
        }

        private void GenerateVisitor()
        {
            WriteLine($"public abstract partial class AstVisitor");
            WriteOpenBraceAndIndent();

            foreach (var @class in Declarations.OfType<Class>())
            {
                if (@class.Name == "Stmt") continue;

                PushBlock();
                var paramName = "stmt";
                WriteLine("public virtual bool Visit{0}({0} {1})",
                    @class.Name, paramName);
                WriteOpenBraceAndIndent();

                WriteLine($"if (!Visit{@class.BaseClass.Name}({paramName}))");
                WriteLineIndent("return false;");
                NewLine();

                WriteLine("return true;");

                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            UnindentAndWriteCloseBrace();
        }

        private void GenerateVisitorInterface()
        {
            WriteLine($"public interface IStmtVisitor<out T>");
            WriteOpenBraceAndIndent();

            foreach (var @class in Declarations.OfType<Class>())
            {
                var paramName = "stmt";
                WriteLine("T Visit{0}({0} {1});",
                    @class.Name, paramName);
            }

            UnindentAndWriteCloseBrace();
        }
    }

    class ASTConverterCodeGenerator : ManagedParserCodeGenerator
    {
        readonly Enumeration StmtClassEnum;

        public ASTConverterCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations, Enumeration stmtClassEnum)
            : base(context, declarations)
        {
            StmtClassEnum = stmtClassEnum;
        }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);
            NewLine();

            WriteLine("using CppSharp.Parser.AST;");
            WriteLine("using static CppSharp.ConversionUtils;");
            NewLine();

            WriteLine("namespace CppSharp");
            WriteOpenBraceAndIndent();

            GenerateVisitor();
            NewLine();

            GenerateConverter();

            UnindentAndWriteCloseBrace();
        }

        public virtual string BaseTypeName { get; }

        public string ParamName => BaseTypeName.ToLowerInvariant();

        private void GenerateVisitor()
        {
            var comment = new RawComment
            {
                BriefText = "Implements the visitor pattern for the generated" +
                    $" {BaseTypeName.ToLowerInvariant()} bindings.\n"
            };

            GenerateComment(comment);

            WriteLine($"public abstract class {BaseTypeName}Visitor<TRet> where TRet : class");
            WriteOpenBraceAndIndent();

            var classes = Declarations.OfType<Class>().Select(@class => @class.Name)
                .Where(@class => !IsAbstractStmt(@class));

            foreach (var className in classes)
            {
                WriteLine("public abstract TRet Visit{0}({0} {1});",
                    className, ParamName);
            }

            NewLine();
            WriteLine($"public virtual TRet Visit(Parser.AST.{BaseTypeName} {ParamName})");
            WriteOpenBraceAndIndent();

            WriteLine($"if ({ParamName} == null)");
            WriteLineIndent("return default(TRet);");
            NewLine();

            WriteLine($"switch({ParamName}.StmtClass)");
            WriteOpenBraceAndIndent();

            var enumItems = StmtClassEnum != null ?
                StmtClassEnum.Items.Where(item => item.IsGenerated)
                    .Select(item => RemoveFromEnd(item.Name, "Class"))
                    .Where(@class => !IsAbstractStmt(@class))
                    : new List<string>();

            GenerateSwitchCases(StmtClassEnum != null ? enumItems : classes);

            UnindentAndWriteCloseBrace();
            UnindentAndWriteCloseBrace();
            UnindentAndWriteCloseBrace();
        }

        public virtual void GenerateSwitchCases(IEnumerable<string> classes)
        {
            foreach (var className in classes)
            {
                WriteLine($"case StmtClass.{className}:");
                WriteOpenBraceAndIndent();

                WriteLine($"var _{ParamName} = {className}.__CreateInstance({ParamName}.__Instance);");

                var isExpression = !Declarations.OfType<Class>()
                    .Where(c => c.Name == className).Any();

                if (isExpression)
                    WriteLine($"return VisitExpression(_{ParamName} as Expr) as TRet;");
                else
                    WriteLine($"return Visit{className}(_{ParamName});");

                UnindentAndWriteCloseBrace();
            }

            WriteLine($"default:");
            WriteLineIndent($"throw new System.NotImplementedException(" +
                $"{ParamName}.StmtClass.ToString());");
        }

        private void GenerateConverter()
        {
            WriteLine("public unsafe class {0}Converter : {0}Visitor<AST.{0}>",
                BaseTypeName);
            WriteOpenBraceAndIndent();

            foreach (var @class in Declarations.OfType<Class>())
            {
                if (IsAbstractStmt(@class))
                    continue;

                PushBlock();
                WriteLine("public override AST.{0} Visit{1}({1} {2})",
                    BaseTypeName, @class.Name, ParamName);
                WriteOpenBraceAndIndent();

                var qualifiedName = $"{GetQualifiedName(@class)}";
                WriteLine($"var _{ParamName} = new AST.{qualifiedName}();");

                var classHierarchy = GetBaseClasses(@class);
                foreach (var baseClass in classHierarchy)
                    GenerateMembers(baseClass);

                WriteLine($"return _{ParamName};");

                UnindentAndWriteCloseBrace();
                PopBlock(NewLineKind.BeforeNextBlock);
            }

            UnindentAndWriteCloseBrace();
        }

        private void GenerateMembers(Class @class)
        {
            foreach (var property in @class.Properties.Where(p => p.IsGenerated))
            {
                if (SkipProperty(property))
                    continue;

                property.Visit(this);
            }

            foreach (var method in @class.Methods)
            {
                if (SkipMethod(method))
                    continue;

                method.Visit(this);
            }
        }

        public override bool VisitProperty(Property property)
        {
            var propertyName = GetDeclName(property, GeneratorKind.CSharp);
            Write($"_{ParamName}.{propertyName} = ");

            var bindingsProperty = $"{ParamName}.{propertyName}";

            var type = property.Type;
            var declTypeName = GetDeclTypeName(type, TypePrinter);
            MarshalDecl(type, declTypeName, bindingsProperty);
            WriteLine(";");

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            var managedName = GetDeclName(method, GeneratorKind.CSharp);
            var nativeName = CaseRenamePass.ConvertCaseString(method,
                RenameCasePattern.LowerCamelCase);

            WriteLine($"for (uint i = 0; i < {ParamName}.Get{nativeName}Count; i++)");
            WriteOpenBraceAndIndent();
            WriteLine($"var _E = {ParamName}.Get{nativeName}(i);");

            var bindingsType = GetIteratorType(method);
            var iteratorTypeName = GetIteratorTypeName(bindingsType, TypePrinter);

            Write($"_{ParamName}.{managedName}.Add(");
            MarshalDecl(bindingsType, iteratorTypeName, "_E");
            WriteLine(");");

            UnindentAndWriteCloseBrace();

            return true;
        }

        private void MarshalDecl(AST.Type type, string declTypeName, string bindingsName)
        {
            var typeName = $"AST.{declTypeName}";
            if (type.TryGetEnum(out Enumeration @enum))
                Write($"(AST.{GetQualifiedName(@enum, TypePrinter)}) {bindingsName}");
            else if (typeName.Contains("SourceLocation"))
                Write($"VisitSourceLocation({bindingsName})");
            else if (typeName.Contains("SourceRange"))
                Write($"VisitSourceRange({bindingsName})");
            else if (typeName.Contains("Stmt"))
                Write($"VisitStatement({bindingsName}) as {typeName}");
            else if (typeName.Contains("Expr"))
                Write($"VisitExpression({bindingsName}) as {typeName}");
            else if (typeName.Contains("Decl") || typeName.Contains("Function") ||
                     typeName.Contains("Method") || typeName.Contains("Field"))
                Write($"VisitDeclaration({bindingsName}) as {typeName}");
            else if (typeName.Contains("QualifiedType"))
                Write($"VisitQualifiedType({bindingsName})");
            else if (typeName.Contains("TemplateArgument"))
                Write($"VisitTemplateArgument({bindingsName})");
            else
                Write($"{bindingsName}");
        }
    }

    class StmtASTConverterCodeGenerator : ASTConverterCodeGenerator
    {
        public StmtASTConverterCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations, Enumeration stmtClassEnum)
            : base(context, declarations, stmtClassEnum)
        {
        }

        public override string BaseTypeName => "Stmt";
    }

    class ExprASTConverterCodeGenerator : ASTConverterCodeGenerator
    {
        public ExprASTConverterCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations, null)
        {
        }

        public override string BaseTypeName => "Expr";
    }

    #endregion

    #region Native code generators

    class StmtDeclarationsCodeGenerator : NativeParserCodeGenerator
    {
        public StmtDeclarationsCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public void GenerateDeclarations()
        {
            Process();
            WriteInclude("Sources.h", CInclude.IncludeKind.Quoted);
            WriteInclude("Types.h", CInclude.IncludeKind.Quoted);
            GenerateIncludes();
            NewLine();

            WriteLine("namespace CppSharp { namespace CppParser { namespace AST {");
            NewLine();

            WriteLine("class Expr;");
            WriteLine("class Declaration;");
            GenerateForwardDecls();
            NewLine();

            foreach (var decl in Declarations)
            {
                if (decl.Name == "GCCAsmStmt")
                {
                    WriteLine("class StringLiteral;");
                    NewLine();
                }

                decl.Visit(this);
            }

            NewLine();
            WriteLine("} } }");
        }

        public virtual void GenerateIncludes()
        {

        }

        public virtual void GenerateForwardDecls()
        {

        }

        public override bool GenerateClassBody(Class @class)
        {
            Unindent();
            WriteLine("public:");
            Indent();

            PushBlock();
            VisitDeclContext(@class);
            PopBlock(NewLineKind.Always);

            WriteLine($"{@class.Name}();");

            if (IsInheritedClass(@class))
                WriteLine($"{@class.Name}(StmtClass klass);");

            if (@class.Name == "Stmt")
                WriteLine("StmtClass stmtClass;");

            foreach (var method in @class.Methods)
            {
                if (SkipMethod(method))
                    continue;

                var iteratorType = GetIteratorType(method);
                string iteratorTypeName = GetIteratorTypeName(iteratorType,
                    CodeGeneratorHelpers.CppTypePrinter);

                WriteLine($"VECTOR({iteratorTypeName}, {method.Name})");
            }

            foreach (var property in @class.Properties)
            {
                if (SkipProperty(property))
                    continue;

                string typeName = GetDeclTypeName(property);
                WriteLine($"{typeName} {GetDeclName(property)};");
            }

            return true;
        }
    }

    class StmtDefinitionsCodeGenerator : NativeParserCodeGenerator
    {

        public StmtDefinitionsCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override bool GeneratePragmaOnce => false;

        public void GenerateDefinitions()
        {
            Process();

            GenerateIncludes();
            NewLine();

            WriteLine("namespace CppSharp { namespace CppParser { namespace AST {");
            NewLine();

            foreach (var decl in Declarations.OfType<Class>())
                decl.Visit(this);

            WriteLine("} } }");
        }

        public virtual void GenerateIncludes()
        {
            GenerateCommonIncludes();
            WriteInclude("Stmt.h", CInclude.IncludeKind.Quoted);
        }


        public override bool VisitClassDecl(Class @class)
        {
            VisitDeclContext(@class);

            var isStmt = @class.Name == "Stmt";
            if (!isStmt && !@class.HasBaseClass)
            {
                WriteLine($"{GetQualifiedName(@class)}::{@class.Name}()");
                WriteOpenBraceAndIndent();
                UnindentAndWriteCloseBrace();
                NewLine();
                return true;
            }

            WriteLine($"{@class.Name}::{@class.Name}()");
            var stmtMember = isStmt ? "stmtClass" : @class.BaseClass.Name;
            var stmtClass = IsAbstractStmt(@class) ? "NoStmt" : @class.Name;
            WriteLineIndent($": {stmtMember}(StmtClass::{stmtClass})");
            GenerateMemberInits(@class);
            WriteOpenBraceAndIndent();
            UnindentAndWriteCloseBrace();
            NewLine();

            var isInherited = IsInheritedClass(@class);
            if (isInherited)
            {
                WriteLine($"{@class.Name}::{@class.Name}(StmtClass klass)");
                var member = isStmt ? "stmtClass" : @class.BaseClass.Name;
                WriteLineIndent($": {member}(klass)");
                GenerateMemberInits(@class);
                WriteOpenBraceAndIndent();
                UnindentAndWriteCloseBrace();
                NewLine();
            }

            foreach (var method in @class.Methods)
            {
                if (SkipMethod(method))
                    continue;

                var iteratorType = GetIteratorType(method);
                string iteratorTypeName = GetIteratorTypeName(iteratorType,
                    CodeGeneratorHelpers.CppTypePrinter);

                WriteLine($"DEF_VECTOR({@class.Name}, {iteratorTypeName}, {method.Name})");
                NewLine();
            }

            return true;
        }

        private void GenerateMemberInits(Class @class)
        {
            foreach (var property in @class.Properties)
            {
                if (SkipProperty(property))
                    continue;

                var typeName = GetDeclTypeName(property);
                if (typeName == "std::string")
                    continue;

                WriteLineIndent($", {GetDeclName(property)}({GenerateInit(property)})");
            }
        }

        private string GenerateInit(Property property)
        {
            if (property.Type.IsPointer())
                return "nullptr";

            var typeName = GetDeclTypeName(property);
            if (property.Type.TryGetClass(out Class @class))
                return $"{typeName}()";

            if (property.Type.TryGetEnum(out Enumeration @enum))
                return $"({GetQualifiedName(@enum)}::{@enum.Items.First().Name})";

            return "0";
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            return true;
        }
    }

    class StmtParserCodeGenerator : NativeParserCodeGenerator
    {
        IEnumerable<Class> ExpressionClasses;

        public StmtParserCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations, IEnumerable<Class> exprs)
            : base(context, declarations)
        {
            ExpressionClasses = exprs;
        }

        public override bool GeneratePragmaOnce => false;

        public void GenerateParser()
        {
            Process();

            WriteInclude("AST.h", CInclude.IncludeKind.Quoted);
            WriteInclude("Parser.h", CInclude.IncludeKind.Quoted);
            GenerateIncludes();
            NewLine();

            WriteLine("namespace CppSharp { namespace CppParser {");
            NewLine();

            GenerateWalkStatement();

            NewLine();
            WriteLine("} }");
        }

        public virtual void GenerateIncludes()
        {
            WriteInclude("clang/AST/Stmt.h", CInclude.IncludeKind.Angled);
            WriteInclude("clang/AST/StmtCXX.h", CInclude.IncludeKind.Angled);
        }

        public virtual string MethodSig =>
            "AST::Stmt* Parser::WalkStatement(const clang::Stmt* Stmt)";

        public virtual string BaseTypeName => "Stmt";

        private void GenerateWalkStatement()
        {
            WriteLine(MethodSig);
            WriteOpenBraceAndIndent();

            WriteLine($"if ({BaseTypeName } == nullptr)");
            WriteLineIndent("return nullptr;");
            NewLine();

            WriteLine($"AST::{BaseTypeName}* _{BaseTypeName}= 0;");
            NewLine();

            WriteLine($"switch ({BaseTypeName}->getStmtClass())");
            WriteLine("{");

            foreach (var @class in Declarations.OfType<Class>())
            {
                if (IsAbstractStmt(@class))
                    continue;

                WriteLine($"case clang::Stmt::{@class.Name}Class:");
                WriteOpenBraceAndIndent();

                WriteLine($"auto S = const_cast<clang::{@class.Name}*>(" +
                    $"llvm::cast<clang::{@class.Name}>({BaseTypeName}));");
                WriteLine($"auto _S = new AST::{@class.Name}();");

                var classHierarchy = GetBaseClasses(@class);
                foreach (var baseClass in classHierarchy)
                    baseClass.Visit(this);

                WriteLine($"_{BaseTypeName} = _S;");
                WriteLine("break;");
                UnindentAndWriteCloseBrace();
            }

            if (ExpressionClasses != null)
            {
                foreach (var @class in ExpressionClasses.Where(c => !IsAbstractStmt(c)))
                    WriteLine($"case clang::Stmt::{@class.Name}Class:");

                WriteOpenBraceAndIndent();
                WriteLine("return WalkExpression(llvm::cast<clang::Expr>(Stmt));");
                UnindentAndWriteCloseBrace();
            }

            WriteLine("default:");
            WriteLineIndent("printf(\"Unhandled statement kind: %s\\n\"," +
                $" {BaseTypeName}->getStmtClassName());");

            WriteLine("}");
            NewLine();

            WriteLine($"return _{BaseTypeName};");

            UnindentAndWriteCloseBrace();
        }

        public override bool VisitClassDecl(Class @class)
        {
            foreach (var property in @class.Properties)
            {
                if (SkipProperty(property, skipBaseCheck: true))
                    continue;

                property.Visit(this);
            }

            foreach (var method in @class.Methods)
            {
                if (SkipMethod(method))
                    continue;

                method.Visit(this);
            }

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            var iteratorType = GetIteratorType(method);
            string iteratorTypeName = GetIteratorTypeName(iteratorType,
                CodeGeneratorHelpers.CppTypePrinter);

            WriteLine($"for (auto _E : S->{method.Name}())");
            WriteOpenBraceAndIndent();

            string walkMethod = string.Empty;
            if (iteratorTypeName.Contains("Decl"))
                walkMethod = "WalkDeclaration";
            else if (iteratorTypeName.Contains("Expr"))
                walkMethod = "WalkExpression";
            else if (iteratorTypeName.Contains("Stmt"))
                walkMethod = "WalkStatement";
            else
                throw new NotImplementedException();

            WriteLine($"auto _ES = {walkMethod}(_E);");
            WriteLine($"_S->add{method.Name}(_ES);");
            UnindentAndWriteCloseBrace();

            return true;
        }

        public override bool VisitProperty(Property property)
        {
            var typeName = GetDeclTypeName(property);
            var fieldName = GetDeclName(property);
            var methodName = property.GetMethod?.Name;

            var validMethod = $"is{FirstLetterToUpperCase(property.Name)}";
            var @class = property.Namespace as Class;
            var validMethodExists = @class.Methods.Exists(m => m.Name == validMethod)
                && methodName != validMethod;

            if (validMethodExists)
            {
                WriteLine($"if (S->{validMethod}())");
                Indent();
            }

            if (property.Type.TryGetEnum(out Enumeration @enum))
                WriteLine($"_S->{fieldName} = ({GetQualifiedName(@enum)}) S->{methodName}();");
            else if (typeName.Contains("SourceLocation"))
                return false;
            else if (typeName.Contains("SourceRange"))
                return false;
            else if (typeName.Contains("Stmt"))
                WriteLine($"_S->{fieldName} = static_cast<AST::{typeName}>(" +
                    $"WalkStatement(S->{methodName}()));");
            else if (typeName.Contains("Expr"))
                WriteLine($"_S->{fieldName} = static_cast<AST::{typeName}>(" +
                    $"WalkExpression(S->{methodName}()));");
            else if (typeName.Contains("Decl") || typeName.Contains("Method") ||
                     typeName.Contains("Function") || typeName.Contains("Field"))
                WriteLine($"_S->{fieldName} = static_cast<AST::{typeName}>(" +
                    $"WalkDeclaration(S->{methodName}()));");
            else if (typeName.Contains("TemplateArgument"))
                WriteLine($"_S->{fieldName} = WalkTemplateArgument(S->{methodName}());");
            else if (typeName.Contains("QualifiedType"))
                WriteLine($"_S->{fieldName} = GetQualifiedType(S->{methodName}());");
            else if (fieldName == "value" && @class.Bases.Exists(b => b.Class.Name.Contains("AP")))
            {
                // Use llvm::APInt or llvm::APFloat conversion methods
                methodName = property.Type.IsPrimitiveType(PrimitiveType.ULongLong) ?
                    "getLimitedValue" : "convertToDouble";
                WriteLine($"_S->{fieldName} = S->getValue().{methodName}();");
            }
            else
                WriteLine($"_S->{fieldName} = S->{methodName}();");

            if (validMethodExists)
                Unindent();

            return true;
        }
    }

    class ExprDeclarationsCodeGenerator : StmtDeclarationsCodeGenerator
    {
        public ExprDeclarationsCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override void GenerateIncludes()
        {
            WriteInclude("Stmt.h", CInclude.IncludeKind.Quoted);
        }

        public override void GenerateForwardDecls()
        {
            WriteLine("class Field;");
            WriteLine("class Method;");
            WriteLine("class Function;");
        }
    }

    class ExprDefinitionsCodeGenerator : StmtDefinitionsCodeGenerator
    {
        public ExprDefinitionsCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override bool GeneratePragmaOnce => false;

        public override void GenerateIncludes()
        {
            GenerateCommonIncludes();
            WriteInclude("Expr.h", CInclude.IncludeKind.Quoted);
        }
    }

    class ExprParserCodeGenerator : StmtParserCodeGenerator
    {
        public ExprParserCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations, null)
        {
        }

        public override void GenerateIncludes()
        {
            WriteInclude("clang/AST/Expr.h", CInclude.IncludeKind.Angled);
            WriteInclude("clang/AST/ExprCXX.h", CInclude.IncludeKind.Angled);
        }

        public override string BaseTypeName => "Expr";

        public override string MethodSig =>
            "AST::Expr* Parser::WalkExpression(const clang::Expr* Expr)";
    }

    class NativeParserCodeGenerator : Generators.C.CCodeGenerator
    {
        internal readonly IEnumerable<Declaration> Declarations;

        public NativeParserCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context)
        {
            Declarations = declarations;
        }

        public override string FileExtension => throw new NotImplementedException();

        public virtual bool GeneratePragmaOnce => true;

        public override void Process()
        {
            Context.Options.GeneratorKind = GeneratorKind.CPlusPlus;
            CTypePrinter.PushScope(TypePrintScopeKind.Local);

            GenerateFilePreamble(CommentKind.BCPL);
            NewLine();

            if (GeneratePragmaOnce)
                WriteLine("#pragma once");

            NewLine();
        }

        public void GenerateCommonIncludes()
        {
            WriteInclude("Sources.h", CInclude.IncludeKind.Quoted);
        }

        public override List<string> GenerateExtraClassSpecifiers(Class @class)
            => new List<string> { "CS_API" };

        public override bool VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            return true;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            return true;
        }

        public bool IsInheritedClass(Class @class)
        {
            foreach (var decl in Declarations.OfType<Class>())
            {
                foreach (var @base in decl.Bases)
                {
                    if (!@base.IsClass) continue;
                    if (@base.Class == @class)
                        return true;
                }
            }

            return false;
        }
    }

    static class CodeGeneratorHelpers
    {
        internal static CppTypePrinter CppTypePrinter;

        public static bool IsAbstractStmt(Class @class) => IsAbstractStmt(@class.Name);

        public static bool IsAbstractStmt(string className) =>
            className == "Stmt" ||
            className == "NoStmt" ||
            className == "SwitchCase" ||
            className == "AsmStmt" ||
            className == "Expr" ||
            className == "FullExpr" ||
            className == "CastExpr" ||
            className == "ExplicitCastExpr" ||
            className == "AbstractConditionalOperator" ||
            className == "CXXNamedCastExpr" ||
            className == "OverloadExpr" ||
            className == "CoroutineSuspendExpr";

        public static bool SkipProperty(Property property, bool skipBaseCheck = false)
        {
            if (!property.IsGenerated)
                return true;

            if (property.Access != AccessSpecifier.Public)
                return true;

            var @class = property.Namespace as Class;

            if (!skipBaseCheck)
            {
                if (@class.GetBaseProperty(property) != null)
                    return true;
            }

            if ((property.Name == "beginLoc" || property.Name == "endLoc") &&
                @class.Name != "Stmt")
                return true;

            switch (property.Name)
            {
                case "stmtClass":
                case "stmtClassName":
                    return true;
                case "isOMPStructuredBlock":
                    return true;
            }

            var typeName = property.Type.Visit(CppTypePrinter).Type;

            //
            // Statement properties.
            //

            if (typeName.Contains("LabelDecl") ||
                typeName.Contains("VarDecl") ||
                typeName.Contains("Token") ||
                typeName.Contains("CapturedDecl") ||
                typeName.Contains("CapturedRegionKind") ||
                typeName.Contains("RecordDecl") ||
                typeName.Contains("StringLiteral") ||
                typeName.Contains("SwitchCase") ||
                typeName.Contains("CharSourceRange") ||
                typeName.Contains("NestedNameSpecifierLoc") ||
                typeName.Contains("DeclarationNameInfo") ||
                typeName.Contains("DeclGroupRef"))
                return true;

            //
            // Expression properties.
            //

            // CastExpr
            if (property.Name == "targetUnionField")
                return true;

            // ShuffleVectorExprClass
            if (property.Name == "subExprs")
                return true;

            // InitListExprClass
            if (property.Name == "initializedFieldInUnion")
                return true;

            // ParenListExprClass
            if (property.Name == "exprs" && @class.Name == "ParenListExpr")
                return true;

            // EvalResult
            if (typeName.Contains("ExprValueKind") ||
                typeName.Contains("ExprObjectKind") ||
                typeName.Contains("ObjC"))
                return true;

            // DeclRefExpr
            if (typeName.Contains("ValueDecl") ||
                typeName.Contains("NestedNameSpecifier") ||
                typeName.Contains("TemplateArgumentLoc"))
                return true;

            // FloatingLiteral
            if (typeName.Contains("APFloatSemantics") ||
                typeName.Contains("fltSemantics") ||
                typeName.Contains("APFloat"))
                return true;

            // OffsetOfExpr
            // UnaryExprOrTypeTraitExpr
            // CompoundLiteralExpr
            // ExplicitCastExpr
            // ConvertVectorExpr
            // VAArgExpr
            if (typeName.Contains("TypeSourceInfo"))
                return true;

            // MemberExpr
            if (typeName.Contains("ValueDecl") ||
                typeName.Contains("DeclAccessPair") ||
                typeName.Contains("NestedNameSpecifier") ||
                typeName.Contains("TemplateArgumentLoc"))
                return true;

            // BinaryOperator
            if (typeName.Contains("FPOptions"))
                return true;

            // DesignatedInitExpr
            // ExtVectorElementExpr
            if (typeName.Contains("IdentifierInfo"))
                return true;

            // BlockExpr
            if (typeName.Contains("BlockDecl"))
                return true;

            // ArrayInitLoopExpr
            if (typeName.Contains("APInt"))
                return true;

            // MemberExpr
            if (typeName.Contains("BlockExpr") ||
                typeName.Contains("FunctionProtoType"))
                return true;

            //
            // C++ expression properties.
            //

            // MSPropertyRefExpr
            if (typeName.Contains("MSPropertyDecl"))
                return true;

            // CXXBindTemporaryExpr
            if (typeName.Contains("CXXTemporary"))
                return true;

            // CXXConstructExpr
            // CXXInheritedCtorInitExpr
            if (typeName.Contains("CXXConstructorDecl") ||
                typeName.Contains("ConstructionKind"))
                return true;

            // CXXInheritedCtorInitExpr
            if (typeName.Contains("LambdaCaptureDefault") ||
                typeName.Contains("TemplateParameterList"))
                return true;

            // CXXNewExpr
            if (property.Name == "placementArgs")
                return true;

            // TypeTraitExpr
            if (typeName == "TypeTrait")
                return true;

            // ArrayTypeTraitExpr
            if (typeName.Contains("ArrayTypeTrait"))
                return true;

            // ExpressionTraitExpr
            if (typeName.Contains("ExpressionTrait"))
                return true;

            // OverloadExpr
            // DependentScopeDeclRefExpr
            // UnresolvedMemberExpr
            if (typeName.Contains("DeclarationName"))
                return true;

            // PackExpansionExpr
            if (typeName.Contains("Optional<"))
                return true;

            // SubstNonTypeTemplateParmExpr
            // SubstNonTypeTemplateParmPackExpr
            if (typeName.Contains("NonTypeTemplateParmDecl"))
                return true;

            // MaterializeTemporaryExpr
            if (typeName.Contains("StorageDuration"))
                return true;

            // General properties.
            if (typeName.Contains("_iterator") ||
                typeName.Contains("_range"))
                return true;

            if (typeName.Contains("ArrayRef"))
                return true;

            // AtomicExpr
            if (typeName.Contains("unique_ptr<AtomicScopeModel, default_delete<AtomicScopeModel>>"))
                return true;

            if (typeName.Contains("Expr**"))
                return true;

            return false;
        }

        public static bool SkipMethod(Method method)
        {
            if (method.Ignore)
                return true;

            var @class = method.Namespace as Class;
            if (@class.GetBaseMethod(method) != null)
                return true;

            if (method.Name == "children")
                return true;

            // CastExpr
            if (method.Name == "path")
                return true;

            // CXXNewExpr
            if (method.Name == "placement_arguments" && method.IsConst)
                return true;

            var typeName = method.ReturnType.Visit(CppTypePrinter).Type;
            if (typeName.Contains("const"))
                return true;

            if (!typeName.Contains("range"))
                return true;

            // OverloadExpr
            if (typeName.Contains("UnresolvedSet"))
                return true;

            // LambdaExpr
            if (method.Name == "captures")
                return true;

            var iteratorType = GetIteratorType(method);
            string iteratorTypeName = GetIteratorTypeName(iteratorType, CppTypePrinter);
            if (iteratorTypeName.Contains("LambdaCapture"))
                return true;

            return false;
        }

        public static string GetQualifiedName(Declaration decl,
            TypePrinter typePrinter)
        {
            typePrinter.PushScope(TypePrintScopeKind.Qualified);
            var qualifiedName = decl.Visit(typePrinter).Type;
            typePrinter.PopScope();

            qualifiedName = CleanClangNamespaceFromName(qualifiedName);

            return qualifiedName;
        }

        public static string GetQualifiedName(Declaration decl) =>
            GetQualifiedName(decl, CppTypePrinter);

        private static string CleanClangNamespaceFromName(string qualifiedName)
        {
            qualifiedName = qualifiedName.StartsWith("clang::") ?
                qualifiedName.Substring("clang::".Length) : qualifiedName;

            qualifiedName = qualifiedName.StartsWith("clang.") ?
                qualifiedName.Substring("clang.".Length) : qualifiedName;

            return qualifiedName;
        }

        public static string GetDeclName(Declaration decl, GeneratorKind kind)
        {
            string name = decl.Name;

            if (kind == GeneratorKind.CPlusPlus)
            {
                if (Generators.C.CCodeGenerator.IsReservedKeyword(name))
                    name = $"_{name}";
            }
            else if (kind == GeneratorKind.CSharp)
            {
                bool hasConflict = false;
                switch (name)
                {
                    case "identKind":
                    case "literalOperatorKind":
                    case "initializationStyle":
                    case "capturedStmt":
                        hasConflict = true;
                        break;
                }

                if (!hasConflict)
                    name = CSharpSources.SafeIdentifier(
                        CaseRenamePass.ConvertCaseString(decl,
                            RenameCasePattern.UpperCamelCase));
            }
            else throw new NotImplementedException();

            return name;
        }

        public static string GetDeclName(Declaration decl)
        {
            return GetDeclName(decl, GeneratorKind.CPlusPlus);
        }

        public static AST.Type GetDeclType(AST.Type type,
            TypePrinter typePrinter)
        {
            var qualifiedType = new QualifiedType(type);
            if (qualifiedType.Type.IsPointerTo(out TagType tagType))
                qualifiedType = qualifiedType.StripConst();

            var typeName = qualifiedType.Type.Visit(typePrinter).Type;
            if (typeName.Contains("StringRef") || typeName.Contains("string"))
                type = new BuiltinType(PrimitiveType.String);

            return type;
        }

        public static string GetDeclTypeName(ITypedDecl decl) =>
            GetDeclTypeName(decl.Type, CppTypePrinter);

        public static string GetDeclTypeName(AST.Type type,
            TypePrinter typePrinter)
        {
            var declType = GetDeclType(type, typePrinter);
            var typeName = declType.Visit(typePrinter).Type;
            typeName = CleanClangNamespaceFromName(typeName);

            if (typeName.Contains("QualType"))
                typeName = "QualifiedType";

            if (typeName.Contains("UnaryOperator::Opcode"))
                typeName = "UnaryOperatorKind";

            if (typeName.Contains("BinaryOperator::Opcode"))
                typeName = "BinaryOperatorKind";

            string className = null;
            if (typeName.Contains("FieldDecl"))
                className = "Field";
            else if (typeName.Contains("NamedDecl"))
                className = "Declaration";
            else if (typeName.Contains("CXXMethodDecl"))
                className = "Method";
            else if (typeName.Contains("FunctionDecl"))
                className = "Function";
            else if (typeName == "Decl" || typeName == "Decl*")
                className = "Declaration";

            if (className != null)
                return (typePrinter is CppTypePrinter) ?
                 $"{className}*" : className;

            return typeName;
        }

        public static AST.Type GetIteratorType(Method method)
        {
            var retType = method.ReturnType.Type;

            TemplateSpecializationType templateSpecType;
            TypedefType typedefType;
            TypedefNameDecl typedefNameDecl;

            if (retType is TemplateSpecializationType)
            {
                templateSpecType = retType as TemplateSpecializationType;
                typedefType = templateSpecType.Arguments[0].Type.Type as TypedefType;
                typedefNameDecl = typedefType.Declaration as TypedefNameDecl;
            }
            else
            {
                typedefType = retType as TypedefType;
                typedefNameDecl = typedefType.Declaration as TypedefNameDecl;
                templateSpecType = typedefNameDecl.Type as TemplateSpecializationType;
                typedefType = templateSpecType.Arguments[0].Type.Type as TypedefType;
                typedefNameDecl = typedefType.Declaration as TypedefNameDecl;
                typedefType = typedefNameDecl.Type as TypedefType;
                if (typedefType != null)
                    typedefNameDecl = typedefType.Declaration as TypedefNameDecl;
            }

            var iteratorType = typedefNameDecl.Type;
            if (iteratorType.IsPointerTo(out PointerType pointee))
                iteratorType = iteratorType.GetPointee();

            return iteratorType;
        }

        public static string GetIteratorTypeName(AST.Type iteratorType,
            TypePrinter typePrinter)
        {
            if (iteratorType.IsPointer())
                iteratorType = iteratorType.GetFinalPointee();

            typePrinter.PushScope(TypePrintScopeKind.Qualified);
            var iteratorTypeName = iteratorType.Visit(typePrinter).Type;
            typePrinter.PopScope();

            iteratorTypeName = CleanClangNamespaceFromName(iteratorTypeName);

            if (iteratorTypeName.Contains("ExprIterator"))
                iteratorTypeName = "Expr";

            if (iteratorTypeName == "Decl")
                iteratorTypeName = "Declaration";

            if (typePrinter is CppTypePrinter)
                return $"{iteratorTypeName}*";

            return iteratorTypeName;
        }

        public static List<Class> GetBaseClasses(Class @class)
        {
            var baseClasses = new List<Class>();

            Class currentClass = @class;
            while (currentClass != null)
            {
                baseClasses.Add(currentClass);
                currentClass = currentClass.HasBaseClass ?
                    currentClass.BaseClass : null;
            }

            baseClasses.Reverse();
            return baseClasses;
        }

        public static string RemoveFromEnd(string s, string suffix)
        {
            return s.EndsWith(suffix) ? s.Substring(0, s.Length - suffix.Length) : s;
        }

        public static string FirstLetterToUpperCase(string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentException("There is no first letter");

            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }

    #endregion
}
