using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    /// <summary>
    /// Validates operators for C# (if they can be overloaded).
    /// </summary>
    public class ValidateOperatorsPass : TranslationUnitPass
    {
        public ValidateOperatorsPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassMethods | VisitFlags.ClassProperties |
            VisitFlags.ClassTemplateSpecializations);

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) ||
                !method.IsOperator || !method.IsGenerated)
                return false;

            if (!IsValidOperatorOverload(method) || method.IsPure)
            {
                Diagnostics.Debug("Invalid operator overload {0}::{1}",
                    method.Namespace.Name, method.OperatorKind);
                method.ExplicitlyIgnore();
            }

            return true;
        }

        private bool IsValidOperatorOverload(Method @operator)
        {
            // These follow the order described in MSDN (Overloadable Operators).

            switch (@operator.OperatorKind)
            {
                // These unary operators can be overloaded
                case CXXOperatorKind.Plus:
                case CXXOperatorKind.Minus:
                case CXXOperatorKind.Exclaim:
                case CXXOperatorKind.Tilde:

                // These binary operators can be overloaded
                case CXXOperatorKind.Slash:
                case CXXOperatorKind.Percent:
                case CXXOperatorKind.Amp:
                case CXXOperatorKind.Pipe:
                case CXXOperatorKind.Caret:

                // The array indexing operator can be overloaded
                case CXXOperatorKind.Subscript:

                // The conversion operators can be overloaded
                case CXXOperatorKind.Conversion:
                case CXXOperatorKind.ExplicitConversion:
                    return true;

                // The comparison operators can be overloaded if their return type is bool
                case CXXOperatorKind.EqualEqual:
                case CXXOperatorKind.ExclaimEqual:
                case CXXOperatorKind.Less:
                case CXXOperatorKind.Greater:
                case CXXOperatorKind.LessEqual:
                case CXXOperatorKind.GreaterEqual:
                    return @operator.ReturnType.Type.IsPrimitiveType(PrimitiveType.Bool);

                // Only prefix operators can be overloaded
                case CXXOperatorKind.PlusPlus:
                case CXXOperatorKind.MinusMinus:
                    Class @class;
                    var returnType = @operator.OriginalReturnType.Type.Desugar();
                    returnType = (returnType.GetFinalPointee() ?? returnType).Desugar();
                    return returnType.TryGetClass(out @class) &&
                        @class.GetNonIgnoredRootBase() ==
                            ((Class)@operator.Namespace).GetNonIgnoredRootBase() &&
                        @operator.Parameters.Count == 0;

                // Bitwise shift operators can only be overloaded if the second parameter is int
                case CXXOperatorKind.LessLess:
                case CXXOperatorKind.GreaterGreater:
                    {
                        Parameter parameter = @operator.Parameters.Last();
                        Type type = parameter.Type.Desugar();
                        var kind = Options.GeneratorKind;
                        switch (kind)
                        {
                            case var _ when ReferenceEquals(kind, GeneratorKind.CLI):
                                return type.IsPrimitiveType(PrimitiveType.Int);
                            case var _ when ReferenceEquals(kind, GeneratorKind.CSharp):
                                Types.TypeMap typeMap;
                                if (Context.TypeMaps.FindTypeMap(type, GeneratorKind.CSharp, out typeMap))
                                {
                                    var mappedTo = typeMap.SignatureType(
                                        new TypePrinterContext
                                        {
                                            Parameter = parameter,
                                            Type = type
                                        });
                                    var cilType = mappedTo as CILType;
                                    if (cilType?.Type == typeof(int))
                                        return true;
                                }
                                break;
                        }
                        return false;
                    }

                // No parameters means the dereference operator - cannot be overloaded
                case CXXOperatorKind.Star:
                    return @operator.Parameters.Count > 0;

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
                default:
                    return false;
            }
        }
    }
}
