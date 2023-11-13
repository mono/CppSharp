using CppSharp.AST;
using CppSharp.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace CppSharp.Generators
{
    public class TypePrinterResult
    {
        public string Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public StringBuilder NamePrefix { get; set; } = new StringBuilder();
        public StringBuilder NameSuffix { get; set; } = new StringBuilder();
        public TypeMap TypeMap { get; set; }
        public GeneratorKind Kind { get; set; }

        public TypePrinterResult(string type = "", string nameSuffix = "")
        {
            Type = type;
            NameSuffix.Append(nameSuffix);
        }

        public void RemoveNamespace()
        {
            var index = Type.LastIndexOf('.');
            if (index != -1)
                Type = Type.Substring(index + 1);
        }

        public static implicit operator TypePrinterResult(string type) =>
            new TypePrinterResult { Type = type };

        public static implicit operator string(TypePrinterResult result) =>
           result.ToString();

        public override string ToString()
        {
            if (Kind == GeneratorKind.TypeScript)
                return $"{Name}{NameSuffix}: {Type}";

            var hasPlaceholder = Type.Contains("{0}");
            if (hasPlaceholder)
                return string.Format(Type, $"{NamePrefix}{Name}{NameSuffix}");

            var namePrefix = Name.Length > 0 && (NamePrefix.Length > 0 || Type.Length > 0) ?
                $"{NamePrefix} " : NamePrefix.ToString();
            return $"{Type}{namePrefix}{Name}{NameSuffix}";
        }
    }

    public class TypePrinter : ITypePrinter<TypePrinterResult>,
        IDeclVisitor<TypePrinterResult>
    {
        private readonly Stack<TypePrinterContextKind> contexts;
        private readonly Stack<MarshalKind> marshalKinds;
        private readonly Stack<TypePrintScopeKind> scopeKinds;

        public BindingContext Context { get; set; }
        public TypePrinterContextKind ContextKind => contexts.Peek();

        public MarshalKind MarshalKind => marshalKinds.Peek();

        public TypePrintScopeKind ScopeKind => scopeKinds.Peek();
        public bool IsGlobalQualifiedScope => ScopeKind == TypePrintScopeKind.GlobalQualified;

        public TypePrinter(BindingContext context, TypePrinterContextKind contextKind = TypePrinterContextKind.Managed)
        {
            Context = context;
            contexts = new Stack<TypePrinterContextKind>();
            marshalKinds = new Stack<MarshalKind>();
            scopeKinds = new Stack<TypePrintScopeKind>();
            PushContext(contextKind);
            PushMarshalKind(MarshalKind.Unknown);
            PushScope(TypePrintScopeKind.GlobalQualified);
        }

        public void PushContext(TypePrinterContextKind kind) => contexts.Push(kind);
        public TypePrinterContextKind PopContext() => contexts.Pop();

        public void PushMarshalKind(MarshalKind kind) => marshalKinds.Push(kind);
        public MarshalKind PopMarshalKind() => marshalKinds.Pop();

        public void PushScope(TypePrintScopeKind kind) => scopeKinds.Push(kind);
        public TypePrintScopeKind PopScope() => scopeKinds.Pop();

        public Parameter Parameter;

        #region Dummy implementations

        public virtual string ToString(CppSharp.AST.Type type)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitAttributedType(AttributedType attributed,
            TypeQualifiers quals)
        {
            return attributed.Modified.Visit(this);
        }

        public virtual TypePrinterResult VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type, quals);
        }

        public virtual TypePrinterResult VisitCILType(CILType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitClassDecl(Class @class)
        {
            return VisitDeclaration(@class);
        }

        public virtual TypePrinterResult VisitClassTemplateDecl(ClassTemplate template)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitClassTemplateSpecializationDecl(
            ClassTemplateSpecialization specialization)
        {
            return VisitClassDecl(specialization);
        }

        public virtual TypePrinterResult VisitDecayedType(DecayedType decayed,
            TypeQualifiers quals)
        {
            return decayed.Decayed.Visit(this);
        }

        public virtual TypePrinterResult VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitDeclaration(Declaration decl,
            TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public virtual TypePrinterResult VisitDelegate(FunctionType function)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitDependentNameType(
            DependentNameType dependent, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitEnumDecl(Enumeration @enum)
        {
            return VisitDeclaration(@enum);
        }

        public virtual TypePrinterResult VisitEnumItemDecl(Enumeration.Item item)
        {
            return VisitDeclaration(@item);
        }

        public virtual TypePrinterResult VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitFieldDecl(Field field)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitFriend(Friend friend)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitFunctionTemplateDecl(
            FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitFunctionTemplateSpecializationDecl(
            FunctionTemplateSpecialization specialization)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitFunctionType(FunctionType function,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitInjectedClassNameType(
            InjectedClassNameType injected, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitMemberPointerType(
            MemberPointerType member, TypeQualifiers quals)
        {
            return member.QualifiedPointee.Visit(this);
        }

        public virtual TypePrinterResult VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitNonTypeTemplateParameterDecl(
            NonTypeTemplateParameter nonTypeTemplateParameter)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitPackExpansionType(
            PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitParameter(Parameter param,
            bool hasName = true)
        {
            Parameter = param;
            var type = param.QualifiedType.Visit(this);
            var name = hasName ? $" {param.Name}" : string.Empty;
            Parameter = null;
            return $"{type}{name}";
        }

        public virtual TypePrinterResult VisitParameterDecl(Parameter parameter)
        {
            return VisitParameter(parameter, hasName: false);
        }

        public virtual TypePrinterResult VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames = true)
        {
            var args = new List<string>();

            foreach (var param in @params)
            {
                Parameter = param;
                args.Add(VisitParameter(param, hasNames).Type);
            }

            Parameter = null;
            return string.Join(", ", args);
        }

        public virtual TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitPrimitiveType(PrimitiveType type,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitProperty(Property property)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (tag.Declaration == null)
                return string.Empty;

            return tag.Declaration.Visit(this);
        }

        public virtual TypePrinterResult VisitTemplateParameterDecl(
            TypeTemplateParameter templateParameter)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTemplateParameterType(
            TemplateParameterType param, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTemplateTemplateParameterDecl(
            TemplateTemplateParameter templateTemplateParameter)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTranslationUnit(TranslationUnit unit)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return VisitDeclaration(typeAlias);
        }

        public virtual TypePrinterResult VisitTypeAliasTemplateDecl(
            TypeAliasTemplate typeAliasTemplate)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTypedefDecl(TypedefDecl typedef)
        {
            return VisitDeclaration(typedef);
        }

        public virtual TypePrinterResult VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitUnaryTransformType(
            UnaryTransformType unaryTransformType, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public TypePrinterResult VisitUnresolvedUsingType(UnresolvedUsingType unresolvedUsingType, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitUnsupportedType(UnsupportedType type,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitVariableDecl(Variable variable)
        {
            return VisitDeclaration(variable);
        }

        public virtual TypePrinterResult VisitVarTemplateDecl(VarTemplate template)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitVarTemplateSpecializationDecl(
            VarTemplateSpecialization template)
        {
            throw new NotImplementedException();
        }

        public TypePrinterResult VisitUnresolvedUsingDecl(UnresolvedUsingTypename unresolvedUsingTypename)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitVectorType(VectorType vectorType,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitQualifiedType(QualifiedType type)
        {
            return type.Type.Visit(this, type.Qualifiers);
        }

        #endregion
    }
}