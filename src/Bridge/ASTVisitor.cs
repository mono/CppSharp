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

            foreach (var param in function.Parameters)
                param.Visit(this);

            return true;
        }

        public virtual bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (pointer.Pointee == null)
                return false;

            return pointer.Pointee.Visit(this, quals);
        }

        public virtual bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
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

        public virtual bool VisitTemplateSpecializationType(TemplateSpecializationType template,
            TypeQualifiers quals)
        {
            foreach (var arg in template.Arguments)
            {
                switch (arg.Kind)
                {
                    case TemplateArgument.ArgumentKind.Type:
                        var type = arg.Type.Type;
                        if (type != null)
                            type.Visit(this, arg.Type.Qualifiers);
                        break;
                    case TemplateArgument.ArgumentKind.Declaration:
                        arg.Declaration.Visit(this);
                        break;
                }
            }

            return template.Template.Visit(this);
        }

        public virtual bool VisitTemplateParameterType(TemplateParameterType param,
            TypeQualifiers quals)
        {
            return true;
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

            if (!VisitDeclaration(@class))
                return false;

            foreach (var baseClass in @class.Bases)
                if (baseClass.IsClass)
                    baseClass.Class.Visit(this);

            foreach (var field in @class.Fields)
                VisitFieldDecl(field);

            foreach (var property in @class.Properties)
                VisitProperty(property);

            foreach (var method in @class.Methods)
                VisitMethodDecl(method);

            foreach (var @event in @class.Events)
                VisitEvent(@event);

            return true;
        }

        public virtual bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            return field.Type.Visit(this, field.QualifiedType.Qualifiers);
        }

        public virtual bool VisitProperty(Property property)
        {
            if (!VisitDeclaration(property))
                return false;

            return property.Type.Visit(this);
        }

        public virtual bool VisitFunctionDecl(Function function)
        {
            if (!VisitDeclaration(function))
                return false;

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
            if (!VisitDeclaration(parameter))
                return false; 
            
            return parameter.Type.Visit(this, parameter.QualifiedType.Qualifiers);
        }

        public virtual bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (!VisitDeclaration(typedef))
                return false;

            if (typedef.Type == null)
                return false;

            return typedef.Type.Visit(this, typedef.QualifiedType.Qualifiers);
        }

        public virtual bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            foreach (var item in @enum.Items)
                VisitEnumItem(item);

            return true;
        }

        public virtual bool VisitEnumItem(Enumeration.Item item)
        {
            return true;
        }

        public virtual bool VisitClassTemplateDecl(ClassTemplate template)
        {
            if (!VisitDeclaration(template))
                return false;

            return template.TemplatedClass.Visit(this);
        }

        public virtual bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            if (!VisitDeclaration(template))
                return false; 
            
            return template.TemplatedFunction.Visit(this);
        }

        public virtual bool VisitMacroDefinition(MacroDefinition macro)
        {
            return VisitDeclaration(macro);
        }

        public virtual bool VisitNamespace(Namespace @namespace)
        {
            if (!VisitDeclaration(@namespace))
                return false; 

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

        public virtual bool VisitEvent(Event @event)
        {
            return VisitDeclaration(@event);
        }

        #endregion
    }
}