using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    public interface IAstVisited
    {
        ISet<object> Visited { get; }

        bool AlreadyVisited(Declaration decl);
        bool AlreadyVisited(Type type);
    }

    public interface IAstVisitor<out T> : ITypeVisitor<T>, IDeclVisitor<T>, IStmtVisitor<T>
    {
        AstVisitorOptions VisitOptions { get; }
    }

    [Flags]
    public enum VisitFlags
    {
        /// <summary>
        /// We always visit declaration contexts.
        /// </summary>
        Default = 0,

        ClassBases = 1 << 0,
        ClassFields = 1 << 1,
        ClassProperties = 1 << 2,
        ClassMethods = 1 << 3,
        ClassTemplateSpecializations = 1 << 4,
        PropertyAccessors = 1 << 5,

        NamespaceEnums = 1 << 6,
        NamespaceTemplates = 1 << 7,
        NamespaceTypedefs = 1 << 8,
        NamespaceEvents = 1 << 9,
        NamespaceVariables = 1 << 10,
        NamespaceClasses = 1 << 15,
        NamespaceFunctions = 1 << 16,

        FunctionReturnType = 1 << 11,
        FunctionParameters = 1 << 12,
        EventParameters = 1 << 13,
        TemplateArguments = 1 << 14,

        Any = ClassBases | ClassFields | ClassProperties | ClassMethods |
            ClassTemplateSpecializations | PropertyAccessors |
            NamespaceEnums | NamespaceTemplates | NamespaceTypedefs |
            NamespaceEvents | NamespaceVariables | NamespaceClasses | NamespaceFunctions |
            FunctionReturnType | FunctionParameters |
            EventParameters | TemplateArguments
    }

    public class AstVisitorOptions
    {
        public AstVisitorOptions(VisitFlags flags)
            => this.flags = flags;

        public bool VisitClassBases => (flags & VisitFlags.ClassBases) != 0;
        public bool VisitClassFields => (flags & VisitFlags.ClassFields) != 0;
        public bool VisitClassProperties => (flags & VisitFlags.ClassProperties) != 0;
        public bool VisitClassMethods => (flags & VisitFlags.ClassMethods) != 0;
        public bool VisitClassTemplateSpecializations => (flags & VisitFlags.ClassTemplateSpecializations) != 0;
        public bool VisitPropertyAccessors => (flags & VisitFlags.PropertyAccessors) != 0;

        public bool VisitNamespaceClasses => (flags & VisitFlags.NamespaceClasses) != 0;
        public bool VisitNamespaceEnums => (flags & VisitFlags.NamespaceEnums) != 0;
        public bool VisitNamespaceFunctions => (flags & VisitFlags.NamespaceFunctions) != 0;
        public bool VisitNamespaceTemplates => (flags & VisitFlags.NamespaceTemplates) != 0;
        public bool VisitNamespaceTypedefs => (flags & VisitFlags.NamespaceTypedefs) != 0;
        public bool VisitNamespaceEvents => (flags & VisitFlags.NamespaceEvents) != 0;
        public bool VisitNamespaceVariables => (flags & VisitFlags.NamespaceVariables) != 0;

        public bool VisitFunctionReturnType => (flags & VisitFlags.FunctionReturnType) != 0;
        public bool VisitFunctionParameters => (flags & VisitFlags.FunctionParameters) != 0;
        public bool VisitEventParameters => (flags & VisitFlags.EventParameters) != 0;
        public bool VisitTemplateArguments => (flags & VisitFlags.TemplateArguments) != 0;

        public void SetFlags(VisitFlags flags) => this.flags |= flags;
        public void ResetFlags(VisitFlags flags) => this.flags = flags;
        public void ClearFlags(VisitFlags flags) => this.flags &= ~flags;

        private VisitFlags flags;
    }

    /// <summary>
    /// Base class for AST visitors.
    /// You can override the methods to customize the behaviour, by default
    /// this will visit all the nodes in a default way that should be useful
    /// for a lot of applications.
    /// </summary>
    public abstract partial class AstVisitor : IAstVisitor<bool>, IAstVisited
    {
        public ISet<object> Visited { get; private set; } = new HashSet<object>();
        public AstVisitorOptions VisitOptions { get; private set; }
            = new AstVisitorOptions(VisitFlags.Any & ~VisitFlags.PropertyAccessors);

        public bool AlreadyVisited(Type type)
        {
            return !Visited.Add(type);
        }

        public bool AlreadyVisited(Declaration decl)
        {
            return !Visited.Add(decl);
        }

        public bool AlreadyVisited(Stmt stmt)
        {
            return !Visited.Add(stmt);
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

            if (VisitOptions.VisitFunctionParameters)
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

            return pointer.QualifiedPointee.Visit(this);
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

            if (VisitOptions.VisitTemplateArguments)
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

            if (template.IsDependent && template.Template != null)
            {
                var @class = template.Template.TemplatedDecl as Class;
                if (@class != null)
                    return @class.Specializations.Any(s => s.Visit(this));
                return template.Template.Visit(this);
            }

            var specialization = template.GetClassTemplateSpecialization();
            return specialization != null && specialization.Visit(this);
        }

        public bool VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals)
        {
            if (!VisitType(template, quals))
                return false;

            if (VisitOptions.VisitTemplateArguments)
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

            if (template.Desugared.Type != null)
                return template.Desugared.Visit(this);

            return false;
        }

        public virtual bool VisitTemplateParameterType(TemplateParameterType param,
            TypeQualifiers quals)
        {
            return VisitType(param, quals);
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

        public virtual bool VisitPackExpansionType(PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            return true;
        }

        public virtual bool VisitUnaryTransformType(UnaryTransformType unaryTransformType, TypeQualifiers quals)
        {
            return true;
        }

        public virtual bool VisitUnresolvedUsingType(UnresolvedUsingType unresolvedUsingType, TypeQualifiers quals)
        {
            return true;
        }

        public virtual bool VisitVectorType(VectorType vectorType, TypeQualifiers quals)
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

        public virtual bool VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            if (!VisitType(type, quals))
                return false;

            return true;
        }

        public virtual bool VisitQualifiedType(QualifiedType type)
        {
            return type.Type.Visit(this, type.Qualifiers);
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

        public virtual bool VisitTranslationUnit(TranslationUnit unit)
        {
            return VisitNamespace(unit);
        }

        public virtual bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclarationContext(@class))
                return false;

            if (VisitOptions.VisitClassBases)
                foreach (var baseClass in @class.Bases)
                    if (baseClass.IsClass)
                        baseClass.Class.Visit(this);

            if (VisitOptions.VisitClassFields)
            {
                foreach (var field in @class.Fields)
                    VisitFieldDecl(field);

                var fields = @class.Declarations.OfType<Field>().ToArray();
                foreach (var field in fields)
                    VisitFieldDecl(field);
            }

            if (VisitOptions.VisitClassProperties)
            {
                foreach (var property in @class.Properties)
                    VisitProperty(property);

                var properties = @class.Declarations.OfType<Property>().ToArray();
                foreach (var property in properties)
                    VisitProperty(property);
            }

            if (VisitOptions.VisitClassMethods)
            {
                var methods = @class.Methods.ToArray();
                foreach (var method in methods)
                    VisitMethodDecl(method);

                methods = @class.Declarations.OfType<Method>().ToArray();
                foreach (var method in methods)
                    VisitMethodDecl(method);
            }

            if (@class.IsDependent && VisitOptions.VisitClassTemplateSpecializations)
                foreach (var specialization in @class.Specializations)
                    VisitClassTemplateSpecializationDecl(specialization);

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

            if (VisitOptions.VisitFunctionReturnType)
                property.Type.Visit(this);

            if (VisitOptions.VisitPropertyAccessors)
            {
                if (property.GetMethod != null)
                    property.GetMethod.Visit(this);

                if (property.SetMethod != null)
                    property.SetMethod.Visit(this);
            }

            return true;
        }

        public virtual bool VisitFriend(Friend friend)
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
            if (VisitOptions.VisitFunctionReturnType && retType.Type != null)
                retType.Type.Visit(this, retType.Qualifiers);

            if (VisitOptions.VisitFunctionParameters)
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

        public virtual bool VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            if (!VisitDeclaration(typedef))
                return false;

            return typedef.Type.Visit(this, typedef.QualifiedType.Qualifiers);
        }

        public virtual bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return VisitTypedefNameDecl(typedef);
        }

        public virtual bool VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return VisitTypedefNameDecl(typeAlias);
        }

        public virtual bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            foreach (var item in @enum.Items)
                VisitEnumItemDecl(item);

            return true;
        }

        public virtual bool VisitVariableDecl(Variable variable)
        {
            if (!VisitDeclaration(variable))
                return false;

            // TODO: Remove this null check once CppParser can properly handle auto types.
            // This is workaround for https://github.com/mono/CppSharp/issues/1412.            
            if (variable.Type == null)
                return false;

            return variable.Type.Visit(this, variable.QualifiedType.Qualifiers);
        }

        public virtual bool VisitEnumItemDecl(Enumeration.Item item)
        {
            if (!VisitDeclaration(item))
                return false;

            return true;
        }

        public virtual bool VisitTypeAliasTemplateDecl(TypeAliasTemplate template)
        {
            if (!VisitDeclaration(template))
                return false;

            foreach (var templateParameter in template.Parameters)
                templateParameter.Visit(this);

            return true;
        }

        public virtual bool VisitClassTemplateDecl(ClassTemplate template)
        {
            if (!VisitDeclaration(template))
                return false;

            foreach (var templateParameter in template.Parameters)
                templateParameter.Visit(this);

            if (VisitOptions.VisitClassTemplateSpecializations)
                foreach (var specialization in template.Specializations)
                    specialization.Visit(this);

            template.TemplatedClass.Visit(this);

            return true;
        }

        public virtual bool VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization)
        {
            return VisitClassDecl(specialization);
        }

        public virtual bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            if (!VisitDeclaration(template))
                return false;

            foreach (var templateParameter in template.Parameters)
                templateParameter.Visit(this);

            foreach (var specialization in template.Specializations)
                specialization.Visit(this);

            template.TemplatedFunction.Visit(this);

            return true;
        }

        public virtual bool VisitFunctionTemplateSpecializationDecl(FunctionTemplateSpecialization specialization)
        {
            return specialization.SpecializedFunction.Visit(this);
        }

        public virtual bool VisitVarTemplateDecl(VarTemplate template)
        {
            if (!VisitDeclaration(template))
                return false;

            foreach (var templateParameter in template.Parameters)
                templateParameter.Visit(this);

            foreach (var specialization in template.Specializations)
                specialization.Visit(this);

            template.TemplatedVariable.Visit(this);

            return true;
        }

        public virtual bool VisitVarTemplateSpecializationDecl(VarTemplateSpecialization specialization)
        {
            return VisitVariableDecl(specialization);
        }

        public virtual bool VisitMacroDefinition(MacroDefinition macro)
        {
            return false;
        }

        public virtual bool VisitNamespace(Namespace @namespace)
        {
            return VisitDeclarationContext(@namespace);
        }

        public virtual bool VisitDeclarationContext(DeclarationContext context)
        {
            if (!VisitDeclaration(context))
                return false;

            foreach (var decl in context.Classes.Where(c => !(c is ClassTemplateSpecialization)))
                decl.Visit(this);

            foreach (var decl in context.Functions)
                decl.Visit(this);

            if (VisitOptions.VisitNamespaceEnums)
                foreach (var decl in context.Enums)
                    decl.Visit(this);

            if (VisitOptions.VisitNamespaceTemplates)
                foreach (var decl in context.Templates)
                    decl.Visit(this);

            if (VisitOptions.VisitNamespaceTypedefs)
                foreach (var decl in context.Typedefs)
                    decl.Visit(this);

            if (VisitOptions.VisitNamespaceVariables)
                foreach (var decl in context.Variables)
                    decl.Visit(this);

            if (VisitOptions.VisitNamespaceEvents)
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

            if (VisitOptions.VisitEventParameters)
                foreach (var param in @event.Parameters)
                    param.Visit(this);

            return true;
        }

        public virtual bool VisitTemplateTemplateParameterDecl(TemplateTemplateParameter templateTemplateParameter)
        {
            if (!VisitDeclaration(templateTemplateParameter))
                return false;

            foreach (var templateParameter in templateTemplateParameter.Parameters)
                templateParameter.Visit(this);

            return true;
        }

        public virtual bool VisitTemplateParameterDecl(TypeTemplateParameter templateParameter)
        {
            if (!VisitDeclaration(templateParameter))
                return false;

            return true;
        }

        public virtual bool VisitNonTypeTemplateParameterDecl(NonTypeTemplateParameter nonTypeTemplateParameter)
        {
            if (!VisitDeclaration(nonTypeTemplateParameter))
                return false;

            return true;
        }

        public virtual bool VisitUnresolvedUsingDecl(UnresolvedUsingTypename unresolvedUsingTypename)
        {
            if (!VisitDeclaration(unresolvedUsingTypename))
                return false;

            return true;
        }

        #endregion

        public virtual bool VisitStmt(Stmt stmt)
        {
            return !AlreadyVisited(stmt);
        }
    }
}
