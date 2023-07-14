using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;
using CppSharp.Generators.CSharp;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CLI
{
    public class CLITypePrinter : CppTypePrinter
    {
        public CLITypePrinter(BindingContext context) : base(context)
        {
        }

        public override TypePrinterResult VisitTagType(TagType tag, TypeQualifiers quals)
        {
            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(tag, out typeMap))
            {
                var typePrinterContext = new TypePrinterContext { Type = tag };
                return typeMap.CLISignatureType(typePrinterContext).ToString();
            }

            Declaration decl = tag.Declaration;
            if (decl == null)
                return string.Empty;

            return VisitDeclaration(decl, quals);
        }

        public override TypePrinterResult VisitArrayType(ArrayType array,
            TypeQualifiers quals)
        {
            // const char* and const char[] are the same so we can use a string
            if (Context.Options.MarshalConstCharArrayAsString &&
                array.SizeType == ArrayType.ArraySize.Incomplete &&
                array.Type.Desugar().IsPrimitiveType(PrimitiveType.Char) &&
                array.QualifiedType.Qualifiers.IsConst)
                return VisitCILType(new CILType(typeof(string)), quals);

            return $"cli::array<{array.Type.Visit(this)}>^";
        }

        public override TypePrinterResult VisitFunctionType(FunctionType function,
            TypeQualifiers quals)
        {
            var arguments = function.Parameters;
            var returnType = function.ReturnType;
            var args = string.Empty;

            if (arguments.Count > 0)
                args = VisitParameters(function.Parameters, hasNames: false).ToString();

            if (returnType.Type.IsPrimitiveType(PrimitiveType.Void))
            {
                if (!string.IsNullOrEmpty(args))
                    args = $"<{args}>";
                return $"System::Action{args}";
            }

            if (!string.IsNullOrEmpty(args))
                args = $", {args}";

            return $"System::Func<{returnType.Visit(this)}{args}>";
        }

        public override TypePrinterResult VisitParameter(Parameter param,
            bool hasName = true)
        {
            Parameter = param;
            var type = param.QualifiedType.Visit(this);
            Parameter = null;

            var str = string.Empty;
            if (param.Usage == ParameterUsage.Out)
                str += "[::System::Runtime::InteropServices::Out] ";
            else if (param.Usage == ParameterUsage.InOut)
                str += "[::System::Runtime::InteropServices::In, ::System::Runtime::InteropServices::Out] ";

            str += type;

            if (param.Usage == ParameterUsage.Out ||
               param.Usage == ParameterUsage.InOut)
                str += "%";

            if (hasName && !string.IsNullOrEmpty(param.Name))
                str += " " + param.Name;

            return str;
        }

        public override TypePrinterResult VisitDelegate(FunctionType function)
        {
            return string.Format("delegate {0} {{0}}({1})",
                function.ReturnType.Visit(this),
                VisitParameters(function.Parameters, hasNames: true));
        }

        public override TypePrinterResult VisitPointerType(PointerType pointer,
            TypeQualifiers quals)
        {
            TypeMap typeMap;
            if (Context.TypeMaps.FindTypeMap(pointer.Desugar(), out typeMap))
            {
                var typePrinterContext = new TypePrinterContext
                {
                    Kind = ContextKind,
                    MarshalKind = MarshalKind,
                    Type = pointer
                };

                return typeMap.CLISignatureType(typePrinterContext).Visit(this);
            }

            var pointee = pointer.Pointee.Desugar();

            if (pointee is FunctionType)
            {
                var function = pointee as FunctionType;
                return $"{function.Visit(this, quals)}^";
            }

            // From http://msdn.microsoft.com/en-us/library/y31yhkeb.aspx
            // Any of the following types may be a pointer type:
            // * sbyte, byte, short, ushort, int, uint, long, ulong, char, float, double, decimal, or bool.
            // * Any enum type.
            // * Any pointer type.
            // * Any user-defined struct type that contains fields of unmanaged types only.
            var finalPointee = pointer.GetFinalPointee();
            if (finalPointee.IsPrimitiveType())
            {
                // Skip one indirection if passed by reference
                bool isRefParam = Parameter != null && (Parameter.IsOut || Parameter.IsInOut);
                if (isRefParam)
                    return pointer.QualifiedPointee.Visit(this);

                if (pointee.IsPrimitiveType(PrimitiveType.Void))
                    return "::System::IntPtr";

                var result = pointer.QualifiedPointee.Visit(this).ToString();
                return !isRefParam && result == "::System::IntPtr" ? "void**" :
                    result + (pointer.IsReference ? "" : "*");
            }

            Enumeration @enum;
            if (pointee.TryGetEnum(out @enum))
            {
                var typeName = VisitDeclaration(@enum, quals);

                // Skip one indirection if passed by reference
                if (Parameter != null && (Parameter.Type.IsReference()
                    || ((Parameter.IsOut || Parameter.IsInOut)
                    && pointee == finalPointee)))
                    return typeName;

                return $"{typeName}*";
            }

            return pointer.QualifiedPointee.Visit(this);
        }

        public override TypePrinterResult VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            return VisitPrimitiveType(primitive);
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool: return "bool";
                case PrimitiveType.Void: return "void";
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar: return "::System::Char";
                case PrimitiveType.Char: return Options.MarshalCharAsManagedChar ? "::System::Char" : "char";
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
                case PrimitiveType.Int128: return "__int128";
                case PrimitiveType.UInt128: return "__uint128";
                case PrimitiveType.Half: return "__fp16";
                case PrimitiveType.Float: return "float";
                case PrimitiveType.Double: return "double";
                case PrimitiveType.LongDouble: return "long double";
                case PrimitiveType.IntPtr: return "IntPtr";
                case PrimitiveType.UIntPtr: return "UIntPtr";
                case PrimitiveType.Null: return "void*";
                case PrimitiveType.String: return "::System::String";
            }

            throw new NotSupportedException();
        }

        public override TypePrinterResult VisitTypedefType(TypedefType typedef,
            TypeQualifiers quals)
        {
            var decl = typedef.Declaration;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(decl.Type, out typeMap))
            {
                typeMap.Type = typedef;
                var typePrinterContext = new TypePrinterContext { Type = typedef };
                return typeMap.CLISignatureType(typePrinterContext).ToString();
            }

            FunctionType func;
            if (decl.Type.IsPointerTo(out func))
            {
                // TODO: Use SafeIdentifier()
                return $"{VisitDeclaration(decl)}^";
            }

            return decl.Type.Visit(this);
        }

        public override TypePrinterResult VisitTemplateSpecializationType(
            TemplateSpecializationType template, TypeQualifiers quals)
        {
            var decl = template.Template.TemplatedDecl;
            if (decl == null)
                return string.Empty;

            TypeMap typeMap = null;
            if (TypeMapDatabase.FindTypeMap(template, out typeMap) && !typeMap.IsIgnored)
            {
                var typePrinterContext = new TypePrinterContext { Type = template };
                return typeMap.CLISignatureType(typePrinterContext).ToString();
            }

            return decl.Name;
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
            return param.Replacement.Visit(this);
        }

        public override TypePrinterResult VisitInjectedClassNameType(
            InjectedClassNameType injected, TypeQualifiers quals)
        {
            return string.Empty;
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

        public override TypePrinterResult VisitCILType(CILType type, TypeQualifiers quals)
        {
            var result = type.Type.FullName.Replace(".", "::");
            if (!type.Type.IsValueType)
                result += "^";

            return $"::{result}";
        }

        public override TypePrinterResult VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals)
        {
            return type.Description;
        }

        public override TypePrinterResult VisitDeclaration(Declaration decl)
        {
            var names = new List<string>();

            string rootNamespace = null;
            if (!string.IsNullOrEmpty(decl.TranslationUnit.Module.OutputNamespace))
                names.Add(rootNamespace = decl.TranslationUnit.Module.OutputNamespace);

            if (!string.IsNullOrEmpty(decl.Namespace.QualifiedName))
            {
                names.Add(decl.Namespace.QualifiedName);
                if (string.IsNullOrEmpty(rootNamespace))
                    rootNamespace = decl.Namespace.QualifiedName;
            }

            names.Add(decl.Visit(this).ToString());

            var result = string.Join("::", names);
            var translationUnit = decl.Namespace as TranslationUnit;
            if (translationUnit != null && translationUnit.HasFunctions &&
                rootNamespace == translationUnit.FileNameWithoutExtension)
                return "::" + result;
            return result;
        }

        public override TypePrinterResult VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            return $"{@class.Name}{(@class.IsRefType ? "^" : string.Empty)}";
        }

        public override TypePrinterResult VisitClassTemplateDecl(ClassTemplate template)
        {
            return template.Name;
        }

        public override TypePrinterResult VisitTypedefDecl(TypedefDecl typedef)
        {
            return typedef.Name;
        }

        public override TypePrinterResult VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return typeAlias.Name;
        }

        public override TypePrinterResult VisitEnumDecl(Enumeration @enum)
        {
            return @enum.Name;
        }

        public override TypePrinterResult VisitEnumItemDecl(Enumeration.Item item)
        {
            return $"{VisitEnumDecl((Enumeration)item.Namespace)}::{VisitDeclaration(item)}";
        }

        public override string ToString(Type type)
        {
            return type.Visit(this).ToString();
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
    }
}
