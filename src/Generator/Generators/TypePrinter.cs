using CppSharp.AST;
using CppSharp.Types;
using System;
using System.Collections.Generic;

namespace CppSharp.Generators
{
    public class TypePrinterResult
    {
        public string Type;
        public TypeMap TypeMap;
        public string NameSuffix;

        public static implicit operator TypePrinterResult(string type)
        {
            return new TypePrinterResult { Type = type };
        }

        public override string ToString() => Type;
    }

    public class TypePrinter : ITypePrinter<TypePrinterResult>,
        IDeclVisitor<TypePrinterResult>
    {
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
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitCILType(CILType type, TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitClassDecl(Class @class)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitClassTemplateDecl(ClassTemplate template)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitClassTemplateSpecializationDecl(
            ClassTemplateSpecialization specialization)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitDecayedType(DecayedType decayed,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitDeclaration(Declaration decl,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitEnumItemDecl(Enumeration.Item item)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitParameterDecl(Parameter parameter)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames = true)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTypeAliasTemplateDecl(
            TypeAliasTemplate typeAliasTemplate)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitTypedefDecl(TypedefDecl typedef)
        {
            throw new NotImplementedException();
        }

        public TypePrinterResult VisitTypedefNameDecl(TypedefNameDecl typedef)
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

        public virtual TypePrinterResult VisitUnsupportedType(UnsupportedType type,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        public virtual TypePrinterResult VisitVariableDecl(Variable variable)
        {
            throw new NotImplementedException();
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

        public virtual TypePrinterResult VisitVectorType(VectorType vectorType,
            TypeQualifiers quals)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}