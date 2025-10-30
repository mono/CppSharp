using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.C;
using CppSharp.Generators.CSharp;

using static CppSharp.CodeGeneratorHelpers;

namespace CppSharp
{
    internal class DeclDeclarationsCodeGenerator : DeclarationsCodeGenerator
    {
        public DeclDeclarationsCodeGenerator(BindingContext context, IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override string BaseTypeName => "Decl";

        public override void GenerateIncludes()
        {
            WriteInclude("Sources.h", CInclude.IncludeKind.Quoted);
            WriteInclude("Types.h", CInclude.IncludeKind.Quoted);
            WriteInclude("string", CInclude.IncludeKind.Angled);
        }

        public override void GenerateForwardDecls()
        {
            WriteLine("class Expr;");
            WriteLine("class Stmt;");
            WriteLine("class LabelStmt;");
            WriteLine("class EvaluatedStmt;");
            WriteLine("class CompoundStmt;");
            WriteLine("enum class OverloadedOperatorKind;");
            NewLine();
            WriteLine("class Declaration;");
            WriteLine("class UsingDirectiveDecl;");
            WriteLine("class FriendDecl;");
            WriteLine("class ConstructorUsingShadowDecl;");
            WriteLine("class LinkageSpecDecl;");
            WriteLine("class NamedDecl;");
            WriteLine("class NamespaceDecl;");
            WriteLine("class TemplateDecl;");
            WriteLine("class ClassTemplateDecl;");
            WriteLine("class VarTemplateDecl;");
            WriteLine("class TypeAliasTemplateDecl;");
            WriteLine("class FunctionDecl;");
            WriteLine("class FunctionTemplateDecl;");
            WriteLine("class CXXMethodDecl;");
            WriteLine("class CXXConstructorDecl;");
            WriteLine("class CXXDestructorDecl;");
            WriteLine("class BaseUsingDecl;");
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (SkipClass(@class))
                return false;

            @class.Bases.Find(x => x.Class?.Name == "Node")
                ?.ExplicitlyIgnore();

            return base.VisitClassDecl(@class);
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

            foreach (var method in @class.Methods)
            {
                if (SkipMethod(method))
                    continue;

                var iteratorType = GetIteratorType(method);
                string iteratorTypeName = GetIteratorTypeName(iteratorType, CodeGeneratorHelpers.CppTypePrinter);

                WriteLine($"VECTOR({iteratorTypeName}, {method.Name})");
            }

            foreach (var property in @class.Properties)
            {
                if (SkipProperty(property) || property.IsStatic)
                    continue;

                string typeName = GetDeclTypeName(property);

                if (typeName.StartsWith("TemplateArgumentList"))
                {
                    WriteLine($"VECTOR(TemplateArgument*, {GetDeclName(property)})");
                }
                else if (typeName.StartsWith("TemplateParameterList"))
                {
                    WriteLine($"VECTOR(NamedDecl*, {GetDeclName(property)})");
                }
                else
                {
                    WriteLine($"{typeName} {GetDeclName(property)};");
                }
            }

            return true;
        }
    }

    internal class DeclDefinitionsCodeGenerator : DefinitionsCodeGenerator
    {
        public DeclDefinitionsCodeGenerator(BindingContext context, IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override string BaseTypeName => "Decl";

        public override void GenerateIncludes()
        {
            GenerateCommonIncludes();
            NewLine();
            WriteInclude("Decl.h", CInclude.IncludeKind.Quoted);
            WriteInclude("Types.h", CInclude.IncludeKind.Quoted);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!@class.IsGenerated)
                return false;

            VisitDeclContext(@class);

            var isBaseType = @class.Name == BaseTypeName;
            if (!isBaseType && !@class.HasBaseClass)
            {
                WriteLine($"{GetQualifiedName(@class)}::{@class.Name}()");
                WriteOpenBraceAndIndent();
                UnindentAndWriteCloseBrace();
                NewLine();
                return true;
            }

            WriteLine($"{@class.Name}::{@class.Name}()");
            GenerateMemberInits(@class);
            WriteOpenBraceAndIndent();
            UnindentAndWriteCloseBrace();
            NewLine();

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
    }

    internal class DeclASTConverterCodeGenerator : ASTConverterCodeGenerator
    {
        public DeclASTConverterCodeGenerator(BindingContext context, IEnumerable<Declaration> declarations)
            : base(context, declarations)
        {
        }

        public override string BaseTypeName => "Decl";

        public override bool IsAbstractASTNode(Class kind)
        {
            return IsAbstractDecl(kind);
        }

        protected override void GenerateVisitorSwitch(IEnumerable<string> classes)
        {
            WriteLine($"switch({ParamName}.DeclKind)");
            WriteOpenBraceAndIndent();

            GenerateSwitchCases(classes);

            UnindentAndWriteCloseBrace();
        }

        private void GenerateSwitchCases(IEnumerable<string> classes)
        {
            foreach (var className in classes)
            {
                WriteLine($"case Parser.AST.{BaseTypeName}Kind.{className}:");
                WriteLineIndent($"return Visit{className}({ParamName} as Parser.AST.{className}Decl);");
            }
            foreach (var className in classes)
            {
                WriteLine($"case StmtClass.{className}:");
                WriteOpenBraceAndIndent();

                WriteLine($"var _{ParamName} = {className}.__CreateInstance({ParamName}.__Instance);");

                WriteLine($"return Visit{className}(_{ParamName});");

                UnindentAndWriteCloseBrace();
            }

            WriteLine("default:");
            WriteLineIndent($"throw new System.NotImplementedException({ParamName}.DeclKind.ToString());");
        }
    }
}