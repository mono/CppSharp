using System.Linq;
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
                if (!IsValidOperatorOverload(@operator.OperatorKind))
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

                @operator.Parameters.Insert(0, new Parameter
                {
                    Name = Helpers.GeneratedIdentifier("op"),
                    QualifiedType = new QualifiedType(new TagType(@class)),
                    Kind = ParameterKind.OperatorParameter
                });
            }
        }

        static void HandleMissingOperatorOverloadPair(Class @class, CXXOperatorKind op1,
            CXXOperatorKind op2)
        {
            int index;
            var missingKind = CheckMissingOperatorOverloadPair(@class, out index, op1, op2);

            if (missingKind == CXXOperatorKind.None)
                return;

            var existingKind = missingKind == op2 ? op1 : op2;

            var overload = @class.FindOperator(existingKind).First();
            var @params = overload.Parameters;

            var method = new Method()
            {
                IsSynthetized = true,
                Kind = CXXMethodKind.Operator,
                OperatorKind = missingKind,
                ReturnType = overload.ReturnType,
                Parameters = @params
            };

            @class.Methods.Insert(index, method);
        }

        static CXXOperatorKind CheckMissingOperatorOverloadPair(Class @class,
            out int index, CXXOperatorKind op1, CXXOperatorKind op2)
        {
            var first = @class.FindOperator(op1);
            var second = @class.FindOperator(op2);

            var hasFirst = first.Count > 0;
            var hasSecond = second.Count > 0;

            if (hasFirst && !hasSecond)
            {
                index = @class.Methods.IndexOf(first.Last());
                return op2;
            }

            if (hasSecond && !hasFirst)
            {
                index = @class.Methods.IndexOf(second.First());
                return op1;
            }

            index = 0;
            return CXXOperatorKind.None;
        }

        static bool IsValidOperatorOverload(CXXOperatorKind kind)
        {
            // These follow the order described in MSDN (Overloadable Operators).

            switch (kind)
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
                case CXXOperatorKind.LessLess:
                case CXXOperatorKind.GreaterGreater:

                // The comparison operators can be overloaded
                case CXXOperatorKind.EqualEqual:
                case CXXOperatorKind.ExclaimEqual:
                case CXXOperatorKind.Less:
                case CXXOperatorKind.Greater:
                case CXXOperatorKind.LessEqual:
                case CXXOperatorKind.GreaterEqual:
                    return true;

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

    public static class CheckOperatorsOverloadsExtensions
    {
        public static void CheckOperatorOverloads(this PassBuilder builder)
        {
            var pass = new CheckOperatorsOverloadsPass();
            builder.AddPass(pass);
        }
    }
}
