using CppSharp.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;

using static CppSharp.CodeGeneratorHelpers;

namespace CppSharp
{
    internal class StmtDeclarationsCodeGenerator : NativeParserCodeGenerator
    {
        public StmtDeclarationsCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public void GenerateDeclarations()
        {
            Process();
            GenerateIncludes();
            NewLine();

            WriteLine("namespace CppSharp::CppParser::AST {");
            NewLine();

            GenerateForwardDecls();
            NewLine();

            foreach (var decl in Declarations)
            {
                if (decl.Name == "GCCAsmStmt")
                {
                    WriteLine("class StringLiteral;");
                    WriteLine("class AddrLabelExpr;");
                    NewLine();
                }

                decl.Visit(this);
            }

            NewLine();
            WriteLine("}");
        }

        public virtual void GenerateIncludes()
        {
            WriteInclude("Sources.h", CInclude.IncludeKind.Quoted);
            WriteInclude("Types.h", CInclude.IncludeKind.Quoted);
        }

        public virtual void GenerateForwardDecls()
        {
            WriteLine("class Expr;");
            WriteLine("class Declaration;");
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

    internal class StmtDefinitionsCodeGenerator : NativeParserCodeGenerator
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

            WriteLine("namespace CppSharp::CppParser::AST {");
            NewLine();

            foreach (var decl in Declarations.OfType<Class>())
                decl.Visit(this);

            WriteLine("}");
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

        internal void GenerateMemberInits(Class @class)
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

            if (property.Type.IsPrimitiveType(PrimitiveType.Bool))
                return "false";

            var typeName = GetDeclTypeName(property);
            if (property.Type.TryGetClass(out Class _))
                return $"{typeName}()";

            if (property.Type.TryGetEnum(out Enumeration @enum))
                return $"{GetQualifiedName(@enum)}::{@enum.Items.First().Name}";

            return "0";
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            return true;
        }
    }

    internal class StmtParserCodeGenerator : NativeParserCodeGenerator
    {
        private IEnumerable<Class> ExpressionClasses;

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

            WriteLine("namespace CppSharp::CppParser {");
            NewLine();

            GenerateWalkStatement();

            NewLine();
            WriteLine("}");
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

            WriteLine($"if (!{BaseTypeName})");
            WriteLineIndent("return nullptr;");
            NewLine();

            WriteLine($"AST::{BaseTypeName}* _{BaseTypeName}= nullptr;");
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

            bool isBaseType = iteratorTypeName switch
            {
                "Declaration*" or "Expr*" or "Stmt*" => true,
                _ => false
            };

            string walkMethod;
            if (iteratorTypeName.Contains("Decl"))
            {
                walkMethod = "WalkDeclaration";
            }
            else if (iteratorTypeName.Contains("Expr"))
            {
                walkMethod = "WalkExpression";
            }
            else if (iteratorTypeName.Contains("Stmt"))
            {
                walkMethod = "WalkStatement";
            }
            else
            {
                throw new NotImplementedException();
            }

            WriteLine("auto _ES = {0}{1}(_E);", isBaseType ? string.Empty : $"(AST::{iteratorTypeName})", walkMethod);
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
            var @class = (property.Namespace as Class)!;
            var validMethodExists = @class.Methods.Exists(m => m.Name == validMethod)
                && methodName != validMethod;

            if (validMethodExists)
            {
                WriteLine($"if (S->{validMethod}())");
                Indent();
            }

            if (property.Type.TryGetEnum(out Enumeration @enum))
                WriteLine($"_S->{fieldName} = (AST::{GetQualifiedName(@enum)})S->{methodName}();");
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
            else if (fieldName == "guidDecl")
                WriteLine($"_S->{fieldName} = S->getGuidDecl()->getNameAsString();");
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

    internal class StmtASTConverterCodeGenerator : ASTConverterCodeGenerator
    {
        private readonly Enumeration StmtClassEnum;

        public StmtASTConverterCodeGenerator(BindingContext context, IEnumerable<Declaration> declarations, Enumeration stmtClassEnum)
            : base(context, declarations)
        {
            StmtClassEnum = stmtClassEnum;
        }

        public override string BaseTypeName => "Stmt";

        public override bool IsAbstractASTNode(Class kind)
        {
            return CodeGeneratorHelpers.IsAbstractStmt(kind);
        }

        protected override void GenerateVisitorSwitch(IEnumerable<string> classes)
        {
            WriteLine($"switch({ParamName}.StmtClass)");
            WriteOpenBraceAndIndent();

            var enumItems = StmtClassEnum != null ?
                StmtClassEnum.Items.Where(item => item.IsGenerated)
                    .Select(item => RemoveFromEnd(item.Name, "Class"))
                    .Where(@class => !IsAbstractStmt(@class))
                : new List<string>();

            GenerateSwitchCases(StmtClassEnum != null ? enumItems : classes);

            UnindentAndWriteCloseBrace();
        }

        private void GenerateSwitchCases(IEnumerable<string> classes)
        {
            foreach (var className in classes)
            {
                WriteLine($"case StmtClass.{className}:");
                WriteOpenBraceAndIndent();

                WriteLine($"var _{ParamName} = {className}.__CreateInstance({ParamName}.__Instance);");

                var isExpression = Declarations
                    .OfType<Class>()
                    .All(c => c.Name != className);

                if (isExpression)
                    WriteLine($"return VisitExpression(_{ParamName} as Expr) as TRet;");
                else
                    WriteLine($"return Visit{className}(_{ParamName});");

                UnindentAndWriteCloseBrace();
            }

            WriteLine("default:");
            WriteLineIndent($"throw new System.NotImplementedException({ParamName}.StmtClass.ToString());");
        }
    }

    internal class ExprDeclarationsCodeGenerator : StmtDeclarationsCodeGenerator
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
            WriteLine("class FunctionTemplate;");
        }
    }

    internal class ExprDefinitionsCodeGenerator : StmtDefinitionsCodeGenerator
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

    internal class ExprParserCodeGenerator : StmtParserCodeGenerator
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

    internal class ExprASTConverterCodeGenerator : StmtASTConverterCodeGenerator
    {
        public ExprASTConverterCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations, null)
        {
        }

        public override string BaseTypeName => "Expr";

        public override bool IsAbstractASTNode(Class kind)
        {
            return CodeGeneratorHelpers.IsAbstractStmt(kind);
        }

        protected override void GenerateVisitorSwitch(IEnumerable<string> classes)
        {
            WriteLine($"switch({ParamName}.StmtClass)");
            WriteOpenBraceAndIndent();

            GenerateSwitchCases(classes);

            UnindentAndWriteCloseBrace();
        }

        private void GenerateSwitchCases(IEnumerable<string> classes)
        {
            foreach (var className in classes)
            {
                WriteLine($"case StmtClass.{className}:");
                WriteOpenBraceAndIndent();

                WriteLine($"var _{ParamName} = {className}.__CreateInstance({ParamName}.__Instance);");

                var isExpression = Declarations
                    .OfType<Class>()
                    .All(c => c.Name != className);

                if (isExpression)
                    WriteLine($"return VisitExpression(_{ParamName} as Expr) as TRet;");
                else
                    WriteLine($"return Visit{className}(_{ParamName});");

                UnindentAndWriteCloseBrace();
            }

            WriteLine("default:");
            WriteLineIndent($"throw new System.NotImplementedException({ParamName}.StmtClass.ToString());");
        }
    }
}
