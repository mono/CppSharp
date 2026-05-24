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
    class DeclParserCodeGenerator : StmtParserCodeGenerator
    {
        public DeclParserCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations, null)
        {
        }

        public override void GenerateIncludes()
        {
            WriteInclude("clang/AST/Decl.h", CInclude.IncludeKind.Angled);
            WriteInclude("clang/AST/DeclCXX.h", CInclude.IncludeKind.Angled);
            WriteInclude("clang/AST/DeclTemplate.h", CInclude.IncludeKind.Angled);
        }

        public override string BaseTypeName => "Decl";

        public override string MethodSig =>
            "AST::Decl* Parser::WalkDeclaration(const clang::Decl* Decl)";

        public override bool VisitProperty(Property property)
        {
            if (SkipDeclProperty(property))
                return false;

            var typeName = GetDeclTypeName(property);
            var fieldName = GetDeclName(property);
            var methodName = property.GetMethod?.Name;

            Write($"_D->{fieldName} = ");
            if (property.Type.TryGetClass(out Class @class))
            {
                if (@class.Name.Contains("Type"))
                    WriteLine($"GetQualifiedType(D->{methodName}());");
                else if (@class.Name.Contains("Decl"))
                    WriteLine($"WalkDeclaration(D->{methodName}());");
                else
                    WriteLine($"D->{methodName}();");
            }
            else
                WriteLine($"D->{methodName}();");

            return true;
        }
    }

    class TypeParserCodeGenerator : StmtParserCodeGenerator
    {
        public TypeParserCodeGenerator(BindingContext context,
            IEnumerable<Declaration> declarations)
            : base(context, declarations, null)
        {
        }

        public override void GenerateIncludes()
        {
            WriteInclude("clang/AST/Type.h", CInclude.IncludeKind.Angled);
            WriteInclude("clang/AST/CXXType.h", CInclude.IncludeKind.Angled);
        }

        public override string BaseTypeName => "Type";

        public override string MethodSig =>
            "AST::Type* Parser::WalkType(const clang::Type* Type)";

        public override bool VisitProperty(Property property)
        {
            if (CodeGeneratorHelpers.SkipTypeProperty(property))
                return false;

            var typeName = GetDeclTypeName(property);
            var fieldName = GetDeclName(property);
            var methodName = property.GetMethod?.Name;

            WriteLine($"_T->{fieldName} = ");
            if (property.Type.TryGetClass(out Class @class))
            {
                if (@class.Name.Contains("Type"))
                    WriteLine($"GetQualifiedType(T->{methodName}());");
                else if (@class.Name.Contains("Decl"))
                    WriteLine($"WalkDeclaration(T->{methodName}());");
                else
                    WriteLine($"T->{methodName}();");
            }
            else
                WriteLine($"T->{methodName}();");

            return true;
        }
    }
}