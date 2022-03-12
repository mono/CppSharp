using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators.C;

namespace CppSharp.Generators.TS
{
    public class TSTypePrinter : CppTypePrinter
    {
        public override string NamespaceSeparator => ".";

        public override bool HasGlobalNamespacePrefix => false;

        public override bool PrefixSpecialFunctions => true;

        public TSTypePrinter(BindingContext context) : base(context)
        {
            PrintTypeModifiers = false;
        }

        public override string GlobalNamespace(Declaration declaration)
        {
            return declaration.TranslationUnit?.Module?.LibraryName;
        }

        public override TypePrinterResult GetDeclName(Declaration declaration, TypePrintScopeKind scope)
        {
            var result = base.GetDeclName(declaration, scope);
            result.Type = result.Type.Replace("::", NamespaceSeparator);
            return result;
        }

        public override TypePrinterResult VisitArrayType(ArrayType array, TypeQualifiers quals)
        {
            return $"{array.Type.Visit(this)}[]";
        }

        public override TypePrinterResult VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals)
        {
            return VisitPrimitiveType(builtin.Type);
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive, TypeQualifiers quals)
        {
            return VisitPrimitiveType(primitive);
        }

        public override TypePrinterResult VisitTypedefType(TypedefType typedef, TypeQualifiers quals)
        {
            if (typedef.Declaration.QualifiedOriginalName == "std::nullptr_t")
                return VisitPrimitiveType(PrimitiveType.Null);

            if (typedef.Declaration.Type.IsPrimitiveType())
                return typedef.Declaration.Type.Visit(this);

            return base.VisitTypedefType(typedef, quals);
        }

        public override TypePrinterResult VisitPrimitiveType(PrimitiveType primitive)
        {
            switch (primitive)
            {
                case PrimitiveType.Bool:
                    return "boolean";
                case PrimitiveType.Void:
                    return "void";
                case PrimitiveType.Char16:
                case PrimitiveType.Char32:
                case PrimitiveType.WideChar:
                case PrimitiveType.Char:
                case PrimitiveType.SChar:
                case PrimitiveType.UChar:
                case PrimitiveType.Short:
                case PrimitiveType.UShort:
                case PrimitiveType.Int:
                case PrimitiveType.UInt:
                case PrimitiveType.Long:
                case PrimitiveType.ULong:
                case PrimitiveType.LongLong:
                    return "number";
                case PrimitiveType.ULongLong:
                    return "bigint";
                case PrimitiveType.Half:
                case PrimitiveType.Float:
                case PrimitiveType.Double:
                    return "number";
                case PrimitiveType.LongDouble:
                case PrimitiveType.Float128:
                case PrimitiveType.Int128:
                case PrimitiveType.UInt128:
                case PrimitiveType.IntPtr:
                case PrimitiveType.UIntPtr:
                case PrimitiveType.Null:
                    return "null";
                case PrimitiveType.String:
                    return "string";
                case PrimitiveType.Decimal:
                    return "number";
            }

            throw new NotSupportedException();
        }

        public override TypePrinterResult VisitPointerType(PointerType pointer, TypeQualifiers quals)
        {
            if (pointer.IsConstCharString())
                return VisitPrimitiveType(PrimitiveType.String);

            return base.VisitPointerType(pointer, quals);
        }

        public override TypePrinterResult VisitTagType(TagType tag, TypeQualifiers quals)
        {
            if (FindTypeMap(tag, out var result))
                return result;

            return tag.Declaration.Visit(this);
        }

        public override TypePrinterResult VisitTemplateSpecializationType(TemplateSpecializationType template,
            TypeQualifiers quals)
        {
            if (!template.Desugared.Type.TryGetClass(out var @class))
                return string.Empty;

            var args = template.Arguments.Select(a => a.Type.Visit(this));
            return $"{@class.Visit(this)}<{string.Join(", ", args)}>";
        }

        public override TypePrinterResult VisitParameter(Parameter param, bool hasName = true)
        {
            var oldParam = Parameter;
            Parameter = param;
            var result = param.QualifiedType.Visit(this);
            result.Kind = GeneratorKind.TypeScript;
            Parameter = oldParam;

            var printName = hasName && !string.IsNullOrEmpty(param.Name);
            if (!printName)
                return result;

            result.Name = param.Name;

            /*
                        if (param.DefaultArgument != null && Options.GenerateDefaultValuesForArguments)
                        {
                            try
                            {
                                var expressionPrinter = new CSharpExpressionPrinter(this);
                                var defaultValue = expressionPrinter.VisitParameter(param);
                                return $"{result} = {defaultValue}";
                            }
                            catch (Exception)
                            {
                                var function = param.Namespace as Function;
                                Diagnostics.Warning($"Error printing default argument expression: " +
                                                    $"{function.QualifiedOriginalName}({param.OriginalName})");
                            }
                        }
            */

            return $"{result}";
        }
    }
}
