using System.Collections.Generic;

namespace Cxxi
{
    public interface IAstVisited
    {
        ISet<object> Visited { get; }

        bool AlreadyVisited(Declaration decl);
        bool AlreadyVisited(Type type);
    }

    public interface IAstVisitor<out T> : ITypeVisitor<T>, IDeclVisitor<T>
    {
        
    }

    /// <summary>
    /// Base class for AST visitors.
    /// You can override the methods to customize the behaviour, by default
    /// this will visit all the nodes in a default way that should be useful
    /// for a lot of applications.
    /// </summary>
    public abstract class AstVisitor : IAstVisitor<bool>, IAstVisited
    {
        public ISet<object> Visited { get; private set; }

        protected AstVisitor()
        {
            Visited = new HashSet<object>();
        }

        public bool AlreadyVisited(Type type)
        {
            return !Visited.Add(type);
        }

        public bool AlreadyVisited(Declaration decl)
        {
            return  !Visited.Add(decl);
        }

        #region Type Visitors

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

        public virtual bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            return true;
        }

        #endregion

        #region Decl Visitors

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
            if (AlreadyVisited(@class))
                return true;

            return VisitDeclaration(@class);
        }

        public virtual bool VisitFieldDecl(Field field)
        {
            return field.Type.Visit(this, field.QualifiedType.Qualifiers);
        }

        public virtual bool VisitFunctionDecl(Function function)
        {
            if (function.ReturnType != null)
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
            return parameter.Type.Visit(this, parameter.QualifiedType.Qualifiers);
        }

        public virtual bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (typedef.Type == null)
                return false;
            return typedef.Type.Visit(this, typedef.QualifiedType.Qualifiers);
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

        public bool VisitNamespace(Namespace @namespace)
        {
            foreach (var decl in @namespace.Classes)
                decl.Visit(this);

            foreach (var decl in @namespace.Functions)
                decl.Visit(this);

            foreach (var decl in @namespace.Enums)
                decl.Visit(this);

            foreach (var decl in @namespace.Templates)
                decl.Visit(this);

            foreach (var decl in @namespace.Typedefs)
                decl.Visit(this);

            foreach (var decl in @namespace.Namespaces)
                decl.Visit(this);

            return true;
        }

        #endregion
    }
}