using System.Collections.Generic;

namespace CppSharp.AST
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

    public class AstVisitorOptions
    {
        public bool VisitDeclaration = true;
        public bool VisitClassBases = true;
        public bool VisitClassFields = true;
        public bool VisitClassProperties = true;
        public bool VisitClassMethods = true;

        public bool VisitNamespaceEnums = true;
        public bool VisitNamespaceTemplates = true;
        public bool VisitNamespaceTypedefs = true;
        public bool VisitNamespaceEvents = true;
        public bool VisitNamespaceVariables = true;

        public bool VisitFunctionReturnType = true;
        public bool VisitFunctionParameters = true;
        public bool VisitTemplateArguments = true;
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
        public AstVisitorOptions Options { get; private set; }

        protected AstVisitor()
        {
            Visited = new HashSet<object>();
            Options = new AstVisitorOptions();
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

        public virtual bool VisitType(Type type, TypeQualifiers quals)
        {
            return true;
        }

        public virtual bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (!VisitType(tag, quals))
                return false;

            return tag.Declaration.Visit(this);
        }

        public virtual bool VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            if (!VisitType(array, quals))
                return false;

            // FIXME: Remove this once array dependent types are processed.
            if (array.SizeType == ArrayType.ArraySize.Dependent)
                return false;

            return array.QualifiedType.Visit(this);
        }

        public virtual bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            if (!VisitType(function, quals))
                return false;

            if (function.ReturnType.Type != null)
                function.ReturnType.Visit(this);

            if (Options.VisitFunctionParameters)
                foreach (var param in function.Parameters)
                    param.Visit(this);

            return true;
        }

        public virtual bool VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (!VisitType(pointer, quals))
                return false;

            if (pointer.Pointee == null)
                return false;

            return pointer.Pointee.Visit(this, quals);
        }

        public virtual bool VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            if (!VisitType(member, quals))
                return false;

            return member.QualifiedPointee.Visit(this);
        }

        public virtual bool VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            if (!VisitType(builtin, quals))
                return false;

            return VisitPrimitiveType(builtin.Type, quals);
        }

        public virtual bool VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            if (!VisitType(typedef, quals))
                return false;

            return typedef.Declaration.Visit(this);
        }

        public bool VisitAttributedType(AttributedType attributed, TypeQualifiers quals)
        {
            if (!VisitType(attributed, quals))
                return false;

            return attributed.Modified.Visit(this);
        }

        public virtual bool VisitDecayedType(DecayedType decayed, TypeQualifiers quals)
        {
            if (!VisitType(decayed, quals))
                return false;

            return decayed.Decayed.Visit(this);
        }

        public virtual bool VisitTemplateSpecializationType(TemplateSpecializationType template,
            TypeQualifiers quals)
        {
            if (!VisitType(template, quals))
                return false;

            if (Options.VisitTemplateArguments)
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
            }

            return template.Template.Visit(this);
        }

        public virtual bool VisitTemplateParameterType(TemplateParameterType param,
            TypeQualifiers quals)
        {
            if (!VisitType(param, quals))
                return false;

            return true;
        }

        public virtual bool VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            if (!VisitType(param, quals))
                return false;

            return param.Replacement.Type.Visit(this, quals);
        }

        public virtual bool VisitInjectedClassNameType(InjectedClassNameType injected,
            TypeQualifiers quals)
        {
            if (!VisitType(injected, quals))
                return false;

            return true;
        }

        public virtual bool VisitDependentNameType(DependentNameType dependent,
            TypeQualifiers quals)
        {
            if (!VisitType(dependent, quals))
                return false;

            return true;
        }

        public bool VisitPackExpansionType(PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            return true;
        }

        public virtual bool VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            return true;
        }

        public virtual bool VisitCILType(CILType type, TypeQualifiers quals)
        {
            if (!VisitType(type, quals))
                return false;

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
            return !AlreadyVisited(decl);
        }

        public virtual bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclarationContext(@class))
                return false;

            if (Options.VisitClassBases)
                foreach (var baseClass in @class.Bases)
                    if (baseClass.IsClass)
                        VisitClassDecl(baseClass.Class);

            if (Options.VisitClassFields)
                foreach (var field in @class.Fields)
                    VisitFieldDecl(field);

            if (Options.VisitClassProperties)
                foreach (var property in @class.Properties)
                    VisitProperty(property);

            if (Options.VisitClassMethods)
            {
                var methods = @class.Methods.ToArray();
                foreach (var method in methods)
                    VisitMethodDecl(method);
            }

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

            if (Options.VisitFunctionReturnType)
                return property.Type.Visit(this);

            return true;
        }

        public bool VisitFriend(Friend friend)
        {
            if (!VisitDeclaration(friend))
                return false;

            return friend.Declaration.Visit(this);
        }

        public virtual bool VisitFunctionDecl(Function function)
        {
            if (!VisitDeclaration(function))
                return false;

            var retType = function.ReturnType;
            if (Options.VisitFunctionReturnType && retType.Type != null)
                retType.Type.Visit(this, retType.Qualifiers);

            if (Options.VisitFunctionParameters)
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

        public virtual bool VisitVariableDecl(Variable variable)
        {
            if (!VisitDeclaration(variable))
                return false;

            return variable.Type.Visit(this, variable.QualifiedType.Qualifiers);
        }

        public virtual bool VisitEnumItem(Enumeration.Item item)
        {
            return true;
        }

        public virtual bool VisitClassTemplateDecl(ClassTemplate template)
        {
            if (!VisitDeclaration(template))
                return false;

            return true;
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
            return VisitDeclarationContext(@namespace);
        }

        public virtual bool VisitDeclarationContext(DeclarationContext context)
        {
            if (!VisitDeclaration(context))
                return false;

            foreach (var decl in context.Classes)
                decl.Visit(this);

            foreach (var decl in context.Functions)
                decl.Visit(this);

            if (Options.VisitNamespaceEnums)
                foreach (var decl in context.Enums)
                  decl.Visit(this);

            if (Options.VisitNamespaceTemplates)
                foreach (var decl in context.Templates)
                    decl.Visit(this);

            if (Options.VisitNamespaceTypedefs)
                foreach (var decl in context.Typedefs)
                    decl.Visit(this);

            if (Options.VisitNamespaceVariables)
                foreach (var decl in context.Variables)
                    decl.Visit(this);

            if (Options.VisitNamespaceEvents)
                foreach (var decl in context.Events)
                    decl.Visit(this);

            foreach (var decl in context.Namespaces)
                decl.Visit(this);

            return true;
        }

        public virtual bool VisitEvent(Event @event)
        {
            if (!VisitDeclaration(@event))
                return false;

            foreach (var param in @event.Parameters)
                param.Visit(this);

            return true;
        }

        #endregion
    }
}
