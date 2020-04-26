using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;

namespace CppSharp.Generators.C
{
    public enum CppTypePrintFlavorKind
    {
        C,
        Cpp,
        ObjC
    }

    public class CppTypePrinter : TypePrinter
    {
        public CppTypePrintFlavorKind PrintFlavorKind { get; set; }
        public bool PrintLogicalNames { get; set; }
        public bool PrintTypeQualifiers { get; set; }
        public bool PrintTypeModifiers { get; set; }
        public bool PrintVariableArrayAsPointers { get; set; }

        public CppTypePrinter(BindingContext context) : base(TypePrinterContextKind.Native)
        {
            Context = context;
            PrintFlavorKind = CppTypePrintFlavorKind.Cpp;
            ScopeKind = TypePrintScopeKind.GlobalQualified;
            PrintTypeQualifiers = true;
            PrintTypeModifiers = true;
        }

        public BindingContext Context { get; private set; }
        public TypeMapDatabase TypeMapDatabase => Context.TypeMaps;
        public DriverOptions Options => Context.Options;

        public bool ResolveTypeMaps { get; set; } = true;
        public bool ResolveTypedefs { get; set; }

        public bool FindTypeMap(CppSharp.AST.Type type, out TypePrinterResult result)
        {
            result = null;

            if (!ResolveTypeMaps)
                return false;

            TypeMap typeMap;
            if (!TypeMapDatabase.FindTypeMap(type, out typeMap) || typeMap.IsIgnored)
                return false;

            var typePrinterContext = new TypePrinterContext
            {
                Type = type,
                Kind = Kind,
                MarshalKind = MarshalKind
            };

            var typePrinter = new CppTypePrinter(Context)
            {
                PrintFlavorKind = PrintFlavorKind,
                ScopeKind = ScopeKind,
                PrintTypeQualifiers = PrintTypeQualifiers,
                PrintTypeModifiers = PrintTypeModifiers,
                ResolveTypeMaps = false
            };
            typePrinter.PushContext(ContextKind);

            var typeName = typeMap.CppSignatureType(typePrinterContext).Visit(typePrinter);
            result = new TypePrinterResult(typeName) { TypeMap = typeMap };

            return true;
        }

        public override TypePrinterResult VisitTagType(TagType tag,
            TypeQualifiers quals)
        {
            TypePrinterResult result;
            if (FindTypeMap(tag, out result))
                return result;

            var qual = GetStringQuals(quals);
            return $"{qual}{tag.Declaration.Visit(this)}";
        }

        public override TypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            var arraySuffix = string.Empty;

            switch (array.SizeType)
            {
            case ArrayType.ArraySize.Constant:
                arraySuffix = $"[{array.Size}]";
                break;
            case ArrayType.ArraySize.Variable:
            case ArrayType.ArraySize.Dependent:
            case ArrayType.ArraySize.Incomplete:
                arraySuffix = $"{(PrintVariableArrayAsPointers ? "*" : "[]")}";
                break;
            default:
                throw new NotImplementedException();
            }

            var result = new TypePrinterResult
            {
                Type = array.Type.Visit(this),
                NameSuffix = new System.Text.StringBuilder(arraySuffix)
            };

            return result;
        }

        private static string ConvertModifierToString(PointerType.TypeModifier modifier)
        {
            switch (modifier)
            {
                case PointerType.TypeModifier.Value: return "[]";
                case PointerType.TypeModifier.Pointer: return "*";
                case PointerType.TypeModifier.LVReference: return "&";
                case PointerType.TypeModifier.RVReference: return "&&";
            }

            return string.Empty;
        }

        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointeeType = pointer.Pointee.Visit(this, pointer.QualifiedPointee.Qualifiers);
            if (pointeeType.TypeMap != null)
                return pointeeType;

            var mod = PrintTypeModifiers ? ConvertModifierToString(pointer.Modifier) : string.Empty;
            pointeeType.NamePrefix.Append(mod);

            var qual = GetStringQuals(quals, false);
            if (!string.IsNullOrEmpty(qual))
                pointeeType.NamePrefix.Append(' ').Append(qual);

