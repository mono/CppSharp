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
            if (declaration.Namespace.TranslationUnit.IsSystemHeader)
                return;

            ForwardReferences.Add(declaration);
        }

        public TypeRefsVisitor()
        {
            ForwardReferences = new HashSet<Declaration>();
        }

        public override bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            // Built-in types should not need forward references.
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            Collect(@class);
            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            if (parameter.Type == null)
                return false;

            return parameter.Type.Visit(this);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            Collect(@enum);
            return @enum.Type.Visit(this, new TypeQualifiers());
        }

        public override bool VisitMacroDefinition(MacroDefinition macro)
        {
            // Macros are not relevant for forward references.
            return true;
        }
    }
}
