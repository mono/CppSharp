using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;
using CppSharp.Types;

namespace CppSharp.AST
{
    public static class ASTUtils
    {
        public static bool CheckIgnoreFunction(Function function)
        {
            if (!function.IsGenerated) return true;

            if (function is Method)
                return CheckIgnoreMethod(function as Method);

            return false;
        }

        public static bool CheckIgnoreMethod(Method method)
        {
            if (!method.IsGenerated) return true;

            if (method.IsDependent && UsesAdditionalTypeParam(method))
                return true;

            var isEmptyCtor = method.IsConstructor && method.Parameters.Count == 0;

            var @class = method.Namespace as Class;
            if (@class != null && @class.IsValueType && isEmptyCtor)
                return true;

            if (method.IsDestructor)
                return true;

            if (method.OperatorKind == CXXOperatorKind.Equal)
                return true;

            if (method.Access == AccessSpecifier.Private && !method.IsOverride && !method.IsExplicitlyGenerated)
                return true;

            // Ignore copy constructor if a base class don't has or has a private copy constructor
            if (method.IsCopyConstructor)
            {
                var baseClass = @class;
                while (baseClass != null && baseClass.HasBaseClass)
                {
                    baseClass = baseClass.BaseClass;
                    if (!baseClass.IsInterface)
                    {
                        var copyConstructor = baseClass.Methods.FirstOrDefault(m => m.IsCopyConstructor);
                        if (copyConstructor == null ||
                            copyConstructor.Access == AccessSpecifier.Private ||
                            (!copyConstructor.IsDeclared &&
                             !copyConstructor.TranslationUnit.IsSystemHeader))
                            return true;
                    }
                }
            }

            return false;
        }

        public static bool CheckIgnoreField(Field field, bool useInternals = false)
        {
            if (field.Access == AccessSpecifier.Private && !useInternals)
                return true;

            if (field.Class.IsValueType && field.IsDeclared)
                return false;

            return !field.IsGenerated && (!useInternals || !field.IsInternal);
        }

        public static bool CheckIgnoreProperty(Property prop)
        {
            if (prop.Access == AccessSpecifier.Private)
                return true;

            if (prop.Field != null && prop.Field.Class.IsValueType && prop.IsDeclared)
                return false;

            return !prop.IsGenerated;
        }

        public static void CheckTypeForSpecialization(Type type, Declaration container,
            Action<ClassTemplateSpecialization> addSpecialization,
            ITypeMapDatabase typeMaps, bool internalOnly = false)
        {
            type = type.Desugar();
            type = (type.GetFinalPointee() ?? type).Desugar();
            ClassTemplateSpecialization specialization;
            type.TryGetDeclaration(out specialization);
            if (specialization == null)
                return;

            TypeMap typeMap;
            typeMaps.FindTypeMap(specialization, out typeMap);

            if ((!internalOnly && (((specialization.Ignore ||
                 specialization.TemplatedDecl.TemplatedClass.Ignore) && typeMap == null) ||
                 specialization.Arguments.Any(a => UnsupportedTemplateArgument(
                     specialization, a, typeMaps)))) ||
                specialization.IsIncomplete ||
                (!internalOnly && specialization.TemplatedDecl.TemplatedClass.IsIncomplete) ||
                specialization is ClassTemplatePartialSpecialization ||
                container.Namespace == specialization)
                return;

            while (container.Namespace != null)
            {
                if (container.Namespace == specialization)
                    return;
                container = container.Namespace;
            }

            if (!internalOnly && typeMaps.FindTypeMap(specialization, out typeMap))
            {
                var typePrinterContext = new TypePrinterContext { Type = type };
                var mappedTo = typeMap.CSharpSignatureType(typePrinterContext);
                mappedTo = mappedTo.Desugar();
                mappedTo = (mappedTo.GetFinalPointee() ?? mappedTo);
                if (mappedTo.IsPrimitiveType() || mappedTo.IsPointerToPrimitiveType() || mappedTo.IsEnum())
                    return;
            }

            addSpecialization(specialization);
        }

        public static bool IsTypeExternal(Module module, Type type)
        {
            Declaration declaration;
            if (!(type.GetFinalPointee() ?? type).TryGetDeclaration(out declaration))
                return false;

            declaration = declaration.CompleteDeclaration ?? declaration;
            if (declaration.Namespace == null || declaration.TranslationUnit.Module == null)
                return false;

            return declaration.TranslationUnit.Module.Dependencies.Contains(module);
        }

        private static bool UsesAdditionalTypeParam(Method method)
        {
            var specialization = method.Namespace as ClassTemplateSpecialization;
            Class template;
            if (specialization != null)
                template = specialization.TemplatedDecl.TemplatedClass;
            else
            {
                template = (Class) method.Namespace;
                if (!template.IsDependent)
                    return false;
            }
            var typeParams = template.TemplateParameters.Select(t => t.Name).ToList();
            return method.Parameters.Any(p =>
            {
                if (!p.IsDependent)
                    return false;
                var desugared = p.Type.Desugar();
                var finalType = (desugared.GetFinalPointee() ?? desugared).Desugar()
                    as TemplateParameterType;
                return finalType != null && !typeParams.Contains(finalType.Parameter.Name);
            });
        }