            return pointeeType;
        }

        public override TypePrinterResult VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            return string.Empty;
        }

        public override TypePrinterResult VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            var qual = GetStringQuals(quals);
            return $"{qual}{VisitPrimitiveType(builtin.Type)}";
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive,
            TypeQualifiers quals)
        {
            var qual = GetStringQuals(quals);
            return $"{qual}{VisitPrimitiveType(primitive)}";
        }

        public virtual TypePrinterResult VisitPrimitiveType(PrimitiveType primitive)
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
                case PrimitiveType.Null:
                    return PrintFlavorKind == CppTypePrintFlavorKind.Cpp ?
                        "std::nullptr_t" : "NULL";
                case PrimitiveType.String:
                {
                    switch (PrintFlavorKind)
                    {
                    case CppTypePrintFlavorKind.C:
                        return "const char*";
                    case CppTypePrintFlavorKind.Cpp:
                        return "std::string";
                    case CppTypePrintFlavorKind.ObjC:
                        return "NSString";
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
                case PrimitiveType.Decimal:
                {
                    switch (PrintFlavorKind)
                    {
                    case CppTypePrintFlavorKind.C:
                    case CppTypePrintFlavorKind.Cpp:
                        return "_Decimal32";
                    case CppTypePrintFlavorKind.ObjC:
                        return "NSDecimalNumber";
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }

            throw new NotSupportedException();
        }

        public override TypePrinterResult VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            FunctionType func;
            var qual = GetStringQuals(quals);
            if (ResolveTypedefs && !typedef.Declaration.Type.IsPointerTo(out func))
            {
                TypePrinterResult type = typedef.Declaration.QualifiedType.Visit(this);
                return new TypePrinterResult { Type = $"{qual}{type.Type}",
                    NamePrefix = type.NamePrefix, NameSuffix = type.NameSuffix };
            }

            var result = typedef.Declaration.Visit(this);
            if (result.NamePrefix.Length > 0)
                result.NamePrefix.Append($"{qual}");

            return result;
        }

        public override TypePrinterResult VisitAttributedType(AttributedType attributed,
            TypeQualifiers quals)
        {
            return attributed.Modified.Visit(this);
        }

        public override TypePrinterResult VisitDecayedType(DecayedType decayed,
            TypeQualifiers quals)
        {
            return decayed.Decayed.Visit(this);
        }

        public override TypePrinterResult VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            var specialization = template.GetClassTemplateSpecialization();
            if (specialization == null)
                return string.Empty;

            var qual = GetStringQuals(quals);
            return $"{qual}{VisitClassTemplateSpecializationDecl(specialization)}";
        }

        public override TypePrinterResult VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals)
        {
            if (template.Desugared.Type != null)
                return template.Desugared.Visit(this);
            return string.Empty;
        }

        public override TypePrinterResult VisitTemplateParameterType(
            TemplateParameterType param, TypeQualifiers quals)
        {
            if (param.Parameter == null || param.Parameter.Name == null)
                return string.Empty;

            return param.Parameter.Name;
        }

        public override TypePrinterResult VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            return param.Replacement.Type.Visit(this, quals);
        }

        public override TypePrinterResult VisitInjectedClassNameType(
            InjectedClassNameType injected, TypeQualifiers quals)
        {
            return injected.Class.Visit(this);
        }

        public override TypePrinterResult VisitDependentNameType(
            DependentNameType dependent, TypeQualifiers quals)
        {
            return dependent.Qualifier.Type != null ?
                dependent.Qualifier.Visit(this).Type : string.Empty;
        }

        public override TypePrinterResult VisitPackExpansionType(
            PackExpansionType packExpansionType, TypeQualifiers quals)
        {
            return string.Empty;
        }

        public override TypePrinterResult VisitUnaryTransformType(
            UnaryTransformType unaryTransformType, TypeQualifiers quals)
        {
            if (unaryTransformType.Desugared.Type != null)
                return unaryTransformType.Desugared.Visit(this);
            return unaryTransformType.BaseType.Visit(this);
        }

        public override TypePrinterResult VisitVectorType(VectorType vectorType,
            TypeQualifiers quals)
        {
            // an incomplete implementation but we'd hardly need anything better
            return "__attribute__()";
        }

        public override TypePrinterResult VisitCILType(CILType type, TypeQualifiers quals)
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

        public override TypePrinterResult VisitUnsupportedType(UnsupportedType type,
            TypeQualifiers quals)
        {
            return type.Description;
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl,
            TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public override TypePrinterResult VisitFunctionType(FunctionType function,
            TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false);

            var callingConvention = string.Empty;
            if (function.CallingConvention != CallingConvention.Default &&
                function.CallingConvention != CallingConvention.C)
            {
                string conventionString = function.CallingConvention.ToString();
                callingConvention = $"__{conventionString.ToLowerInvariant()} ";
            }
            return $"{returnType.Visit(this)} ({callingConvention}{{0}})({args})";
        }

        public override TypePrinterResult VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames = true)
        {
            var args = new List<string>();

            foreach (var param in @params)
                args.Add(VisitParameter(param, hasNames));

            if (PrintFlavorKind == CppTypePrintFlavorKind.ObjC)
                return string.Join(" ", args);

            return string.Join(", ", args);
        }

        public override TypePrinterResult VisitParameter(Parameter param,
            bool hasName = true)
        {
            var result = param.Type.Visit(this, param.QualifiedType.Qualifiers);

            string name = param.Name;
            bool printName = hasName && !string.IsNullOrEmpty(name);

            if (PrintFlavorKind == CppTypePrintFlavorKind.ObjC)
                return printName ? $":({result.Type}){name}" : $":({result.Type})";

            if (!printName)
                return result;

            result.Name = param.Name;
            return result.ToString();
        }

        public override TypePrinterResult VisitDelegate(FunctionType function)
        {
            throw new NotImplementedException();
        }

        public TypePrinterResult GetDeclName(Declaration declaration,
            TypePrintScopeKind scope)
        {
            switch (scope)
            {
            case TypePrintScopeKind.Local:
            {
                if (ContextKind == TypePrinterContextKind.Managed)
                {
                    return PrintLogicalNames ? declaration.LogicalName : declaration.Name;
                }

                return PrintLogicalNames ? declaration.LogicalOriginalName
                    : declaration.OriginalName;
            }
            case TypePrintScopeKind.Qualified:
            {
                if (ContextKind == TypePrinterContextKind.Managed)
                {
                    var outputNamespace = declaration.TranslationUnit?.Module?.OutputNamespace;
                    if (!string.IsNullOrEmpty(outputNamespace))
                        return $"{outputNamespace}::{declaration.QualifiedName}";

                    return declaration.QualifiedName;
                }

                if (declaration.Namespace is Class)
                    return $"{declaration.Namespace.Visit(this)}::{declaration.OriginalName}";

                return PrintLogicalNames ? declaration.QualifiedLogicalOriginalName
                    : declaration.QualifiedOriginalName;
            }
            case TypePrintScopeKind.GlobalQualified:
            {
                var name = (ContextKind == TypePrinterContextKind.Managed) ?
                            declaration.Name : declaration.OriginalName;

                if (declaration.Namespace is Class)
                    return $"{declaration.Namespace.Visit(this)}::{name}";

                var qualifier = PrintFlavorKind == CppTypePrintFlavorKind.Cpp ? "::" : string.Empty;
                return qualifier + GetDeclName(declaration, TypePrintScopeKind.Qualified);
            }
            }

            throw new NotSupportedException();
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl)
        {
            return GetDeclName(decl, ScopeKind);
        }

        public override TypePrinterResult VisitTranslationUnit(TranslationUnit unit)
        {
            return VisitDeclaration(unit);
        }

        public override TypePrinterResult VisitClassDecl(Class @class)
        {
            return VisitDeclaration(@class);
        }

        public override TypePrinterResult VisitClassTemplateSpecializationDecl(
            ClassTemplateSpecialization specialization)
        {
            return string.Format("{0}<{1}>", specialization.TemplatedDecl.Visit(this),
                string.Join(", ",
                specialization.Arguments.Where(
                    a => a.Type.Type != null &&
                        !(a.Type.Type is DependentNameType)).Select(a => a.Type.Visit(this))));
        }

        public override TypePrinterResult VisitFieldDecl(Field field)
        {
            return VisitDeclaration(field);
        }

        public override TypePrinterResult VisitFunctionDecl(Function function)
        {
            return VisitDeclaration(function);
        }

        public override TypePrinterResult VisitMethodDecl(Method method)
        {
            // HACK: this should never happen but there's an inexplicable crash
            // with the 32-bit Windows CI - needs investigation.
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

            string exceptionType;
            switch (functionType.ExceptionSpecType)
            {
                case ExceptionSpecType.BasicNoexcept:
                    exceptionType = " noexcept";
                    break;
                case ExceptionSpecType.NoexceptFalse:
                    exceptionType = " noexcept(false)";
                    break;
                case ExceptionSpecType.NoexceptTrue:
                    exceptionType = " noexcept(true)";
                    break;
                // TODO: research and handle the remaining cases
                default:
                    exceptionType = string.Empty;
                    break;
            }
            return $"{returnType}{@class}::{name}({@params}){@const}{exceptionType}";
        }

        public override TypePrinterResult VisitParameterDecl(Parameter parameter)
        {
            return VisitParameter(parameter, hasName: false);
        }

        public override TypePrinterResult VisitTypedefDecl(TypedefDecl typedef)
        {
            if (ResolveTypedefs)
                return typedef.Type.Visit(this);

            if (PrintFlavorKind != CppTypePrintFlavorKind.Cpp)
                return typedef.OriginalName;

            var originalNamespace = typedef.OriginalNamespace.Visit(this);
            return string.IsNullOrEmpty(originalNamespace) ||
                originalNamespace == "::" ?
                typedef.OriginalName : $"{originalNamespace}::{typedef.OriginalName}";
        }

        public override TypePrinterResult VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return VisitDeclaration(typeAlias);
        }

        public override TypePrinterResult VisitEnumDecl(Enumeration @enum)
        {
            return VisitDeclaration(@enum);
        }

        public override TypePrinterResult VisitEnumItemDecl(Enumeration.Item item)
        {
            return VisitDeclaration(item);
        }

        public override TypePrinterResult VisitVariableDecl(Variable variable)
        {
            return VisitDeclaration(variable);
        }

        public override TypePrinterResult VisitClassTemplateDecl(ClassTemplate template)
        {
            return VisitDeclaration(template);
        }

        public override TypePrinterResult VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            return VisitDeclaration(template);
        }

        public override TypePrinterResult VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitNamespace(Namespace @namespace)
        {
            return VisitDeclaration(@namespace);
        }

        public override TypePrinterResult VisitEvent(Event @event)
        {
            return string.Empty;
        }

        public override TypePrinterResult VisitProperty(Property property)
        {
            return VisitDeclaration(property);
        }

        public override TypePrinterResult VisitFriend(Friend friend)
        {
            throw new NotImplementedException();
        }

        public override string ToString(CppSharp.AST.Type type)
        {
            return type.Visit(this);
        }

        public override TypePrinterResult VisitTemplateTemplateParameterDecl(
            TemplateTemplateParameter templateTemplateParameter)
        {
            return templateTemplateParameter.Name;
        }

        public override TypePrinterResult VisitTemplateParameterDecl(
            TypeTemplateParameter templateParameter)
        {
            if (templateParameter.DefaultArgument.Type == null)
                return templateParameter.Name;

            return $"{templateParameter.Name} = {templateParameter.DefaultArgument.Visit(this)}";
        }

        public override TypePrinterResult VisitNonTypeTemplateParameterDecl(
            NonTypeTemplateParameter nonTypeTemplateParameter)
        {
            if (nonTypeTemplateParameter.DefaultArgument == null)
                return nonTypeTemplateParameter.Name;

            return $"{nonTypeTemplateParameter.Name} = {nonTypeTemplateParameter.DefaultArgument.String}";
        }

        public override TypePrinterResult VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            return VisitDeclaration(typedef);
        }

        public override TypePrinterResult VisitTypeAliasTemplateDecl(TypeAliasTemplate typeAliasTemplate)
        {
            return VisitDeclaration(typeAliasTemplate);
        }

        public override TypePrinterResult VisitFunctionTemplateSpecializationDecl(
            FunctionTemplateSpecialization specialization)
        {
            throw new NotImplementedException();
        }

        public override TypePrinterResult VisitVarTemplateDecl(VarTemplate template)
        {
            return VisitDeclaration(template);
        }

        public override TypePrinterResult VisitVarTemplateSpecializationDecl(
            VarTemplateSpecialization template)
        {
            return VisitDeclaration(template);
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
