using System.Linq;
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
        public CheckOperatorsOverloadsPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassMethods | VisitFlags.ClassProperties |
            VisitFlags.ClassTemplateSpecializations);

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
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
                @operator.Parameters.RemoveAt(0);

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
                    Kind = ParameterKind.OperatorParameter,
                    Namespace = @operator
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
                var pointer = (PointerType)returnType;
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
                    ReturnType = op.ReturnType
                };

                method.Parameters.AddRange(op.Parameters.Select(
                    p => new Parameter(p) { Namespace = method }));

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
    }
}