        private static bool UnsupportedTemplateArgument(
            ClassTemplateSpecialization specialization, TemplateArgument a, ITypeMapDatabase typeMaps)
        {
            if (a.Type.Type == null ||
                IsTypeExternal(specialization.TranslationUnit.Module, a.Type.Type))
                return true;

            var typeIgnoreChecker = new TypeIgnoreChecker(typeMaps);
            a.Type.Type.Visit(typeIgnoreChecker);
            return typeIgnoreChecker.IsIgnored;
        }
    }

    public static class Operators
    {
        public static CXXOperatorArity ClassifyOperator(Function function)
        {
            if (function.Parameters.Count == 1)
                return CXXOperatorArity.Unary;

            return CXXOperatorArity.Binary;
        }

        public static string GetOperatorOverloadPair(CXXOperatorKind kind)
        {
            switch (kind)
            {
                case CXXOperatorKind.EqualEqual:
                    return "!=";
                case CXXOperatorKind.ExclaimEqual:
                    return "==";

                case CXXOperatorKind.Less:
                    return ">";
                case CXXOperatorKind.Greater:
                    return "<";

                case CXXOperatorKind.LessEqual:
                    return ">=";
                case CXXOperatorKind.GreaterEqual:
                    return "<=";

                default:
                    throw new NotSupportedException();
            }
        }

        public static bool IsBuiltinOperator(CXXOperatorKind kind)
        {
            bool isBuiltin;
            GetOperatorIdentifier(kind, out isBuiltin);

            return isBuiltin;
        }

        public static string GetOperatorIdentifier(CXXOperatorKind kind)
        {
            bool isBuiltin;
            return GetOperatorIdentifier(kind, out isBuiltin);
        }

        public static string GetOperatorIdentifier(CXXOperatorKind kind,
            out bool isBuiltin)
        {
            isBuiltin = true;

            // These follow the order described in MSDN (Overloadable Operators).
            switch (kind)
            {
                // These unary operators can be overloaded
                case CXXOperatorKind.Plus: return "operator +";
                case CXXOperatorKind.Minus: return "operator -";
                case CXXOperatorKind.Exclaim: return "operator !";
                case CXXOperatorKind.Tilde: return "operator ~";
                case CXXOperatorKind.PlusPlus: return "operator ++";
                case CXXOperatorKind.MinusMinus: return "operator --";

                // These binary operators can be overloaded
                case CXXOperatorKind.Star: return "operator *";
                case CXXOperatorKind.Slash: return "operator /";
                case CXXOperatorKind.Percent: return "operator %";
                case CXXOperatorKind.Amp: return "operator &";
                case CXXOperatorKind.Pipe: return "operator |";
                case CXXOperatorKind.Caret: return "operator ^";
                case CXXOperatorKind.LessLess: return "operator <<";
                case CXXOperatorKind.GreaterGreater: return "operator >>";

                // The comparison operators can be overloaded
                case CXXOperatorKind.EqualEqual: return "operator ==";
                case CXXOperatorKind.ExclaimEqual: return "operator !=";
                case CXXOperatorKind.Less: return "operator <";
                case CXXOperatorKind.Greater: return "operator >";
                case CXXOperatorKind.LessEqual: return "operator <=";
                case CXXOperatorKind.GreaterEqual: return "operator >=";

                // Assignment operators cannot be overloaded
                case CXXOperatorKind.PlusEqual:
                case CXXOperatorKind.MinusEqual:
                case CXXOperatorKind.StarEqual:
                case CXXOperatorKind.SlashEqual:
                case CXXOperatorKind.PercentEqual:
                case CXXOperatorKind.AmpEqual:
                case CXXOperatorKind.PipeEqual:
                case CXXOperatorKind.CaretEqual:
                case CXXOperatorKind.LessLessEqual:
                case CXXOperatorKind.GreaterGreaterEqual:

                // The array indexing operator cannot be overloaded
                case CXXOperatorKind.Subscript:

                // The conditional logical operators cannot be overloaded
                case CXXOperatorKind.AmpAmp:
                case CXXOperatorKind.PipePipe:

                // These operators cannot be overloaded.
                case CXXOperatorKind.Equal:
                case CXXOperatorKind.Comma:
                case CXXOperatorKind.ArrowStar:
                case CXXOperatorKind.Arrow:
                case CXXOperatorKind.Call:
                case CXXOperatorKind.Conditional:
                case CXXOperatorKind.Coawait:
                case CXXOperatorKind.New:
                case CXXOperatorKind.Delete:
                case CXXOperatorKind.Array_New:
                case CXXOperatorKind.Array_Delete:
                    isBuiltin = false;
                    return "Operator" + kind.ToString();

                case CXXOperatorKind.Conversion:
                    return "implicit operator";
                case CXXOperatorKind.ExplicitConversion:
                    return "explicit operator";
            }

            throw new NotSupportedException();
        }
    }
}
