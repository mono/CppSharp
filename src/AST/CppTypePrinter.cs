using System;
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

    public enum CppTypePrintScopeKind
    {
        Local,
        Qualified,
        GlobalQualified
    }

    public class CppTypePrinter : ITypePrinter<string>, IDeclVisitor<string>
    {
        public CppTypePrintFlavorKind PrintFlavorKind;
        public CppTypePrintScopeKind PrintScopeKind;
        public bool PrintLogicalNames;
        public bool PrintTypeQualifiers;

        public bool PrintTypeModifiers { get; set; }

        public CppTypePrinter(bool printTypeQualifiers = true, bool printTypeModifiers = true)
        {
            PrintFlavorKind = CppTypePrintFlavorKind.Cpp;
            PrintScopeKind = CppTypePrintScopeKind.GlobalQualified;
            PrintTypeQualifiers = printTypeQualifiers;
            PrintTypeModifiers = printTypeModifiers;
        }

        public bool ResolveTypedefs { get; set; }

        public string VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var qual = PrintTypeQualifiers && quals.IsConst ? "const " : string.Empty;
            return string.Format("{0}{1}", qual, tag.Declaration.Visit(this));
        }

        public string VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            var typeName = array.Type.Visit(this);

            switch (array.SizeType)
            {
            case ArrayType.ArraySize.Constant:
                return string.Format("{0}[{1}]", typeName, array.Size);
            case ArrayType.ArraySize.Variable:
            case ArrayType.ArraySize.Dependent:
            case ArrayType.ArraySize.Incomplete:
                return string.Format("{0}[]", typeName);
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

        public string VisitPointerType(PointerType pointer, TypeQualifiers quals)
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

            var qual = PrintTypeQualifiers && quals.IsConst ? "const " : string.Empty;
            var pointeeType = pointer.Pointee.Visit(this, quals);
            var mod = PrintTypeModifiers ? ConvertModifierToString(pointer.Modifier) : string.Empty;
            return string.Format("{0}{1}{2}", qual, pointeeType, mod);
        }

        public string VisitMemberPointerType(MemberPointerType member, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public string VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool: return "bool";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.Char16: return "char16_t";
                case PrimitiveType.Char32: return "char32_t";
                case PrimitiveType.WideChar: return "wchar_t";
                case PrimitiveType.Char: return "char";
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
                case PrimitiveType.Null: return "std::nullptr_t";
            }

            throw new NotSupportedException();
        }

        public string VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            if (ResolveTypedefs)
            {
                var decl = typedef.Declaration;
                FunctionType func;
                return decl.Type.IsPointerTo(out func) ? VisitDeclaration(decl) : decl.Type.Visit(this);
            }
            return GetDeclName(typedef.Declaration);
        }

        public string VisitAttributedType(AttributedType attributed, TypeQualifiers quals)
        {
            return attributed.Modified.Visit(this);
        }

        public string VisitDecayedType(DecayedType decayed, TypeQualifiers quals)
        {
            return decayed.Decayed.Visit(this);
        }

        public string VisitTemplateSpecializationType(TemplateSpecializationType template, TypeQualifiers quals)
        {
            var specialization = template.GetClassTemplateSpecialization();
            if (specialization == null)
                return string.Empty;

            return VisitClassTemplateSpecializationDecl(specialization);
        }

        public string VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals)
        {
            if (template.Desugared.Type != null)
                return template.Desugared.Visit(this);
            return string.Empty;
        }

        public string VisitTemplateParameterType(TemplateParameterType param, TypeQualifiers quals)
        {
            if (param.Parameter.Name == null)
                return string.Empty;

            return param.Parameter.Name;
        }

        public string VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            return param.Replacement.Visit(this);
        }

        public string VisitInjectedClassNameType(InjectedClassNameType injected, TypeQualifiers quals)
        {
            return injected.Class.Visit(this);
        }

        public string VisitDependentNameType(DependentNameType dependent, TypeQualifiers quals)
        {
            return dependent.Desugared.Type != null ? dependent.Desugared.Visit(this) : string.Empty;
        }

        public string VisitPackExpansionType(PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitUnaryTransformType(UnaryTransformType unaryTransformType, TypeQualifiers quals)
        {
            if (unaryTransformType.Desugared.Type != null)
                return unaryTransformType.Desugared.Visit(this);
            return unaryTransformType.BaseType.Visit(this);
        }

        public string VisitVectorType(VectorType vectorType, TypeQualifiers quals)
        {
            // an incomplete implementation but we'd hardly need anything better
            return "__attribute__()";
        }

        public string VisitCILType(CILType type, TypeQualifiers quals)
        {
            if (type.Type == typeof(string))
                return quals.IsConst ? "const char*" : "char*";
 
            throw new NotImplementedException(string.Format("Unhandled .NET type: {0}", type.Type));
        }

        public string VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public string VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals)
        {
            throw new System.NotImplementedException();
        }

        public string VisitDeclaration(Declaration decl, TypeQualifiers quals)
        {
            throw new System.NotImplementedException();
        }

        public string VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false);

            return string.Format("{0} ({1})", returnType.Visit(this), args);
        }

        public string VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames = true)
        {
            var args = new List<string>();

            foreach (var param in @params)
                args.Add(VisitParameter(param, hasNames));

            if (PrintFlavorKind == CppTypePrintFlavorKind.ObjC)
                return string.Join(" ", args);

            return string.Join(", ", args);
        }

        public string VisitParameter(Parameter arg, bool hasName = true)
        {
            var type = arg.Type.Visit(this, arg.QualifiedType.Qualifiers);
            var name = arg.Name;
            var printName = hasName && !string.IsNullOrEmpty(name);

            if (PrintFlavorKind == CppTypePrintFlavorKind.ObjC)
                return printName ? string.Format(":({0}){1}", type, name)
                    : string.Format(":({0})", type);

            return printName ? string.Format("{0} {1}", type, name) : type;
        }

        public string VisitDelegate(FunctionType function)
        {
            throw new System.NotImplementedException();
        }

        public string GetDeclName(Declaration declaration)
        {
            switch (PrintScopeKind)
            {
            case CppTypePrintScopeKind.Local:
                return PrintLogicalNames ? declaration.LogicalOriginalName
                    : declaration.OriginalName;
            case CppTypePrintScopeKind.Qualified:
                return PrintLogicalNames ? declaration.QualifiedLogicalOriginalName
                    : declaration.QualifiedOriginalName;
            case CppTypePrintScopeKind.GlobalQualified:
                return "::" + (PrintLogicalNames ? declaration.QualifiedLogicalOriginalName
                    : declaration.QualifiedOriginalName);
            }

            throw new NotSupportedException();
        }

        public string VisitDeclaration(Declaration decl)
        {
            return GetDeclName(decl);
        }

        public string VisitClassDecl(Class @class)
        {
            return VisitDeclaration(@class);
        }

        public string VisitClassTemplateSpecializationDecl(ClassTemplateSpecialization specialization)
        {
            return string.Format("{0}<{1}>", specialization.TemplatedDecl.Visit(this),
                string.Join(", ",
                specialization.Arguments.Where(
                    a => a.Type.Type != null &&
                        !(a.Type.Type is DependentNameType)).Select(a => a.Type.Visit(this))));
        }

        public string VisitFieldDecl(Field field)
        {
            return VisitDeclaration(field);
        }

        public string VisitFunctionDecl(Function function)
        {
            return VisitDeclaration(function);
        }

        public string VisitMethodDecl(Method method)
        {
            return VisitDeclaration(method);
        }

        public string VisitParameterDecl(Parameter parameter)
        {
            return VisitParameter(parameter, hasName: false);
        }

        public string VisitTypedefDecl(TypedefDecl typedef)
        {
            return VisitDeclaration(typedef);
        }

        public string VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return VisitDeclaration(typeAlias);
        }

        public string VisitEnumDecl(Enumeration @enum)
        {
            return VisitDeclaration(@enum);
        }

        public string VisitEnumItemDecl(Enumeration.Item item)
        {
            return VisitDeclaration(item);
        }

        public string VisitVariableDecl(Variable variable)
        {
            return VisitDeclaration(variable);
        }

        public string VisitClassTemplateDecl(ClassTemplate template)
        {
            return VisitDeclaration(template);
        }

        public string VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return VisitDeclaration(template);
        }

        public string VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public string VisitNamespace(Namespace @namespace)
        {
            return VisitDeclaration(@namespace);
        }

        public string VisitEvent(Event @event)
        {
            return string.Empty;
        }

        public string VisitProperty(Property property)
        {
            return VisitDeclaration(property);
        }

        public string VisitFriend(Friend friend)
        {
            throw new NotImplementedException();
        }

        public string ToString(Type type)
        {
            return type.Visit(this);
        }

        public string VisitTemplateTemplateParameterDecl(TemplateTemplateParameter templateTemplateParameter)
        {
            return templateTemplateParameter.Name;
        }

        public string VisitTemplateParameterDecl(TypeTemplateParameter templateParameter)
        {
            if (templateParameter.DefaultArgument.Type == null)
                return templateParameter.Name;

            return string.Format("{0} = {1}", templateParameter.Name,
                templateParameter.DefaultArgument.Visit(this));
        }

        public string VisitNonTypeTemplateParameterDecl(NonTypeTemplateParameter nonTypeTemplateParameter)
        {
            if (nonTypeTemplateParameter.DefaultArgument == null)
                return nonTypeTemplateParameter.Name;

            return string.Format("{0} = {1}",  nonTypeTemplateParameter.Name,
                nonTypeTemplateParameter.DefaultArgument.String);
        }

        public string VisitTypeAliasTemplateDecl(TypeAliasTemplate typeAliasTemplate)
        {
            throw new NotImplementedException();
        }

        public string VisitFunctionTemplateSpecializationDecl(FunctionTemplateSpecialization specialization)
        {
            throw new NotImplementedException();
        }

        public string VisitVarTemplateDecl(VarTemplate template)
        {
            return VisitDeclaration(template);
        }

        public string VisitVarTemplateSpecializationDecl(VarTemplateSpecialization template)
        {
            throw new NotImplementedException();
        }
    }
}
