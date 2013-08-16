using System.Linq;
using CppSharp.AST;
using CppSharp.Generators.CSharp;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks for missing operator overloads required by C#.
    /// </summary>
    class CheckOperatorsOverloadsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            if (!VisitDeclaration(@class))
                return false;

            if (AlreadyVisited(@class))
                return false;

            // Check for C++ operators that cannot be represented in C#.
            CheckInvalidOperators(@class);

            // The comparison operators, if overloaded, must be overloaded in pairs;
            // that is, if == is overloaded, != must also be overloaded. The reverse
            // is also true, and similar for < and >, and for <= and >=.

            HandleMissingOperatorOverloadPair(@class, CXXOperatorKind.EqualEqual,
                                              CXXOperatorKind.ExclaimEqual);

            HandleMissingOperatorOverloadPair(@class, CXXOperatorKind.Less,
                                              CXXOperatorKind.Greater);

            HandleMissingOperatorOverloadPair(@class, CXXOperatorKind.LessEqual,
                                              CXXOperatorKind.GreaterEqual);

            return false;
        }

        private void CheckInvalidOperators(Class @class)
        {
            foreach (var @operator in @class.Operators)
            {
                if (!IsValidOperatorOverload(@operator))
                {
                    Driver.Diagnostics.EmitError(DiagnosticId.InvalidOperatorOverload,
                        "Invalid operator overload {0}::{1}",
                        @class.OriginalName, @operator.OperatorKind);
                    @operator.ExplicityIgnored = true;
                    continue;
                }

                // Handle missing operator parameters
                if (@operator.IsStatic)
                    @operator.Parameters = @operator.Parameters.Skip(1).ToList();

                var type = new PointerType()
                {
                    QualifiedPointee = new QualifiedType(new TagType(@class)),
                    Modifier = PointerType.TypeModifier.Pointer
                };

                @operator.Parameters.Insert(0, new Parameter
                {
                    Name = Helpers.GeneratedIdentifier("op"),
                    QualifiedType = new QualifiedType(type),
                    Kind = ParameterKind.OperatorParameter
                });
            }
        }

        static void HandleMissingOperatorOverloadPair(Class @class, CXXOperatorKind op1,
            CXXOperatorKind op2)
        {
            foreach (var op in @class.Operators.Where(
                o => o.OperatorKind == op1 || o.OperatorKind == op2).ToList())
            {
                int index;
                var missingKind = CheckMissingOperatorOverloadPair(@class, out index, op1, op2,
                                                                   op.Parameters.Last().Type);

                if (missingKind == CXXOperatorKind.None)
                    return;

                if (op.Ignore) continue;

                bool isBuiltin;
                var method = new Method()
                    {
                        Name = CSharpTextTemplate.GetOperatorIdentifier(missingKind, out isBuiltin),
                        Namespace = @class,
                        IsSynthetized = true,
                        Kind = CXXMethodKind.Operator,
                        OperatorKind = missingKind,
                        ReturnType = op.ReturnType,
                        Parameters = op.Parameters
                    };

                @class.Methods.Insert(index, method);
            }
        }

        static CXXOperatorKind CheckMissingOperatorOverloadPair(Class @class,
            out int index, CXXOperatorKind op1, CXXOperatorKind op2, Type type)
        {
            var first = @class.Operators.FirstOrDefault(o => o.OperatorKind == op1 &&
                                                             o.Parameters.Last().Type.Equals(type));
            var second = @class.Operators.FirstOrDefault(o => o.OperatorKind == op2 &&
                                                              o.Parameters.Last().Type.Equals(type));

            var hasFirst = first != null;
            var hasSecond = second != null;

            if (hasFirst && (!hasSecond || second.Ignore))
            {
                index = @class.Methods.IndexOf(first);
                return op2;
            }

            if (hasSecond && (!hasFirst || first.Ignore))
            {
                index = @class.Methods.IndexOf(second);
                return op1;
            }

            index = 0;
            return CXXOperatorKind.None;
        }

        static bool IsValidOperatorOverload(Method @operator)
        {
            // These follow the order described in MSDN (Overloadable Operators).

            switch (@operator.OperatorKind)
            {
                // These unary operators can be overloaded
                case CXXOperatorKind.Plus:
                case CXXOperatorKind.Minus:
                case CXXOperatorKind.Exclaim:
                case CXXOperatorKind.Tilde:
                case CXXOperatorKind.PlusPlus:
                case CXXOperatorKind.MinusMinus:

                // These binary operators can be overloaded
                case CXXOperatorKind.Star:
                case CXXOperatorKind.Slash:
                case CXXOperatorKind.Percent:
                case CXXOperatorKind.Amp:
                case CXXOperatorKind.Pipe:
                case CXXOperatorKind.Caret:

                // The comparison operators can be overloaded
                case CXXOperatorKind.EqualEqual:
                case CXXOperatorKind.ExclaimEqual:
                case CXXOperatorKind.Less:
                case CXXOperatorKind.Greater:
                case CXXOperatorKind.LessEqual:
                case CXXOperatorKind.GreaterEqual:
                    return true;

                case CXXOperatorKind.LessLess:
                case CXXOperatorKind.GreaterGreater:
                    PrimitiveType primitiveType;
                    return @operator.Parameters.Last().Type.IsPrimitiveType(out primitiveType) &&
                           primitiveType == PrimitiveType.Int32;

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
