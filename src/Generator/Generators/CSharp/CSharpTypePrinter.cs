using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;
using Type = CppSharp.AST.Type;
using ParserTargetInfo = CppSharp.Parser.ParserTargetInfo;
using System.Linq;
using System.Text;

namespace CppSharp.Generators.CSharp
{
    public class CSharpTypePrinterContext : TypePrinterContext
    {
        public TypePrinterContextKind CSharpKind;
        public MarshalKind MarshalKind;
        public QualifiedType FullType;
    }

    public class CSharpTypePrinter : TypePrinter
    {
        public const string IntPtrType = "global::System.IntPtr";

        private readonly Stack<TypePrinterContextKind> contexts;
        private readonly Stack<MarshalKind> marshalKinds;

        public TypePrinterContextKind ContextKind => contexts.Peek();

        public MarshalKind MarshalKind => marshalKinds.Peek();

        public CSharpTypePrinterContext TypePrinterContext;

        public BindingContext Context { get; set; }

        public DriverOptions Options => Context.Options;
        public TypeMapDatabase TypeMapDatabase => Context.TypeMaps;

        public CSharpTypePrinter(BindingContext context)
        {
            Context = context;
            contexts = new Stack<TypePrinterContextKind>();
            marshalKinds = new Stack<MarshalKind>();
            PushContext(TypePrinterContextKind.Managed);
            PushMarshalKind(MarshalKind.Unknown);

            TypePrinterContext = new CSharpTypePrinterContext();
        }

        public void PushContext(TypePrinterContextKind contextKind)
        {
            contexts.Push(contextKind);
        }

        public TypePrinterContextKind PopContext() => contexts.Pop();

        public void PushMarshalKind(MarshalKind marshalKind)
        {
            marshalKinds.Push(marshalKind);
        }

        public MarshalKind PopMarshalKind() => marshalKinds.Pop();

        public override TypePrinterResult VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (tag.Declaration == null)
                return string.Empty;

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(tag.Declaration, out typeMap))
            {
                typeMap.Type = tag;
                TypePrinterContext.CSharpKind = ContextKind;
                TypePrinterContext.MarshalKind = MarshalKind;
                TypePrinterContext.Type = tag;

                string type = typeMap.CSharpSignature(TypePrinterContext);
                if (!string.IsNullOrEmpty(type))
                {
                    return new TypePrinterResult
                    {
                        Type = type,
                        TypeMap = typeMap
                    };
                }
            }

