using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

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

            if (!VisitDeclarationContext(@class))
                return false;

            // Check for C++ operators that cannot be represented in C#.
            CheckInvalidOperators(@class);

            if (Driver.Options.IsCSharpGenerator)
            {
                // The comparison operators, if overloaded, must be overloaded in pairs;
                // that is, if == is overloaded, != must also be overloaded. The reverse
                // is also true, and similar for < and >, and for <= and >=.

                HandleMissingOperatorOverloadPair(@class, CXXOperatorKind.EqualEqual,
                                                  CXXOperatorKind.ExclaimEqual);

                HandleMissingOperatorOverloadPair(@class, CXXOperatorKind.Less,
                                                  CXXOperatorKind.Greater);

                HandleMissingOperatorOverloadPair(@class, CXXOperatorKind.LessEqual,
                                                  CXXOperatorKind.GreaterEqual);
            }

            return false;
        }

        private void CheckInvalidOperators(Class @class)
        {
            foreach (var @operator in @class.Operators.Where(o => !o.Ignore))
            {
                if (!IsValidOperatorOverload(@operator))
                {
                    Driver.Diagnostics.Debug(DiagnosticId.InvalidOperatorOverload,
                        "Invalid operator overload {0}::{1}",
                        @class.OriginalName, @operator.OperatorKind);
                    @operator.ExplicityIgnored = true;
                    continue;
                }
                if (@operator.SynthKind == FunctionSynthKind.NonMemberOperator)
                    continue;

                if (@operator.OperatorKind == CXXOperatorKind.Subscript)
                {
                    CreateIndexer(@class, @operator);
                }
                else
                {
                    // Handle missing operator parameters
                    if (@operator.IsStatic)
                        @operator.Parameters = @operator.Parameters.Skip(1).ToList();

                    var type = new PointerType()
                    {
                        QualifiedPointee = new QualifiedType(new TagType(@class)),
                        Modifier = PointerType.TypeModifier.LVReference
                    };

                    @operator.Parameters.Insert(0, new Parameter
                    {
                        Name = Generator.GeneratedIdentifier("op"),
                        QualifiedType = new QualifiedType(type),
                        Kind = ParameterKind.OperatorParameter
                    });
                }
            }
        }

        private static void CreateIndexer(Class @class, Method @operator)
        {
            Property property = new Property
                {
                    Name = "Item",
                    QualifiedType = @operator.ReturnType,
                    Access = @operator.Access,
                    Namespace = @class,
                    GetMethod = @operator
                };
            property.Parameters.AddRange(@operator.Parameters);
            if (!@operator.ReturnType.Qualifiers.IsConst && @operator.ReturnType.Type.IsAddress())
                property.SetMethod = @operator;
            @class.Properties.Add(property);
            @operator.IsGenerated = false;
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

                if (missingKind == CXXOperatorKind.None || op.Ignore)
                    continue;

                var method = new Method()
                    {
                        Name = Operators.GetOperatorIdentifier(missingKind),
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

                // These binary operators can be overloaded
                case CXXOperatorKind.Slash:
                case CXXOperatorKind.Percent:
                case CXXOperatorKind.Amp:
                case CXXOperatorKind.Pipe:
                case CXXOperatorKind.Caret:

                // The array indexing operator can be overloaded
                case CXXOperatorKind.Subscript:

                // The comparison operators can be overloaded
                case CXXOperatorKind.EqualEqual:
                case CXXOperatorKind.ExclaimEqual:
                case CXXOperatorKind.Less:
                case CXXOperatorKind.Greater:
                case CXXOperatorKind.LessEqual:
                case CXXOperatorKind.GreaterEqual:
                case CXXOperatorKind.Conversion:
                    return true;

                // Only prefix operators can be overloaded
                case CXXOperatorKind.PlusPlus:
                case CXXOperatorKind.MinusMinus:
                    return @operator.Parameters.Count == 0;

                // Bitwise shift operators can only be overloaded if the second parameter is int
                case CXXOperatorKind.LessLess:
                case CXXOperatorKind.GreaterGreater:
                    PrimitiveType primitiveType;
                    return @operator.Parameters.Last().Type.IsPrimitiveType(out primitiveType) &&
                           primitiveType == PrimitiveType.Int32;

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
