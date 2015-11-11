using System.Linq;
using CppSharp.AST;
using CppSharp.Types;
using Type = CppSharp.AST.Type;

namespace CppSharp.Generators.CSharp
{
    public class CSharpExpressionPrinterResult
    {
        public string Value;

        public override string ToString()
        {
            return Value;
        }
    }

    public static class CSharpExpressionPrinterExtensions
    {
        public static CSharpExpressionPrinterResult CSharpValue(this Expression value, CSharpExpressionPrinter printer)
        {
            return value.Visit(printer);
        }

    }

    public class CSharpExpressionPrinter : IExpressionPrinter<CSharpExpressionPrinterResult>
    {
        public CSharpExpressionPrinter(CSharpTypePrinter typePrinter)
        {
            this.typePrinter = typePrinter;
        }

        public CSharpExpressionPrinterResult VisitExpression(Expression expr)
        {
            switch (expr.Class)
            {
                case StatementClass.Call:
                    var callExpr = (CallExpr) expr;
                    switch (callExpr.Declaration.GenerationKind)
                    {
                        case GenerationKind.Generate:
                        case GenerationKind.Link:
                            return new CSharpExpressionPrinterResult
                            {
                                Value = string.Format("{0}.{1}({2})",
                                    typePrinter.VisitDeclaration(callExpr.Declaration.Namespace),
                                    callExpr.Declaration.Name,
                                    string.Join(", ", callExpr.Arguments.Select(VisitExpression)))
                            };
                        case GenerationKind.Internal:
                            // a non-ctor can only be internal if it's been converted to a property
                            return new CSharpExpressionPrinterResult
                            {
                                Value = string.Format("{0}.{1}",
                                    typePrinter.VisitDeclaration(callExpr.Declaration.Namespace),
                                    callExpr.Declaration.Name)
                            };
                        default:
                            return new CSharpExpressionPrinterResult { Value = expr.String };
                    }
                default:
                    return new CSharpExpressionPrinterResult { Value = expr.String };
            }
        }

        public string ToString(Type type)
        {
            throw new System.NotImplementedException();
        }

        private readonly CSharpTypePrinter typePrinter;
    }
}