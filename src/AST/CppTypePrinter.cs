﻿using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    public enum CppTypePrintFlavorKind
    {
        C,
        Cpp,
        ObjC
    } 

    public enum TypePrintScopeKind
    {
        Local,
        Qualified,
        GlobalQualified
    }

    public class CppTypePrinter : ITypePrinter<string>, IDeclVisitor<string>
    {
        public CppTypePrintFlavorKind PrintFlavorKind { get; set; }
        public TypePrintScopeKind PrintScopeKind { get; set; }
        public bool PrintLogicalNames { get; set; }
        public bool PrintTypeQualifiers { get; set; }
        public bool PrintTypeModifiers { get; set; }
        public bool PrintVariableArrayAsPointers { get; set; }

        public CppTypePrinter()
        {
            PrintFlavorKind = CppTypePrintFlavorKind.Cpp;
            PrintScopeKind = TypePrintScopeKind.GlobalQualified;
            PrintTypeQualifiers = true;
            PrintTypeModifiers = true;
        }

        public bool ResolveTypedefs { get; set; }

        public virtual string VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var qual = GetStringQuals(quals);
            return $@"{qual}{tag.Declaration.Visit(this)}";
        }

        public virtual string VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            var typeName = array.Type.Visit(this);

            switch (array.SizeType)
            {
            case ArrayType.ArraySize.Constant:
                return string.Format("{0}[{1}]", typeName, array.Size);
            case ArrayType.ArraySize.Variable:
            case ArrayType.ArraySize.Dependent:
            case ArrayType.ArraySize.Incomplete:
                return string.Format("{0}{1}", typeName,
                                     PrintVariableArrayAsPointers ? "*" : "[]");
            }

            throw new NotSupportedException();
        }

        static string ConvertModifierToString(PointerType.TypeModifier modifier)
        {
            switch (modifier)
            {
                case PointerType.TypeModifier.Value: return string.Empty;
                case PointerType.TypeModifier.Pointer: return "*";
                case PointerType.TypeModifier.LVReference: return "&";
                case PointerType.TypeModifier.RVReference: return "&&";
            }

            return string.Empty;
        }

        public virtual string VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            var function = pointee as FunctionType;
            if (function != null)
            {
                var arguments = function.Parameters;
                var returnType = function.ReturnType;
                var args = string.Empty;

                if (arguments.Count > 0)
                    args = VisitParameters(function.Parameters, hasNames: false);

                return string.Format("{0} (*)({1})", returnType.Visit(this), args);
            }

            var qual = GetStringQuals(quals, false);
            var pointeeType = pointee.Visit(this, pointer.QualifiedPointee.Qualifiers);
            var mod = PrintTypeModifiers ? ConvertModifierToString(pointer.Modifier) : string.Empty;
            return $@"{pointeeType}{mod}{(string.IsNullOrEmpty(qual) ? string.Empty : " ")}{qual}";
        }

        public virtual string VisitMemberPointerType(MemberPointerType member, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public virtual string VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            var qual = GetStringQuals(quals);
            return $@"{qual}{VisitPrimitiveType(builtin.Type)}";
        }

        public virtual string VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool: return "bool";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.Char16: return "char16_t";
                case PrimitiveType.Char32: return "char32_t";
                case PrimitiveType.WideChar: return "wchar_t";
                case PrimitiveType.Char: return "char";
                case PrimitiveType.SChar: return "signed char";
                case PrimitiveType.UChar: return "unsigned char";
                case PrimitiveType.Short: return "short";
                case PrimitiveType.UShort: return "unsigned short";
                case PrimitiveType.Int: return "int";
                case PrimitiveType.UInt: return "unsigned int";
                case PrimitiveType.Long: return "long";
                case PrimitiveType.ULong: return "unsigned long";
                case PrimitiveType.LongLong: return "long long";
                case PrimitiveType.ULongLong: return "unsigned long long";
                case PrimitiveType.Int128: return "__int128_t";
                case PrimitiveType.UInt128: return "__uint128_t";
                case PrimitiveType.Half: return "__fp16";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.LongDouble: return "long double";
                case PrimitiveType.Float128: return "__float128";
                case PrimitiveType.IntPtr: return "void*";
                case PrimitiveType.UIntPtr: return "uintptr_t";
                case PrimitiveType.Null: return PrintFlavorKind == CppTypePrintFlavorKind.Cpp ? "std::nullptr_t" : "NULL";
                case PrimitiveType.String:
                {
                    switch (PrintFlavorKind)
                    {
                    case CppTypePrintFlavorKind.C:
                        return "const char*";
                    case CppTypePrintFlavorKind.Cpp:
                        return "std::string";
                    case CppTypePrintFlavorKind.ObjC:
                        return "NSString";;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }

            throw new NotSupportedException();
        }

        public virtual string VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            FunctionType func;
            if (ResolveTypedefs && !typedef.Declaration.Type.IsPointerTo(out func))
                return typedef.Declaration.Type.Visit(this);
            var qual = GetStringQuals(quals);
            return $@"{qual}{typedef.Declaration.Visit(this)}";
        }

        public virtual string VisitAttributedType(AttributedType attributed, TypeQualifiers quals)
        {
            return attributed.Modified.Visit(this);
        }

        public virtual string VisitDecayedType(DecayedType decayed, TypeQualifiers quals)
        {
            return decayed.Decayed.Visit(this);
        }

        public virtual string VisitTemplateSpecializationType(TemplateSpecializationType template, TypeQualifiers quals)
        {
            var specialization = template.GetClassTemplateSpecialization();
            if (specialization == null)
                return string.Empty;

            var qual = GetStringQuals(quals);
            return $@"{qual}{VisitClassTemplateSpecializationDecl(specialization)}";
        }

        public virtual string VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals)
        {
            if (template.Desugared.Type != null)
                return template.Desugared.Visit(this);
            return string.Empty;
        }

        public virtual string VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            if (param.Parameter == null || param.Parameter.Name == null)
                return string.Empty;

            return param.Parameter.Name;
        }

        public virtual string VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            return param.Replacement.Type.Visit(this, quals);
        }

        public virtual string VisitInjectedClassNameType(InjectedClassNameType injected, TypeQualifiers quals)
        {
            return injected.Class.Visit(this);
        }

        public virtual string VisitDependentNameType(DependentNameType dependent, TypeQualifiers quals)
        {
            return dependent.Qualifier.Type != null ? dependent.Qualifier.Visit(this) : string.Empty;
        }

        public virtual string VisitPackExpansionType(PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public virtual string VisitUnaryTransformType(UnaryTransformType unaryTransformType, TypeQualifiers quals)
        {
            if (unaryTransformType.Desugared.Type != null)
                return unaryTransformType.Desugared.Visit(this);
            return unaryTransformType.BaseType.Visit(this);
        }

        public virtual string VisitVectorType(VectorType vectorType, TypeQualifiers quals)
        {
            // an incomplete implementation but we'd hardly need anything better
            return "__attribute__()";
        }

        public virtual string VisitCILType(CILType type, TypeQualifiers quals)
        {
            if (type.Type == typeof(string))
                return quals.IsConst ? "const char*" : "char*";

            switch (System.Type.GetTypeCode(type.Type))
            {
                case TypeCode.Boolean:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.Bool), quals);
                case TypeCode.Char:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.Char), quals);
                case TypeCode.Int16:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.Short), quals);
                case TypeCode.UInt16:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.UShort), quals);
                case TypeCode.Int32:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.Int), quals);
                case TypeCode.UInt32:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.UInt), quals);
                case TypeCode.Int64:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.Long), quals);
                case TypeCode.UInt64:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.ULong), quals);
                case TypeCode.Single:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.Float), quals);
                case TypeCode.Double:
                    return VisitBuiltinType(new BuiltinType(PrimitiveType.Double), quals);
                case TypeCode.String:
                    return quals.IsConst ? "const char*" : "char*";
            }

            return "void*";
        }

        public virtual string VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public virtual string VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new System.NotImplementedException();
        }

        public virtual string VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new System.NotImplementedException();
        }

        public virtual string VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false);

            return string.Format("{0} ({1})", returnType.Visit(this), args);
        }

        public virtual string VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames = true)
        {
            var args = new List<string>();

            foreach (var param in @params)
                args.Add(VisitParameter(param, hasNames));

            if (PrintFlavorKind == CppTypePrintFlavorKind.ObjC)
                return string.Join(" ", args);

            return string.Join(", ", args);
        }

        public virtual string VisitParameter(Parameter arg, bool hasName = true)
        {
            var type = arg.Type.Visit(this, arg.QualifiedType.Qualifiers);
            var name = arg.Name;
            var printName = hasName && !string.IsNullOrEmpty(name);

            if (PrintFlavorKind == CppTypePrintFlavorKind.ObjC)
                return printName ? string.Format(":({0}){1}", type, name)
                    : string.Format(":({0})", type);

            return printName ? string.Format("{0} {1}", type, name) : type;
        }

        public virtual string VisitDelegate(FunctionType function)
        {
            throw new System.NotImplementedException();
        }

        public virtual string GetDeclName(Declaration declaration, TypePrintScopeKind scope)
        {
            switch (scope)
            {
            case TypePrintScopeKind.Local:
                return PrintLogicalNames ? declaration.LogicalOriginalName
                    : declaration.OriginalName;
            case TypePrintScopeKind.Qualified:
                if (declaration.Namespace is Class)
                    return $"{declaration.Namespace.Visit(this)}::{declaration.OriginalName}";
                return PrintLogicalNames ? declaration.QualifiedLogicalOriginalName
                    : declaration.QualifiedOriginalName;
            case TypePrintScopeKind.GlobalQualified:
                if (declaration.Namespace is Class)
                    return $"{declaration.Namespace.Visit(this)}::{declaration.OriginalName}";
                var qualifier = PrintFlavorKind == CppTypePrintFlavorKind.Cpp ? "::" : string.Empty;
                return qualifier + GetDeclName(declaration, TypePrintScopeKind.Qualified);
            }

            throw new NotSupportedException();
        }

        public virtual string VisitDeclaration(Declaration decl)
        {
            return GetDeclName(decl, PrintScopeKind);
        }

        public string VisitTranslationUnit(TranslationUnit unit)
        {
            return VisitDeclaration(unit);
        }

        public virtual string VisitClassDecl(Class @class)
        {
            return VisitDeclaration(@class);
        }

        public virtual string VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization)
        {
            return string.Format("{0}<{1}>", specialization.TemplatedDecl.Visit(this),
                string.Join(", ",
                specialization.Arguments.Where(
                    a => a.Type.Type != null &&
                        !(a.Type.Type is DependentNameType)).Select(a => a.Type.Visit(this))));
        }

        public virtual string VisitFieldDecl(Field field)
        {
            return VisitDeclaration(field);
        }

        public virtual string VisitFunctionDecl(Function function)
        {
            return VisitDeclaration(function);
        }

        public virtual string VisitMethodDecl(Method method)
        {
            // HACK: this should never happen but there's an inexplicable crash with the 32-bit Windows CI - I have no time to fix it right now
            var functionType = method.FunctionType.Type.Desugar() as FunctionType;
            if (functionType == null)
                return string.Empty;
            var returnType = method.IsConstructor || method.IsDestructor ||
                method.OperatorKind == CXXOperatorKind.Conversion ||
                method.OperatorKind == CXXOperatorKind.ExplicitConversion ?
                string.Empty : $"{method.OriginalReturnType.Visit(this)} ";
            var @class = method.Namespace.Visit(this);
            var @params = string.Join(", ", method.Parameters.Select(p => p.Visit(this)));
            var @const = (method.IsConst ? " const" : string.Empty);
            var name = method.OperatorKind == CXXOperatorKind.Conversion ||
                method.OperatorKind == CXXOperatorKind.ExplicitConversion ?
                $"operator {method.OriginalReturnType.Visit(this)}" :
                method.OriginalName;
            var exceptionType =
                functionType.ExceptionSpecType == ExceptionSpecType.BasicNoexcept ?
                " noexcept" : string.Empty;
            return $@"{returnType}{@class}::{name}({@params}){@const}{exceptionType}";
        }

        public virtual string VisitParameterDecl(Parameter parameter)
        {
            return VisitParameter(parameter, hasName: false);
        }

        public virtual string VisitTypedefDecl(TypedefDecl typedef)
        {
            if (ResolveTypedefs)
                return typedef.Type.Visit(this);

            if (PrintFlavorKind != CppTypePrintFlavorKind.Cpp)
                return typedef.OriginalName;

            var originalNamespace = typedef.OriginalNamespace.Visit(this);
            return originalNamespace == "::" ? typedef.OriginalName :
                $"{originalNamespace}::{typedef.OriginalName}";
        }

        public virtual string VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return VisitDeclaration(typeAlias);
        }

        public virtual string VisitEnumDecl(Enumeration @enum)
        {
            return VisitDeclaration(@enum);
        }

        public virtual string VisitEnumItemDecl(Enumeration.Item item)
        {
            return VisitDeclaration(item);
        }

        public virtual string VisitVariableDecl(Variable variable)
        {
            return VisitDeclaration(variable);
        }

        public virtual string VisitClassTemplateDecl(ClassTemplate template)
        {
            return VisitDeclaration(template);
        }

        public virtual string VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return VisitDeclaration(template);
        }

        public virtual string VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public virtual string VisitNamespace(Namespace @namespace)
        {
            return VisitDeclaration(@namespace);
        }

        public virtual string VisitEvent(Event @event)
        {
            return string.Empty;
        }

        public virtual string VisitProperty(Property property)
        {
            return VisitDeclaration(property);
        }

        public virtual string VisitFriend(Friend friend)
        {
            throw new NotImplementedException();
        }

        public virtual string ToString(Type type)
        {
            return type.Visit(this);
        }

        public virtual string VisitTemplateTemplateParameterDecl(TemplateTemplateParameter templateTemplateParameter)
        {
            return templateTemplateParameter.Name;
        }

        public virtual string VisitTemplateParameterDecl(TypeTemplateParameter templateParameter)
        {
            if (templateParameter.DefaultArgument.Type == null)
                return templateParameter.Name;

            return string.Format("{0} = {1}", templateParameter.Name,
                templateParameter.DefaultArgument.Visit(this));
        }

        public virtual string VisitNonTypeTemplateParameterDecl(NonTypeTemplateParameter nonTypeTemplateParameter)
        {
            if (nonTypeTemplateParameter.DefaultArgument == null)
                return nonTypeTemplateParameter.Name;

            return string.Format("{0} = {1}",  nonTypeTemplateParameter.Name,
                nonTypeTemplateParameter.DefaultArgument.String);
        }

        public string VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            throw new NotImplementedException();
        }

        public virtual string VisitTypeAliasTemplateDecl(TypeAliasTemplate typeAliasTemplate)
        {
            throw new NotImplementedException();
        }

        public virtual string VisitFunctionTemplateSpecializationDecl(FunctionTemplateSpecialization specialization)
        {
            throw new NotImplementedException();
        }

        public virtual string VisitVarTemplateDecl(VarTemplate template)
        {
            return VisitDeclaration(template);
        }

        public virtual string VisitVarTemplateSpecializationDecl(VarTemplateSpecialization template)
        {
            throw new NotImplementedException();
        }

        private string GetStringQuals(TypeQualifiers quals, bool appendSpace = true)
        {
            var stringQuals = new List<string>();
            if (PrintTypeQualifiers)
            {
                if (quals.IsConst)
                    stringQuals.Add("const");
                if (quals.IsVolatile)
                    stringQuals.Add("volatile");
            }
            if (stringQuals.Count == 0)
                return string.Empty;
            return string.Join(" ", stringQuals) + (appendSpace ? " " : string.Empty);
        }
    }
}
