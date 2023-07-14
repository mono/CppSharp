using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Extensions;
using CppSharp.Types;
using ParserTargetInfo = CppSharp.Parser.ParserTargetInfo;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public class CSharpTypePrinter : TypePrinter
    {
        public string IntPtrType => "__IntPtr";

        public DriverOptions Options => Context.Options;
        public TypeMapDatabase TypeMapDatabase => Context.TypeMaps;

        public bool PrintModuleOutputNamespace = true;

        public CSharpTypePrinter()
        {
        }

        public CSharpTypePrinter(BindingContext context)
        {
            Context = context;
        }

        public string QualifiedType(string name)
        {
            return IsGlobalQualifiedScope ? $"global::{name}" : name;
        }

        public override TypePrinterResult VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (tag.Declaration == null)
                return string.Empty;

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(tag, out typeMap))
            {
                typeMap.Type = tag;

                var typePrinterContext = new TypePrinterContext()
                {
                    Kind = ContextKind,
                    MarshalKind = MarshalKind,
                    Type = tag
                };

                return typeMap.CSharpSignatureType(typePrinterContext).ToString();
            }

            return base.VisitTagType(tag, quals);
        }

        public override TypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            Type arrayType = array.Type.Desugar();
            if ((MarshalKind == MarshalKind.NativeField ||
                (ContextKind == TypePrinterContextKind.Native &&
                 MarshalKind == MarshalKind.ReturnVariableArray)) &&
                array.SizeType == ArrayType.ArraySize.Constant)
            {
                if (array.Size == 0)
                {
                    var pointer = new PointerType(array.QualifiedType);
                    return pointer.Visit(this);
                }

                PrimitiveType primitiveType;
                if ((arrayType.IsPointerToPrimitiveType(out primitiveType) &&
                    !(arrayType is FunctionType)) ||
                    (arrayType.IsPrimitiveType() && MarshalKind != MarshalKind.NativeField))
                {
                    if (primitiveType == PrimitiveType.Void)
                        return "void*";

                    return array.QualifiedType.Visit(this);
                }

                if (Parameter != null)
                    return IntPtrType;

                Enumeration @enum;
                if (arrayType.TryGetEnum(out @enum))
                {
                    return new TypePrinterResult($"fixed {@enum.BuiltinType}",
                        $"[{array.Size}]");
                }

                if (arrayType.IsClass())
                    return new TypePrinterResult($"fixed byte", $"[{array.GetSizeInBytes()}]");

                TypePrinterResult arrayElemType = array.QualifiedType.Visit(this);

                // C# does not support fixed arrays of machine pointer type (void* or IntPtr).
                // In that case, replace it by a pointer to an integer type of the same size.
                if (arrayElemType == IntPtrType)
                    arrayElemType = Context.TargetInfo.PointerWidth == 64 ? "long" : "int";

                // Do not write the fixed keyword multiple times for nested array types
                var fixedKeyword = arrayType is ArrayType ? string.Empty : "fixed ";
                return new TypePrinterResult(fixedKeyword + arrayElemType.Type,
                    $"[{array.Size}]");
            }

            // const char* and const char[] are the same so we can use a string
            if (Context.Options.MarshalConstCharArrayAsString &&
                array.SizeType == ArrayType.ArraySize.Incomplete &&
                arrayType.IsPrimitiveType(PrimitiveType.Char) &&
                array.QualifiedType.Qualifiers.IsConst)
                return "string";

            if (arrayType.IsPointerToPrimitiveType(PrimitiveType.Char))
            {
                var prefix = ContextKind == TypePrinterContextKind.Managed ? string.Empty :
                    "[MarshalAs(UnmanagedType.LPArray)] ";
                return $"{prefix}string[]";
            }

            if (arrayType.IsPrimitiveType(PrimitiveType.Bool))
            {
                var prefix = ContextKind == TypePrinterContextKind.Managed ? string.Empty :
                    "[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I1)] ";
                return $"{prefix}bool[]";
            }

            if (Context.Options.UseSpan && !(array.SizeType != ArrayType.ArraySize.Constant &&
                MarshalKind == MarshalKind.ReturnVariableArray))
            {
                if (ContextKind == TypePrinterContextKind.Managed)
                {
                    return $"Span<{arrayType.Visit(this)}>";
                }
                else
                {
                    return $"{arrayType.Visit(this)}*"; ;

                }
            }

            var arraySuffix = array.SizeType != ArrayType.ArraySize.Constant &&
                MarshalKind == MarshalKind.ReturnVariableArray ?
                (ContextKind == TypePrinterContextKind.Managed &&
                 arrayType.IsPrimitiveType() ? "*" : string.Empty) : "[]";
            return $"{arrayType.Visit(this)}{arraySuffix}";
        }

        public override TypePrinterResult VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(builtin, out typeMap))
            {
                var typePrinterContext = new TypePrinterContext()
                {
                    Kind = ContextKind,
                    MarshalKind = MarshalKind,
                    Type = builtin,
                    Parameter = Parameter
                };
                return typeMap.CSharpSignatureType(typePrinterContext).Visit(this);
            }
            return base.VisitBuiltinType(builtin, quals);
        }

        public override TypePrinterResult VisitFunctionType(FunctionType function, TypeQualifiers quals)
            => IntPtrType;

        private bool allowStrings = true;

        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            if (MarshalKind == MarshalKind.NativeField && !pointer.Pointee.IsEnumType())
                return IntPtrType;

            if (pointer.Pointee is FunctionType)
                return pointer.Pointee.Visit(this, quals);

            var isManagedContext = ContextKind == TypePrinterContextKind.Managed;

            if (allowStrings && pointer.IsConstCharString())
            {
                TypeMap typeMap;
                TypeMapDatabase.FindTypeMap(pointer, out typeMap);
                var typePrinterContext = new TypePrinterContext()
                {
                    Kind = ContextKind,
                    MarshalKind = MarshalKind,
                    Type = pointer.Pointee,
                    Parameter = Parameter
                };
                return typeMap.CSharpSignatureType(typePrinterContext).Visit(this);
            }

            var pointee = pointer.Pointee.Desugar();

            if (isManagedContext &&
                new QualifiedType(pointer, quals).IsConstRefToPrimitive())
                return pointee.Visit(this);

            // From http://msdn.microsoft.com/en-us/library/y31yhkeb.aspx
            // Any of the following types may be a pointer type:
            // * sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double,
            //   decimal, or bool.
            // * Any enum type.
            // * Any pointer type.
            // * Any user-defined struct type that contains fields of unmanaged types only.
            var finalPointee = (pointee.GetFinalPointee() ?? pointee).Desugar();
            if (finalPointee.IsPrimitiveType() || finalPointee.IsEnum())
            {
                // Skip one indirection if passed by reference
                bool isRefParam = Parameter != null && (Parameter.IsOut || Parameter.IsInOut);
                if (isManagedContext && isRefParam)
                    return pointer.QualifiedPointee.Visit(this);

                if (pointee.IsPrimitiveType(PrimitiveType.Void))
                    return IntPtrType;

                if (pointee.IsConstCharString() && isRefParam)
                    return IntPtrType + "*";

                // Do not allow strings inside primitive arrays case, else we'll get invalid types
                // like string* for const char **.
                allowStrings = isRefParam;
                var result = pointer.QualifiedPointee.Visit(this);
                allowStrings = true;

                string @ref = Parameter != null && Parameter.IsIndirect ? string.Empty : "*";
                return result + @ref;
            }

            if ((pointee.IsDependent || pointee.IsClass())
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

        public override TypePrinterResult VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(typedef, out typeMap))
            {
                typeMap.Type = typedef;

                var typePrinterContext = new TypePrinterContext
                {
                    Kind = ContextKind,
                    MarshalKind = MarshalKind,
                    Type = typedef,
                    Parameter = Parameter
                };

                return typeMap.CSharpSignatureType(typePrinterContext).ToString();
            }

            FunctionType func;
            if (decl.Type.IsPointerTo(out func))
            {
                if (MarshalKind == MarshalKind.GenericDelegate && ContextKind == TypePrinterContextKind.Native)
                    return VisitDeclaration(decl);
            }

            func = decl.Type as FunctionType;
            if (func != null || decl.Type.IsPointerTo(out func))
            {
                if (ContextKind == TypePrinterContextKind.Native)
                    return IntPtrType;
                // TODO: Use SafeIdentifier()
                return VisitDeclaration(decl);
            }

            return decl.Type.Visit(this);
        }

        public override TypePrinterResult VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            var decl = template.GetClassTemplateSpecialization() ??
                template.Template.TemplatedDecl;

            TypeMap typeMap;
            if (!TypeMapDatabase.FindTypeMap(template, out typeMap))
            {
                if (ContextKind == TypePrinterContextKind.Managed &&
                    decl == template.Template.TemplatedDecl &&
                    template.Arguments.All(IsValid))
                {
                    List<TemplateArgument> args = template.Arguments;
                    var @class = (Class)template.Template.TemplatedDecl;
                    TemplateArgument lastArg = args.Last();
                    TypePrinterResult typePrinterResult = VisitDeclaration(decl);
                    typePrinterResult.NameSuffix.Append($@"<{string.Join(", ",
                       args.Concat(Enumerable.Range(0, @class.TemplateParameters.Count - args.Count).Select(
                           i => lastArg)).Select(this.VisitTemplateArgument))}>");
                    return typePrinterResult;
                }

                if (ContextKind == TypePrinterContextKind.Native)
                    return template.Desugared.Visit(this);

                return decl.Visit(this);
            }

            typeMap.Type = template;

            var typePrinterContext = new TypePrinterContext
            {
                Type = template,
                Kind = ContextKind,
                MarshalKind = MarshalKind
            };

            return typeMap.CSharpSignatureType(typePrinterContext).ToString();
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
            return injected.InjectedSpecializationType.Type != null ?
                injected.InjectedSpecializationType.Visit(this) :
                injected.Class.Visit(this);
        }

        public override TypePrinterResult VisitDependentNameType(DependentNameType dependent,
            TypeQualifiers quals)
        {
            if (dependent.Qualifier.Type == null)
                return dependent.Identifier;
            return $"{dependent.Qualifier.Visit(this)}.{dependent.Identifier}";
        }

        public override TypePrinterResult VisitPackExpansionType(PackExpansionType type,
            TypeQualifiers quals)
        {
            return string.Empty;
        }

        public override TypePrinterResult VisitCILType(CILType type, TypeQualifiers quals)
        {
            switch (System.Type.GetTypeCode(type.Type))
            {
                case TypeCode.Boolean:
                    return "bool";
                case TypeCode.Char:
                    return "char";
                case TypeCode.SByte:
                    return "sbyte";
                case TypeCode.Byte:
                    return "byte";
                case TypeCode.Int16:
                    return "short";
                case TypeCode.UInt16:
                    return "ushort";
                case TypeCode.Int32:
                    return "int";
                case TypeCode.UInt32:
                    return "uint";
                case TypeCode.Int64:
                    return "long";
                case TypeCode.UInt64:
                    return "ulong";
                case TypeCode.Single:
                    return "float";
                case TypeCode.Double:
                    return "double";
                case TypeCode.Decimal:
                    return "decimal";
                case TypeCode.String:
                    return "string";
            }

            return QualifiedType(type.Type.FullName);
        }

        public static void GetPrimitiveTypeWidth(PrimitiveType primitive, ParserTargetInfo targetInfo, out uint width, out bool signed)
        {
            width = primitive.GetInfo(targetInfo, out signed).Width;
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
                case PrimitiveType.Int128:
                    return new TypePrinterResult("fixed byte",
"[16]"); // The type is always 128 bits wide
                case PrimitiveType.UInt128:
                    return new TypePrinterResult("fixed byte",
"[16]"); // The type is always 128 bits wide
                case PrimitiveType.Half:
                    return new TypePrinterResult("fixed byte",
$"[{Context.TargetInfo.HalfWidth}]");
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.LongDouble:
                    return new TypePrinterResult("fixed byte",
$"[{Context.TargetInfo.LongDoubleWidth}]");
                case PrimitiveType.IntPtr: return IntPtrType;
                case PrimitiveType.UIntPtr: return QualifiedType("System.UIntPtr");
                case PrimitiveType.Null: return "void*";
                case PrimitiveType.String: return "string";
                case PrimitiveType.Float128: return "__float128";
            }

            throw new NotSupportedException();
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl)
        {
            return GetName(decl);
        }

        public override TypePrinterResult VisitClassDecl(Class @class)
        {
            if (ContextKind == TypePrinterContextKind.Native)
                return $@"{VisitDeclaration(@class.OriginalClass ?? @class)}.{
                    Helpers.InternalStruct}{Helpers.GetSuffixForInternal(@class)}";

            TypePrinterResult printed = VisitDeclaration(@class);
            if (@class.IsTemplate)
                printed.NameSuffix.Append($@"<{string.Join(", ",
                    @class.TemplateParameters.Select(p => p.Name))}>");
            return printed;
        }

        public override TypePrinterResult VisitClassTemplateSpecializationDecl(
            ClassTemplateSpecialization specialization)
        {
            TypePrinterResult typePrinterResult = VisitClassDecl(specialization);
            if (ContextKind != TypePrinterContextKind.Native)
                typePrinterResult.NameSuffix.Append($@"<{string.Join(", ",
                    specialization.Arguments.Select(VisitTemplateArgument))}>");
            return typePrinterResult;
        }

        public TypePrinterResult VisitTemplateArgument(TemplateArgument a)
        {
            if (a.Type.Type == null)
                return a.Integral.ToString(CultureInfo.InvariantCulture);
            var type = a.Type.Type;
            PrimitiveType pointee;
            if (type.IsPointerToPrimitiveType(out pointee) && !type.IsConstCharString())
            {
                return $@"CppSharp.Runtime.Pointer<{(pointee == PrimitiveType.Void ? IntPtrType :
                    VisitPrimitiveType(pointee, new TypeQualifiers()).Type)}>";
            }
            return type.IsPrimitiveType(PrimitiveType.Void) ?
                new TypePrinterResult("object") : type.Visit(this);
        }

        public override TypePrinterResult VisitParameterDecl(Parameter parameter)
        {
            var paramType = parameter.Type;

            if (parameter.Kind == ParameterKind.IndirectReturnType)
                return IntPtrType;

            Parameter = parameter;
            var ret = paramType.Visit(this);
            Parameter = null;

            return ret;
        }

        private string GetName(Declaration decl)
        {
            var names = new Stack<string>();

            Declaration ctx = decl;
            if (decl is ClassTemplateSpecialization specialization &&
                ContextKind == TypePrinterContextKind.Native &&
                specialization.OriginalNamespace is Class &&
                !(specialization.OriginalNamespace is ClassTemplateSpecialization))
            {
                names.Push(string.Format("{0}_{1}", decl.OriginalNamespace.Name, decl.Name));
                ctx = ctx.Namespace ?? ctx;
            }
            else
            {
                names.Push(decl.Name);
            }

            if (decl is Variable && !(decl.Namespace is Class))
                names.Push(decl.TranslationUnit.FileNameWithoutExtension);

            while (!(ctx is TranslationUnit))
            {
                if (ctx is ClassTemplateSpecialization parentSpecialization)
                    ctx = parentSpecialization.TemplatedDecl.TemplatedDecl;

                ctx = ctx.Namespace;
                AddContextName(names, ctx);
            }

            if (PrintModuleOutputNamespace)
            {
                var unit = ctx.TranslationUnit;
                if (!unit.IsSystemHeader && unit.IsValid &&
                    !string.IsNullOrWhiteSpace(unit.Module.OutputNamespace))
                    names.Push(unit.Module.OutputNamespace);
            }

            return QualifiedType(string.Join(".", names));
        }

        public override TypePrinterResult VisitParameters(IEnumerable<Parameter> @params,
            bool hasNames)
        {
            @params = @params.Where(
                p => ContextKind == TypePrinterContextKind.Native ||
                    (p.Kind != ParameterKind.IndirectReturnType && !p.Ignore));

            return base.VisitParameters(@params, hasNames);
        }

        public override TypePrinterResult VisitParameter(Parameter param, bool hasName)
        {
            var typeBuilder = new StringBuilder();
            if (param.Type.Desugar().IsPrimitiveType(PrimitiveType.Bool)
                && MarshalKind == MarshalKind.GenericDelegate)
                typeBuilder.Append("[MarshalAs(UnmanagedType.I1)] ");
            var printedType = param.Type.Visit(this, param.QualifiedType.Qualifiers);
            typeBuilder.Append(printedType);
            var type = typeBuilder.ToString();

            if (ContextKind == TypePrinterContextKind.Native)
                return $"{type} {param.Name}";

            var extension = param.Kind == ParameterKind.Extension ? "this " : string.Empty;
            var usage = GetParameterUsage(param.Usage);

            if (param.DefaultArgument == null || !Options.GenerateDefaultValuesForArguments)
                return $"{extension}{usage}{type} {param.Name}";

            var defaultValue = expressionPrinter.VisitParameter(param);
            return $"{extension}{usage}{type} {param.Name} = {defaultValue}";
        }

        public override TypePrinterResult VisitDelegate(FunctionType function)
        {
            PushMarshalKind(MarshalKind.GenericDelegate);
            var functionRetType = function.ReturnType.Visit(this);
            var @params = VisitParameters(function.Parameters, hasNames: true);
            PopMarshalKind();

            return $"delegate {functionRetType} {{0}}({@params})";
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

        public override TypePrinterResult VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            return type.Description;
        }

        private static string GetParameterUsage(ParameterUsage usage)
        {
            switch (usage)
            {
                case ParameterUsage.Out:
                    return "out ";
                case ParameterUsage.InOut:
                    return "ref ";
                default:
                    return string.Empty;
            }
        }

        public override TypePrinterResult VisitFieldDecl(Field field)
        {
            PushMarshalKind(MarshalKind.NativeField);
            var fieldTypePrinted = field.QualifiedType.Visit(this);
            PopMarshalKind();

            var returnTypePrinter = new TypePrinterResult();
            returnTypePrinter.NameSuffix.Append(fieldTypePrinted.NameSuffix);

            returnTypePrinter.Type = $"{fieldTypePrinted.Type} {field.Name}";

            return returnTypePrinter;
        }

        public TypePrinterResult PrintNative(Declaration declaration)
        {
            PushContext(TypePrinterContextKind.Native);
            var typePrinterResult = declaration.Visit(this);
            PopContext();
            return typePrinterResult;
        }

        public TypePrinterResult PrintNative(Type type)
        {
            PushContext(TypePrinterContextKind.Native);
            var typePrinterResult = type.Visit(this);
            PopContext();
            return typePrinterResult;
        }

        public TypePrinterResult PrintNative(QualifiedType type)
        {
            PushContext(TypePrinterContextKind.Native);
            var typePrinterResult = type.Visit(this);
            PopContext();
            return typePrinterResult;
        }

        public static Type GetSignedType(uint width)
        {
            switch (width)
            {
                case 8:
                    return new CILType(typeof(sbyte));
                case 16:
                    return new CILType(typeof(short));
                case 32:
                    return new CILType(typeof(int));
                case 64:
                    return new CILType(typeof(long));
                default:
                    throw new System.NotSupportedException();
            }
        }

        public static Type GetUnsignedType(uint width)
        {
            switch (width)
            {
                case 8:
                    return new CILType(typeof(byte));
                case 16:
                    return new CILType(typeof(ushort));
                case 32:
                    return new CILType(typeof(uint));
                case 64:
                    return new CILType(typeof(ulong));
                default:
                    throw new System.NotSupportedException();
            }
        }

        private static bool IsValid(TemplateArgument a)
        {
            if (a.Type.Type == null)
                return true;
            var templateParam = a.Type.Type.Desugar() as TemplateParameterType;
            // HACK: TemplateParameterType.Parameter is null in some corner cases, see the parser
            return templateParam == null || templateParam.Parameter != null;
        }

        private void AddContextName(Stack<string> names, Declaration ctx)
        {
            var isInlineNamespace = ctx is Namespace && ((Namespace)ctx).IsInline;
            if (string.IsNullOrWhiteSpace(ctx.Name) || isInlineNamespace)
                return;

            if (ContextKind != TypePrinterContextKind.Managed)
            {
                names.Push(ctx.Name);
                return;
            }

            string name = null;
            var @class = ctx as Class;
            if (@class != null)
            {
                if (@class.IsTemplate)
                    name = $@"{ctx.Name}<{string.Join(", ",
                        @class.TemplateParameters.Select(p => p.Name))}>";
                else
                {
                    var specialization = @class as ClassTemplateSpecialization;
                    if (specialization != null)
                    {
                        name = $@"{ctx.Name}<{string.Join(", ",
                            specialization.Arguments.Select(VisitTemplateArgument))}>";
                    }
                }
            }
            names.Push(name ?? ctx.Name);
        }

        private CSharpExpressionPrinter expressionPrinter => new CSharpExpressionPrinter(this);
    }
}
