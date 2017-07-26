﻿using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    /// <summary>
    /// Checks for missing operator overloads required by C#.
    /// </summary>
    public class CheckOperatorsOverloadsPass : TranslationUnitPass
    {
        public CheckOperatorsOverloadsPass()
        {
            ClearVisitedDeclarations = false;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            if (!VisitDeclarationContext(@class))
                return false;

            // Check for C++ operators that cannot be represented in .NET.
            CheckInvalidOperators(@class);

            if (Options.IsCSharpGenerator)
            {
                // In C# the comparison operators, if overloaded, must be overloaded in pairs;
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
            foreach (var @operator in @class.Operators.Where(o => o.IsGenerated))
            {
                if (!IsValidOperatorOverload(@operator) || @operator.IsPure)
                {
                    Diagnostics.Debug("Invalid operator overload {0}::{1}",
                        @class.OriginalName, @operator.OperatorKind);
                    @operator.ExplicitlyIgnore();
                    continue;
                }

                if (@operator.IsNonMemberOperator)
                    continue;

                if (@operator.OperatorKind == CXXOperatorKind.Subscript)
                    CreateIndexer(@class, @operator);
                else
                    CreateOperator(@class, @operator);
            }
        }

        private static void CreateOperator(Class @class, Method @operator)
        {
            if (@operator.IsStatic)
                @operator.Parameters = @operator.Parameters.Skip(1).ToList();

            if (@operator.ConversionType.Type == null || @operator.Parameters.Count == 0)
            {
                var type = new PointerType
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

        private void CreateIndexer(Class @class, Method @operator)
        {
            var property = new Property
                {
                    Name = "Item",
                    QualifiedType = @operator.ReturnType,
                    Access = @operator.Access,
                    Namespace = @class,
                    GetMethod = @operator
                };

            var returnType = @operator.Type;
            if (returnType.IsAddress() &&
                !returnType.GetQualifiedPointee().Type.Desugar().IsPrimitiveType(PrimitiveType.Void))
            {
                var pointer = (PointerType) returnType;
                var qualifiedPointee = pointer.QualifiedPointee;
                if (!qualifiedPointee.Qualifiers.IsConst)
                    property.SetMethod = @operator;
            }
            
            // If we've a setter use the pointee as the type of the property.
            var pointerType = property.Type as PointerType;
            if (pointerType != null && property.HasSetter)
                property.QualifiedType = new QualifiedType(
                    pointerType.Pointee, property.QualifiedType.Qualifiers);

            if (Options.IsCLIGenerator)
                // C++/CLI uses "default" as the indexer property name.
                property.Name = "default";

            property.Parameters.AddRange(@operator.Parameters);

            @class.Properties.Add(property);

            @operator.GenerationKind = GenerationKind.Internal;
        }

        private static void HandleMissingOperatorOverloadPair(Class @class, CXXOperatorKind op1,
            CXXOperatorKind op2)
        {
            foreach (var op in @class.Operators.Where(
                o => o.OperatorKind == op1 || o.OperatorKind == op2).ToList())
            {
                int index;
                var missingKind = CheckMissingOperatorOverloadPair(@class, out index, op1, op2,
                    op.Parameters.First().Type, op.Parameters.Last().Type);

                if (missingKind == CXXOperatorKind.None || !op.IsGenerated)
                    continue;

                var method = new Method()
                    {
                        Name = Operators.GetOperatorIdentifier(missingKind),
                        Namespace = @class,
                        SynthKind = FunctionSynthKind.ComplementOperator,
                        Kind = CXXMethodKind.Operator,
                        OperatorKind = missingKind,
                        ReturnType = op.ReturnType,
                        Parameters = op.Parameters
                    };

                @class.Methods.Insert(index, method);
            }
        }

        static CXXOperatorKind CheckMissingOperatorOverloadPair(Class @class, out int index,
            CXXOperatorKind op1, CXXOperatorKind op2, Type typeLeft, Type typeRight)
        {
            var first = @class.Operators.FirstOrDefault(o => o.IsGenerated && o.OperatorKind == op1 &&
                o.Parameters.First().Type.Equals(typeLeft) && o.Parameters.Last().Type.Equals(typeRight));
            var second = @class.Operators.FirstOrDefault(o => o.IsGenerated && o.OperatorKind == op2 &&
                o.Parameters.First().Type.Equals(typeLeft) && o.Parameters.Last().Type.Equals(typeRight));

            var hasFirst = first != null;
            var hasSecond = second != null;

            if (hasFirst && !hasSecond)
            {
                index = @class.Methods.IndexOf(first);
                return op2;
            }

            if (hasSecond && !hasFirst)
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
                    return @operator.Parameters.Count == 0;

                // Bitwise shift operators can only be overloaded if the second parameter is int
                case CXXOperatorKind.LessLess:
                case CXXOperatorKind.GreaterGreater:
                    PrimitiveType primitiveType;
                    return @operator.Parameters.Last().Type.IsPrimitiveType(out primitiveType) &&
                           primitiveType == PrimitiveType.Int;

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
