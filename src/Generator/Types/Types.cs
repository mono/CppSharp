using System.Collections.Generic;
using Cxxi.Generators;
using Cxxi.Types;

namespace Cxxi
{
    /// <summary>
    /// This type checker is used to check if a type is complete.
    /// </summary>
    public class TypeCompletionChecker : AstVisitor
    {
        public override bool VisitDeclaration(Declaration decl)
        {
            return !decl.IsIncomplete;
        }

        public override bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return true;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            if (!function.ReturnType.Visit(this))
                return false;

            foreach (var arg in function.Arguments)
            {
                if (!arg.Type.Visit(this))
                    return false;
            }

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.ReturnType.Visit(this))
                return false;

            foreach (var param in function.Parameters)
            {
                if (!param.Visit(this))
                    return false;
            }

            return true;
        }
    }

    /// <summary>
    /// This type checker is used to check if a type is ignored.
    /// </summary>
    public class TypeIgnoreChecker : AstVisitor
    {
        ITypeMapDatabase TypeMapDatabase { get; set; }

        public TypeIgnoreChecker(ITypeMapDatabase database)
        {
            TypeMapDatabase = database;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            return decl.Ignore;
        }

        public override bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return false;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            if (!function.ReturnType.Visit(this))
                return false;

            foreach (var arg in function.Arguments)
            {
                if (!arg.Type.Visit(this))
                    return false;
            }

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!function.ReturnType.Visit(this))
                return false;

            foreach (var param in function.Parameters)
            {
                if (!param.Visit(this))
                    return false;
            }

            return true;
        }

        public override bool VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                if (typeMap.IsIgnored)
                    return true;
            }

            return base.VisitTypedefType(typedef, quals);
        }

        public override bool VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            var decl = template.Template.TemplatedDecl;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                if (typeMap.IsIgnored)
                    return true;
            }

            return base.VisitTemplateSpecializationType(template, quals);
        }
    }

    /// <summary>
    /// This is used to get the declarations that each file needs to forward
    /// reference or include from other header files. Since in C++ everything
    /// that is referenced needs to have been declared previously, it can happen
    /// that a file needs to be reference something that has not been declared
    /// yet. In that case, we need to declare it before referencing it.
    /// </summary>
    class TypeRefsVisitor : AstVisitor
    {
        public ISet<Declaration> ForwardReferences;

        public void Collect(Declaration declaration)
        {
            var @namespace = declaration.Namespace;

            if (@namespace != null)
                if (@namespace.TranslationUnit.IsSystemHeader)
                    return;

            ForwardReferences.Add(declaration);
        }

        public TypeRefsVisitor()
        {
            ForwardReferences = new HashSet<Declaration>();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (AlreadyVisited(@class))
                return true;

            Collect(@class);

            // If the class is incomplete, then we cannot process the record
            // members, else it will add references to declarations that
            // should have not been found.
            if (@class.IsIncomplete)
                return true;

            foreach (var field in @class.Fields)
                VisitFieldDecl(field);

            foreach (var method in @class.Methods)
                VisitMethodDecl(method);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Collect(@enum);
            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            Class @class;
            if (field.Type.IsTagDecl(out @class))
            {
                if (@class.IsValueType)
                    Collect(@field);
            }
            else
            {
                field.Type.Visit(this);
            }

            return true;
        }

        public override bool VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            if (decl.Type == null)
                return false;

            FunctionType function;
            if (decl.Type.IsPointerTo<FunctionType>(out function))
            {
                Collect(decl);
            }

            return decl.Type.Visit(this);
        }
    }
}
