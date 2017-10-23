﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;
using ParserTargetInfo = CppSharp.Parser.ParserTargetInfo;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public class CSharpTypePrinter : TypePrinter
    {
        public const string IntPtrType = "global::System.IntPtr";

        public BindingContext Context { get; set; }

        public DriverOptions Options => Context.Options;
        public TypeMapDatabase TypeMapDatabase => Context.TypeMaps;

        public CSharpTypePrinter(BindingContext context)
        {
            Context = context;
        }

        public override TypePrinterResult VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (tag.Declaration == null)
                return string.Empty;

            TypeMap typeMap;
            if (TypeMapDatabase.FindTypeMap(tag.Declaration, out typeMap))
            {
                typeMap.Type = tag;

                var typePrinterContext = new TypePrinterContext()
                {
                    Kind = Kind,
                    MarshalKind = MarshalKind,
                    Type = tag
                };

                string type = typeMap.CSharpSignature(typePrinterContext);
                if (!string.IsNullOrEmpty(type))
                {
                    return new TypePrinterResult
                    {
                        Type = type,
                        TypeMap = typeMap
                    };
                }
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
                    return new TypePrinterResult
                    {
                        Type = $"fixed {@enum.BuiltinType}",
                        NameSuffix = $"[{array.Size}]"
                    };
                }

                Class @class;
                if (arrayType.TryGetClass(out @class))
                {
                    return new TypePrinterResult
                    {
                        Type = "fixed byte",
                        NameSuffix = $"[{array.Size * @class.Layout.Size}]"
                    };
                }

                var arrayElemType = array.QualifiedType.Visit(this).ToString();

                // C# does not support fixed arrays of machine pointer type (void* or IntPtr).
                // In that case, replace it by a pointer to an integer type of the same size.
                if (arrayElemType == IntPtrType)
                    arrayElemType = Context.TargetInfo.PointerWidth == 64 ? "long" : "int";

                // Do not write the fixed keyword multiple times for nested array types
                var fixedKeyword = arrayType is ArrayType ? string.Empty : "fixed ";
                return new TypePrinterResult
                {
                    Type = $"{fixedKeyword}{arrayElemType}",
                    NameSuffix = $"[{array.Size}]"
                };
            }

            // const char* and const char[] are the same so we can use a string
            if (array.SizeType == ArrayType.ArraySize.Incomplete &&
                arrayType.IsPrimitiveType(PrimitiveType.Char) &&
                array.QualifiedType.Qualifiers.IsConst)
                return "string";

            if (arrayType.IsPointerToPrimitiveType(PrimitiveType.Char))
            {
                var prefix = ContextKind == TypePrinterContextKind.Managed ? string.Empty :
                    "[MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPStr)] ";
                return $"{prefix}string[]";
            }

            var arraySuffix = array.SizeType != ArrayType.ArraySize.Constant &&
                MarshalKind == MarshalKind.ReturnVariableArray ?
                (ContextKind == TypePrinterContextKind.Managed &&
                 arrayType.IsPrimitiveType() ? "*" : string.Empty) : "[]";
            return $"{arrayType.Visit(this)}{arraySuffix}";
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

        static System.Text.RegularExpressions.Regex ptrSelector = new System.Text.RegularExpressions.Regex(@"[\*&]*$");
        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            var pointee = pointer.Pointee;

            if (Context.Options.GenerateRawCBindings && !(pointee is FunctionType))
            {
                var ptrStr = ptrSelector.Match(pointer.ToNativeString()).Value;
                var d = pointee.Desugar();
                if (ptrStr.Equals("&"))
                    return $"ref {d}";
                
                var res = string.Concat(d, ptrStr);
                return res;
            }


            if (MarshalKind == MarshalKind.NativeField)
            {

                return IntPtrType;
            }

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return string.Format("{0}", function.Visit(this, quals));
            }

            var isManagedContext = ContextKind == TypePrinterContextKind.Managed;

            if (allowStrings && IsConstCharString(pointer))
            {
                if (isManagedContext)
                    return "string";
                if (Parameter == null || Parameter.Name == Helpers.ReturnIdentifier)
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
                bool isRefParam = Parameter != null && (Parameter.IsOut || Parameter.IsInOut);
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
                if (isManagedContext && Parameter != null && (Parameter.IsOut || Parameter.IsInOut)
                    && pointee == finalPointee)
                    return pointer.QualifiedPointee.Visit(this);

                return pointer.QualifiedPointee.Visit(this) + "*";
            }

            Class @class;
            if ((desugared.IsDependent || desugared.TryGetClass(out @class))
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
            if (TypeMapDatabase.FindTypeMap(decl, out typeMap))
            {
                typeMap.Type = typedef;

                var typePrinterContext = new TypePrinterContext
                {
                    Kind = ContextKind,
                    MarshalKind = MarshalKind,
                    Type = typedef
                };

                string type = typeMap.CSharpSignature(typePrinterContext);
                if (!string.IsNullOrEmpty(type))
                {
                    return new TypePrinterResult
                    {
                        Type = type,
                        TypeMap = typeMap
                    };
                }
            }

            FunctionType func = decl.Type as FunctionType;
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
                    return $@"{VisitDeclaration(decl)}<{string.Join(", ",
                        template.Arguments.Select(VisitTemplateArgument))}>";

                if (ContextKind == TypePrinterContextKind.Native)
                    return template.Desugared.Visit(this);

                return decl.Visit(this);
            }

            typeMap.Declaration = decl;
            typeMap.Type = template;

            var typePrinterContext = new TypePrinterContext
            {
                Type = template,
                Kind = ContextKind,
                MarshalKind = MarshalKind
            };

            var type = typeMap.CSharpSignature(typePrinterContext);
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
            return type.Type.FullName;
        }

        public static void GetPrimitiveTypeWidth(PrimitiveType primitive,
            ParserTargetInfo targetInfo, out uint width, out bool signed)
        {
            switch (primitive)
            {
                case PrimitiveType.Char:
                    width = targetInfo?.CharWidth ?? 8;
                    signed = true;
                    break;
                case PrimitiveType.UChar:
                    width = targetInfo?.CharWidth ?? 8;
                    signed = false;
                    break;
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
                case PrimitiveType.Int128:
                    return new TypePrinterResult
                    {
                        Type = "fixed byte",
                        NameSuffix = "[16]"
                    }; // The type is always 128 bits wide
                case PrimitiveType.UInt128:
                    return new TypePrinterResult
                    {
                        Type = "fixed byte",
                        NameSuffix = "[16]"
                    }; // The type is always 128 bits wide
                case PrimitiveType.Half:
                    return new TypePrinterResult
                    {
                        Type = "fixed byte",
                        NameSuffix = $"[{Context.TargetInfo.HalfWidth}]"
                    };
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.LongDouble:
                    return new TypePrinterResult
                    {
                        Type = "fixed byte",
                        NameSuffix = $"[{Context.TargetInfo.LongDoubleWidth}]"
                    };
                case PrimitiveType.IntPtr: return IntPtrType;
                case PrimitiveType.UIntPtr: return "global::System.UIntPtr";
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
                if (Context.Options.GenerateRawCBindings)
                    return $"{VisitDeclaration(@class.OriginalClass ?? @class)}";
                else return $"{VisitDeclaration(@class.OriginalClass ?? @class)}.{Helpers.InternalStruct}";


            var printed = VisitDeclaration(@class).Type;
            if (!@class.IsDependent)
                return printed;
            return $@"{printed}<{string.Join(", ",
                @class.TemplateParameters.Select(p => p.Name))}>";
        }

        public override TypePrinterResult VisitClassTemplateSpecializationDecl(
            ClassTemplateSpecialization specialization)
        {
            if (ContextKind == TypePrinterContextKind.Native)
                return $@"{VisitClassDecl(specialization)}{
                    Helpers.GetSuffixForInternal(specialization)}";
            var args = string.Join(", ", specialization.Arguments.Select(VisitTemplateArgument));
            return $"{VisitClassDecl(specialization)}<{args}>";
        }

        public TypePrinterResult VisitTemplateArgument(TemplateArgument a)
        {
            if (a.Type.Type == null)
                return a.Integral.ToString(CultureInfo.InvariantCulture);
            var type = a.Type.Type.Desugar();
            return type.IsPointerToPrimitiveType() ? IntPtrType : type.Visit(this);
        }

        public override TypePrinterResult VisitParameterDecl(Parameter parameter)
        {
            var paramType = parameter.Type;

            if (parameter.Kind == ParameterKind.IndirectReturnType && !Context.Options.GenerateRawCBindings)
            {
                return IntPtrType;
            }

            if(Context.Options.GenerateRawCBindings)
            {
                if (paramType is TypedefType)
                {
                    var typedef = ((TypedefType)paramType);
                    if (typedef.Declaration.Type is PointerType && ((PointerType)typedef.Declaration.Type).Pointee is FunctionType)
                    {
                        var tDefName = typedef.Declaration.Name;
                        return tDefName;
                        //  return paramType.dec
                    }
                }
                //else if (paramType is PointerType && !(((PointerType)paramType).Pointee is FunctionType) && !((PointerType)paramType).GetFinalPointee().ToString().Equals("void"))
                //{
                //    // return $"ref {((PointerType)paramType).Pointee} {parameter.Name}";
                //    parameter.QualifiedType = new QualifiedType(((PointerType)paramType).Pointee);
                   

                //        return $"ref {parameter.Type}";

                //}
            }
          

            Parameter = parameter;
            var pType = parameter.Type;
            var ret = pType.Visit(this);
            Parameter = null;

            return ret;
        }

        string GetName(Declaration decl)
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
                var isInlineNamespace = ctx is Namespace && ((Namespace)ctx).IsInline;
                if (!string.IsNullOrWhiteSpace(ctx.Name) && !isInlineNamespace)
                    names.Push(ctx.Name);

                ctx = ctx.Namespace;
            }

            var unit = ctx.TranslationUnit;
            if (!unit.IsSystemHeader && unit.IsValid &&
                !string.IsNullOrWhiteSpace(unit.Module.OutputNamespace))
                names.Push(unit.Module.OutputNamespace);

            return string.Join(".", names);
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
            var cSharpSourcesDummy = new CSharpSources(Context, new List<TranslationUnit>());
            var safeIdentifier = cSharpSourcesDummy.SafeIdentifier(field.Name);

            if (safeIdentifier.All(c => c.Equals('_')))
            {
                safeIdentifier = cSharpSourcesDummy.SafeIdentifier(field.Name);
            }

            PushMarshalKind(MarshalKind.NativeField);
            var fieldTypePrinted = field.QualifiedType.Visit(this);
            PopMarshalKind();

            var returnTypePrinter = new TypePrinterResult();
            if (!string.IsNullOrWhiteSpace(fieldTypePrinted.NameSuffix))
                returnTypePrinter.NameSuffix = fieldTypePrinted.NameSuffix;

            returnTypePrinter.Type = $"{fieldTypePrinted.Type} {safeIdentifier}";

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

        private static bool IsValid(TemplateArgument a)
        {
            if (a.Type.Type == null)
                return true;
            var templateParam = a.Type.Type.Desugar() as TemplateParameterType;
            // HACK: TemplateParameterType.Parameter is null in some corner cases, see the parser
            return templateParam == null || templateParam.Parameter != null;
        }

        private CSharpExpressionPrinter expressionPrinter => new CSharpExpressionPrinter(this);
    }
}