            return tag.Declaration.Visit(this);
        }

        public override TypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            if (ContextKind == TypePrinterContextKind.Native &&
                array.SizeType == ArrayType.ArraySize.Constant)
            {
                Type arrayType = array.Type.Desugar();

                PrimitiveType primitiveType;
                if ((arrayType.IsPointerToPrimitiveType(out primitiveType) &&
                    !(arrayType is FunctionType)) || 
                    (arrayType.IsPrimitiveType() && MarshalKind != MarshalKind.NativeField))
                {
                    if (primitiveType == PrimitiveType.Void)
                    {
                        return "void*";
                    }
                    return string.Format("{0}", array.Type.Visit(this, quals));
                }

                if (TypePrinterContext.Parameter != null)
                    return string.Format("global::System.IntPtr");

                Enumeration @enum;
                if (arrayType.TryGetEnum(out @enum))
                {
                    return new TypePrinterResult
                    {
                        Type = string.Format("fixed {0}", @enum.BuiltinType),
                        NameSuffix = string.Format("[{0}]", array.Size)
                    };
                }

                Class @class;
                if (arrayType.TryGetClass(out @class))
                {
                    return new TypePrinterResult
                    {
                        Type = "fixed byte",
                        NameSuffix = string.Format("[{0}]", array.Size * @class.Layout.Size)
                    };
                }

                var arrayElemType = array.Type.Visit(this, quals).ToString();

                // C# does not support fixed arrays of machine pointer type (void* or IntPtr).
                // In that case, replace it by a pointer to an integer type of the same size.
                if (arrayElemType == IntPtrType)
                    arrayElemType = Context.TargetInfo.PointerWidth == 64 ? "long" : "int";

                // Do not write the fixed keyword multiple times for nested array types
                var fixedKeyword = array.Type is ArrayType ? string.Empty : "fixed ";
                return new TypePrinterResult
                {
                    Type = string.Format("{0}{1}", fixedKeyword, arrayElemType),
                    NameSuffix = string.Format("[{0}]", array.Size)
                };
            }

            // const char* and const char[] are the same so we can use a string
            if (array.SizeType == ArrayType.ArraySize.Incomplete &&
                array.Type.Desugar().IsPrimitiveType(PrimitiveType.Char) &&
                array.QualifiedType.Qualifiers.IsConst)
                return "string";

            return string.Format("{0}{1}", array.Type.Visit(this),
                array.SizeType == ArrayType.ArraySize.Constant ? "[]" :
                   (ContextKind == TypePrinterContextKind.Managed ? "*" : string.Empty));

            // C# only supports fixed arrays in unsafe sections
            // and they are constrained to a set of built-in types.
        }

        public override TypePrinterResult VisitFunctionType(FunctionType function,
            TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            PushMarshalKind(MarshalKind.GenericDelegate);

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false).Type;

            PopMarshalKind();

            if (ContextKind != TypePrinterContextKind.Managed)
                return IntPtrType;

            if (returnType.Type.IsPrimitiveType(PrimitiveType.Void))
            {
                if (!string.IsNullOrEmpty(args))
                    args = string.Format("<{0}>", args);
                return string.Format("Action{0}", args);
            }

            if (!string.IsNullOrEmpty(args))
                args = string.Format(", {0}", args);

            PushMarshalKind(MarshalKind.GenericDelegate);

            var returnTypePrinterResult = returnType.Visit(this);

            PopMarshalKind();

            return string.Format("Func<{0}{1}>", returnTypePrinterResult, args);
        }

        public static bool IsConstCharString(PointerType pointer)
        {
            var pointee = pointer.Pointee.Desugar();

            return (pointee.IsPrimitiveType(PrimitiveType.Char) ||
                    pointee.IsPrimitiveType(PrimitiveType.Char16) ||
                    pointee.IsPrimitiveType(PrimitiveType.WideChar)) &&
                    pointer.QualifiedPointee.Qualifiers.IsConst;
        }

        public static bool IsConstCharString(Type type)
        {
            var desugared = type.Desugar();

            if (!(desugared is PointerType))
                return false;

            var pointer = desugared as PointerType;
            return IsConstCharString(pointer);
        }

        public static bool IsConstCharString(QualifiedType qualType)
        {
            return IsConstCharString(qualType.Type);
        }

        private bool allowStrings = true;

        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return string.Format("{0}", function.Visit(this, quals));
            }

            var isManagedContext = ContextKind == TypePrinterContextKind.Managed;

            if (allowStrings && IsConstCharString(pointer))
            {
                if (isManagedContext || MarshalKind == MarshalKind.GenericDelegate)
                    return "string";
                if (TypePrinterContext.Parameter == null || TypePrinterContext.Parameter.Name == Helpers.ReturnIdentifier)
                    return IntPtrType;
                if (Options.Encoding == Encoding.ASCII)
                    return string.Format("[MarshalAs(UnmanagedType.LPStr)] string");
                if (Options.Encoding == Encoding.Unicode ||
                    Options.Encoding == Encoding.BigEndianUnicode)
                    return string.Format("[MarshalAs(UnmanagedType.LPWStr)] string");
                throw new NotSupportedException(string.Format("{0} is not supported yet.",
                    Options.Encoding.EncodingName));
            }

            var desugared = pointee.Desugar();

            // From http://msdn.microsoft.com/en-us/library/y31yhkeb.aspx
            // Any of the following types may be a pointer type:
            // * sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double,
            //   decimal, or bool.
            // * Any enum type.
            // * Any pointer type.
            // * Any user-defined struct type that contains fields of unmanaged types only.
            var finalPointee = pointer.GetFinalPointee();
            if (finalPointee.IsPrimitiveType())
            {
                // Skip one indirection if passed by reference
                var param = TypePrinterContext.Parameter;
                bool isRefParam = param != null && (param.IsOut || param.IsInOut);
                if (isManagedContext && isRefParam)
                    return pointer.QualifiedPointee.Visit(this);

                if (pointee.IsPrimitiveType(PrimitiveType.Void))
                    return IntPtrType;

                if (IsConstCharString(pointee) && isRefParam)
                    return IntPtrType + "*";

                // Do not allow strings inside primitive arrays case, else we'll get invalid types
                // like string* for const char **.
                allowStrings = isRefParam;
                var result = pointer.QualifiedPointee.Visit(this);
                allowStrings = true;

                return !isRefParam && result.Type == IntPtrType ? "void**" : result + "*";
            }

            Enumeration @enum;
            if (desugared.TryGetEnum(out @enum))
            {
                // Skip one indirection if passed by reference
                var param = TypePrinterContext.Parameter;
                if (isManagedContext && param != null && (param.IsOut || param.IsInOut)
                    && pointee == finalPointee)
                    return pointer.QualifiedPointee.Visit(this);

                return pointer.QualifiedPointee.Visit(this) + "*";
            }

            Class @class;
            if ((desugared.IsDependent || desugared.TryGetClass(out @class) ||
                (desugared is ArrayType && TypePrinterContext.Parameter != null))
                && ContextKind == TypePrinterContextKind.Native)
            {
                return IntPtrType;
            }

            return pointer.QualifiedPointee.Visit(this);
        }

        public override TypePrinterResult VisitMemberPointerType(MemberPointerType member,
            TypeQualifiers quals)
        {
            FunctionType functionType;
            if (member.IsPointerTo(out functionType))
                return functionType.Visit(this, quals);

            // TODO: Non-function member pointer types are tricky to support.
            // Re-visit this.
            return IntPtrType;
        }

        public override TypePrinterResult VisitBuiltinType(BuiltinType builtin,
            TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type, quals);
        }

        public override TypePrinterResult VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.Type = typedef;
                TypePrinterContext.CSharpKind = ContextKind;
                TypePrinterContext.MarshalKind = MarshalKind;
                TypePrinterContext.Type = typedef;

                string type = typeMap.CSharpSignature(TypePrinterContext);
                if (!string.IsNullOrEmpty(type))
                {
                    return new TypePrinterResult
                    {
                        Type = type,
                        TypeMap = typeMap
                    };
                }
            }

            FunctionType func;
            if (decl.Type.IsPointerTo(out func))
            {
                if (ContextKind == TypePrinterContextKind.Native)
                    return IntPtrType;
                // TODO: Use SafeIdentifier()
                return VisitDeclaration(decl);
            }

            return decl.Type.Visit(this);
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
            var decl = template.GetClassTemplateSpecialization() ??
                template.Template.TemplatedDecl;

            TypeMap typeMap;
            if (!TypeMapDatabase.FindTypeMap(template, out typeMap))
                return decl.Visit(this);

            typeMap.Declaration = decl;
            typeMap.Type = template;
            TypePrinterContext.Type = template;
            TypePrinterContext.CSharpKind = ContextKind;
            TypePrinterContext.MarshalKind = MarshalKind;

            var type = GetCSharpSignature(typeMap);
            if (!string.IsNullOrEmpty(type))
            {
                return new TypePrinterResult
                {
                    Type = type,
                    TypeMap = typeMap
                };
            }

            return decl.Visit(this);
        }

        public override TypePrinterResult VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals)
        {
            if (template.Desugared.Type != null)
                return template.Desugared.Visit(this);
            return string.Empty;
        }

        private string GetCSharpSignature(TypeMap typeMap)
        {
            TypePrinterContext.CSharpKind = ContextKind;
            TypePrinterContext.MarshalKind = MarshalKind;
            return typeMap.CSharpSignature(TypePrinterContext);
        }

        public override TypePrinterResult VisitTemplateParameterType(
            TemplateParameterType param, TypeQualifiers quals)
        {
            return param.Parameter.Name;
        }

        public override TypePrinterResult VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals)
        {
            var type = param.Replacement.Type;
            return type.Visit(this, param.Replacement.Qualifiers);
        }

        public override TypePrinterResult VisitInjectedClassNameType(
            InjectedClassNameType injected, TypeQualifiers quals)
        {
            return injected.Class.Name;
        }

        public override TypePrinterResult VisitDependentNameType(DependentNameType dependent,
            TypeQualifiers quals)
        {
            return dependent.Desugared.Type != null ? dependent.Desugared.Visit(this) : string.Empty;
        }

        public override TypePrinterResult VisitPackExpansionType(PackExpansionType type,
            TypeQualifiers quals)
        {
            return string.Empty;
        }

        public override TypePrinterResult VisitCILType(CILType type, TypeQualifiers quals)
        {
            return type.Type.FullName;
        }

        public static void GetPrimitiveTypeWidth(PrimitiveType primitive,
            ParserTargetInfo targetInfo, out uint width, out bool signed)
        {
            switch (primitive)
            {
                case PrimitiveType.Short:
                    width = targetInfo?.ShortWidth ?? 16;
                    signed = true;
                    break;
                case PrimitiveType.UShort:
                    width = targetInfo?.ShortWidth ?? 16;
                    signed = false;
                    break;
                case PrimitiveType.Int:
                    width = targetInfo?.IntWidth ?? 32;
                    signed = true;
                    break;
                case PrimitiveType.UInt:
                    width = targetInfo?.IntWidth ?? 32;
                    signed = false;
                    break;
                case PrimitiveType.Long:
                    width = targetInfo?.LongWidth ?? 32;
                    signed = true;
                    break;
                case PrimitiveType.ULong:
                    width = targetInfo?.LongWidth ?? 32;
                    signed = false;
                    break;
                case PrimitiveType.LongLong:
                    width = targetInfo?.LongLongWidth ?? 64;
                    signed = true;
                    break;
                case PrimitiveType.ULongLong:
                    width = targetInfo?.LongLongWidth ?? 64;
                    signed = false;
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

       static string GetIntString(PrimitiveType primitive, ParserTargetInfo targetInfo)
        {
            uint width;
            bool signed;

            GetPrimitiveTypeWidth(primitive, targetInfo, out width, out signed);

            switch (width)
            {
                case 8:
                    return signed ? "sbyte" : "byte";
                case 16:
                    return signed ? "short" : "ushort";
                case 32:
                    return signed ? "int" : "uint";
                case 64:
                    return signed ? "long" : "ulong";
                default:
                    throw new NotImplementedException();
            }
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive,
            TypeQualifiers quals)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool:
                    // returned structs must be blittable and bool isn't
                    return MarshalKind == MarshalKind.NativeField ?
                        "byte" : "bool";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar: return "char";
                case PrimitiveType.Char:
                    // returned structs must be blittable and char isn't
                    return Options.MarshalCharAsManagedChar &&
                        ContextKind != TypePrinterContextKind.Native
                        ? "char"
                        : "sbyte";
                case PrimitiveType.SChar: return "sbyte";
                case PrimitiveType.UChar: return "byte";
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                case PrimitiveType.ULongLong:
                    return GetIntString(primitive, Context.TargetInfo);
                case PrimitiveType.Int128: return "__int128";
                case PrimitiveType.UInt128: return "__uint128_t";
                case PrimitiveType.Half: return "__fp16";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                // not really supported yet but it's closest, and we don't want crashes when parsing long doubles
                case PrimitiveType.LongDouble: return "decimal";
                case PrimitiveType.IntPtr: return IntPtrType;
                case PrimitiveType.UIntPtr: return "global::System.UIntPtr";
                case PrimitiveType.Null: return "void*";
                case PrimitiveType.String: return "string";
            }

            throw new NotSupportedException();
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl,
            TypeQualifiers quals)
        {
            return VisitDeclaration(decl);
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl)
        {
            return GetName(decl);
        }

        public override TypePrinterResult VisitClassDecl(Class @class)
        {
            if (ContextKind == TypePrinterContextKind.Native)
                return $"{GetName(@class.OriginalClass ?? @class)}.{Helpers.InternalStruct}";

            return $@"{GetName(@class)}{(@class.IsDependent ? $@"<{
                string.Join(", ", @class.TemplateParameters.Select(p => p.Name))}>" : string.Empty)}";
        }

        public override TypePrinterResult VisitClassTemplateSpecializationDecl(
            ClassTemplateSpecialization specialization)
        {
            if (ContextKind == TypePrinterContextKind.Native)
                return $@"{VisitClassDecl(specialization)}{
                    Helpers.GetSuffixForInternal(specialization)}";
            return VisitClassDecl(specialization);
        }

        public override TypePrinterResult VisitParameterDecl(Parameter parameter)
        {
            var paramType = parameter.Type;

            if (parameter.Kind == ParameterKind.IndirectReturnType)
                return IntPtrType;

            TypePrinterContext.Parameter = parameter;
            var ret = paramType.Visit(this);
            TypePrinterContext.Parameter = null;

            return ret;
        }

        public override TypePrinterResult VisitTypedefDecl(TypedefDecl typedef)
        {
            return GetName(typedef);
        }

        public override TypePrinterResult VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return GetName(typeAlias);
        }

        public override TypePrinterResult VisitEnumDecl(Enumeration @enum)
        {
            return GetName(@enum);
        }

        public override TypePrinterResult VisitEnumItemDecl(Enumeration.Item item)
        {
            return VisitDeclaration(item);
        }

        public string GetName(Declaration decl)
        {
            var names = new Stack<string>();

            Declaration ctx;
            var specialization = decl as ClassTemplateSpecialization;
            if (specialization != null && ContextKind == TypePrinterContextKind.Native)
            {
                ctx = specialization.TemplatedDecl.TemplatedClass.Namespace;
                if (specialization.OriginalNamespace is Class &&
                    !(specialization.OriginalNamespace is ClassTemplateSpecialization))
                {
                    names.Push(string.Format("{0}_{1}", decl.OriginalNamespace.Name, decl.Name));
                    ctx = ctx.Namespace ?? ctx;
                }
                else
                {
                    names.Push(decl.Name);
                }
            }
            else
            {
                names.Push(decl.Name);
                ctx = decl.Namespace;
            }

            if (decl is Variable && !(decl.Namespace is Class))
                names.Push(decl.TranslationUnit.FileNameWithoutExtension);

            while (!(ctx is TranslationUnit))
            {
                if (!string.IsNullOrWhiteSpace(ctx.Name))
                    names.Push(ctx.Name);

                ctx = ctx.Namespace;
            }

            if (!ctx.TranslationUnit.IsSystemHeader && ctx.TranslationUnit.IsValid &&
                !string.IsNullOrWhiteSpace(ctx.TranslationUnit.Module.OutputNamespace))
                names.Push(ctx.TranslationUnit.Module.OutputNamespace);

            return $"global::{string.Join(".", names)}";
        }

        public override TypePrinterResult VisitVariableDecl(Variable variable)
        {
            return GetName(variable);
        }

        public override TypePrinterResult VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames)
        {
            var args = new List<string>();

            foreach (var param in @params)
            {
                TypePrinterContext.Parameter = param;
                args.Add(VisitParameter(param, hasNames).Type);
            }

            TypePrinterContext.Parameter = null;
            return string.Join(", ", args);
        }

        public override TypePrinterResult VisitParameter(Parameter arg, bool hasName)
        {
            var type = arg.Type.Visit(this, arg.QualifiedType.Qualifiers);
            var name = arg.Name;

            if (hasName && !string.IsNullOrEmpty(name))
                return string.Format("{0} {1}", type, name);

            return type;
        }

        public override TypePrinterResult VisitDelegate(FunctionType function)
        {
            return string.Format("delegate {0} {{0}}({1})",
                function.ReturnType.Visit(this),
                VisitParameters(function.Parameters, hasNames: true));
        }

        public override string ToString(Type type)
        {
            return type.Visit(this).Type;
        }

        public override TypePrinterResult VisitTemplateTemplateParameterDecl(
            TemplateTemplateParameter templateTemplateParameter)
        {
            return templateTemplateParameter.Name;
        }

        public override TypePrinterResult VisitTemplateParameterDecl(
            TypeTemplateParameter templateParameter)
        {
            return templateParameter.Name;
        }

        public override TypePrinterResult VisitNonTypeTemplateParameterDecl(
            NonTypeTemplateParameter nonTypeTemplateParameter)
        {
            return nonTypeTemplateParameter.Name;
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
            return vectorType.ElementType.Visit(this);
        }
    }

    public static class CSharpTypePrinterExtensions
    {
        public static TypePrinterResult CSharpType(this QualifiedType type,
            CSharpTypePrinter printer)
        {
            printer.TypePrinterContext.FullType = type;
            return type.Visit(printer);
        }

        public static TypePrinterResult CSharpType(this Type type,
            CSharpTypePrinter printer)
        {
            return CSharpType(new QualifiedType(type), printer);
        }

        public static TypePrinterResult CSharpType(this Declaration decl,
            CSharpTypePrinter printer)
        {
            if (decl is ITypedDecl)
            {
                var type = (decl as ITypedDecl).QualifiedType;
                printer.TypePrinterContext.FullType = type;
            }

            return decl.Visit(printer);
        }
    }
}
