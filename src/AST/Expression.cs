using System.Collections.Generic;

namespace CppSharp.AST
{
    public abstract class Expression : Statement
    {
        public string DebugText;

        public abstract TV Visit<TV>(IExpressionVisitor<TV> visitor);
    }

    public class BuiltinTypeExpression : Expression
    {
        public long Value { get; set; }

        public BuiltinType Type { get; set; }

        public bool IsHexadecimal
        {
            get
            {
                if (DebugText == null)
                {
                    return false;
                }
                return DebugText.Contains("0x") || DebugText.Contains("0X");
            }
        }

        public override string ToString()
        {
            var printAsHex = IsHexadecimal && Type.IsUnsigned;
            var format = printAsHex ? "x" : string.Empty;
            var value = Type.IsUnsigned ? Value.ToString(format) :
                ((long)Value).ToString(format);
            return printAsHex ? "0x" + value : value;
        }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitExpression(this);
        }
    }

    public class CXXConstructExpr : Expression
    {
        public CXXConstructExpr()
        {
            Arguments = new List<Expression>();
        }

        public List<Expression> Arguments { get; private set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitExpression(this);
        }
    }

    public class BinaryOperator : Expression
    {
        public BinaryOperator(Expression lhs, Expression rhs, string opcodeStr)
        {
            Class = StatementClass.BinaryOperator;
            this.LHS = lhs;
            this.RHS = rhs;
            this.OpcodeStr = opcodeStr;
        }

        public Expression LHS { get; set; }
        public Expression RHS { get; set; }
        public string OpcodeStr { get; set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitExpression(this);
        }
    }

    public interface IExpressionVisitor<out T>
    {
        T VisitExpression(Expression exp);
    }
}