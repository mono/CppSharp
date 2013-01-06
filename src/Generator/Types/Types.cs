using System.Collections.Generic;

namespace Cxxi
{
    /// <summary>
    /// Base class for type checkers.
    /// You can override the methods to customize the behaviour, by default
    /// this will visit all the nodes in a default way that should be useful
    /// for a lot of applications.
    /// </summary>
    public abstract class TypeChecker : ITypeVisitor<bool>, IDeclVisitor<bool>
    {
        public virtual bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            return tag.Declaration.Visit(this);
        }

        public virtual bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            return array.Type.Visit(this, quals);
        }

        public virtual bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            if (function.ReturnType != null)
                function.ReturnType.Visit(this);

            foreach (var param in function.Arguments)
                param.Visit(this);

            return true;
        }

        public virtual bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (pointer.Pointee == null)
                return false;
            return pointer.Pointee.Visit(this, quals);
        }

        public virtual bool VisitMemberPointerType(MemberPointerType member, TypeQualifiers quals)
        {
            return member.Pointee.Visit(this, quals);
        }

        public virtual bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return true;
        }

        public virtual bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            return typedef.Declaration.Visit(this);
        }

        public virtual bool VisitTemplateSpecializationType(TemplateSpecializationType template, TypeQualifiers quals)
        {
            return template.Template.Visit(this);
        }

        public virtual bool VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public virtual bool VisitDeclaration(Declaration decl)
        {
            return true;
        }

        public virtual bool VisitClassDecl(Class @class)
        {
            return VisitDeclaration(@class);
        }

        public virtual bool VisitFieldDecl(Field field)
        {
            return field.Type.Visit(this);
        }

        public virtual bool VisitFunctionDecl(Function function)
        {
            if (function.ReturnType == null)
                return false;

            function.ReturnType.Visit(this);

            foreach (var param in function.Parameters)
                param.Visit(this);

            return true;
        }    

        public virtual bool VisitMethodDecl(Method method)
        {
            return VisitFunctionDecl(method);
        }

        public virtual bool VisitParameterDecl(Parameter parameter)
        {
            return parameter.Type.Visit(this);
        }

        public virtual bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (typedef.Type == null)
                return false;
            return typedef.Type.Visit(this);
        }

        public virtual bool VisitEnumDecl(Enumeration @enum)
        {
            return VisitDeclaration(@enum);
        }

        public virtual bool VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.TemplatedClass.Visit(this);
        }

        public virtual bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return template.TemplatedFunction.Visit(this);
        }

        public virtual bool VisitMacroDefinition(MacroDefinition macro)
        {
            return true;
        }
    }

    /// <summary>
    /// This type checker is used to check if a type is complete.
    /// </summary>
    public class TypeCompletionChecker : TypeChecker
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
    public class TypeIgnoreChecker : TypeChecker
    {
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
    class TypeRefsVisitor : TypeChecker
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
