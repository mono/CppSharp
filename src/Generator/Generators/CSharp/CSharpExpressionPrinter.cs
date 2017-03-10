using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public static class CSharpExpressionPrinterExtensions
    {
        public static string CSharpValue(this Expression value, CSharpExpressionPrinter printer)
        {
            return value.Visit(printer);
        }
    }

    public class CSharpExpressionPrinter : IExpressionPrinter<string>
    {
        public CSharpExpressionPrinter(CSharpTypePrinter typePrinter)
        {
            this.typePrinter = typePrinter;
        }

        public string VisitParameter(Parameter parameter)
        {
            var expression = VisitExpression(parameter.DefaultArgument);
            var desugared = parameter.Type.Desugar();
            if (desugared.IsPrimitiveType() &&
                (parameter.DefaultArgument.Declaration != null ||
                 parameter.DefaultArgument.Class == StatementClass.BinaryOperator))
                return $"({desugared.Visit(typePrinter)}) {expression}";
            return expression;
        }

        public string VisitExpression(Expression expr)
        {
            switch (expr.Class)
            {
                case StatementClass.Call:
                    var callExpr = (CallExpr) expr;
                    switch (callExpr.Declaration.GenerationKind)
                    {
                        case GenerationKind.Generate:
                            return string.Format("{0}.{1}({2})",
                                typePrinter.VisitDeclaration(callExpr.Declaration.Namespace),
                                callExpr.Declaration.Name,
                                string.Join(", ", callExpr.Arguments.Select(VisitExpression)));
                        case GenerationKind.Internal:
                            // a non-ctor can only be internal if it's been converted to a property
                            var property = ((Class) callExpr.Declaration.Namespace).Properties.First(
                                p => p.GetMethod == callExpr.Declaration);
                            return string.Format("{0}.{1}",
                                typePrinter.VisitDeclaration(callExpr.Declaration.Namespace),
                                property.Name);
                        default:
                            return expr.String;
                    }
                case StatementClass.DeclarationReference:
                    if (expr.Declaration is Variable || expr.Declaration is Enumeration.Item)
                        return expr.Declaration.Visit(typePrinter).Type;
                    goto default;
                case StatementClass.BinaryOperator:
                    var binaryOperator = (BinaryOperator) expr;

                    var lhsResult = binaryOperator.LHS.String;
                    if (binaryOperator.LHS.Declaration is Enumeration.Item)
                        lhsResult = binaryOperator.LHS.Declaration.Visit(typePrinter).Type;

                    var rhsResult = binaryOperator.RHS.String;
                    if (binaryOperator.RHS.Declaration is Enumeration.Item)
                        rhsResult = binaryOperator.RHS.Declaration.Visit(typePrinter).Type;

                    return $"{lhsResult} {binaryOperator.OpcodeStr} {rhsResult}";
                case StatementClass.ConstructorReference:
                    var constructorExpr = (CXXConstructExpr) expr;
                    if (constructorExpr.Arguments.Count == 1 &&
                        constructorExpr.Arguments[0].Declaration is Enumeration.Item)
                        return constructorExpr.Arguments[0].Declaration.Visit(typePrinter).Type;
                    goto default;
                default:
                    return expr.String;
            }
        }

        public string ToString(Type type)
        {
            throw new System.NotImplementedException();
        }

        private readonly CSharpTypePrinter typePrinter;
    }
}