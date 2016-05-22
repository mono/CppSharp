using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    public abstract class Expression : Statement
    {
        public string DebugText;

        public abstract TV Visit<TV>(IExpressionVisitor<TV> visitor);

        public abstract Expression Clone();
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
            var value = Value.ToString(format);
            return printAsHex ? "0x" + value : value;
        }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitExpression(this);
        }

        public override Expression Clone()
        {
            return new BuiltinTypeExpression
            {
                Value = this.Value,
                Type = this.Type,
                DebugText = this.DebugText,
                Class = this.Class,
                Declaration = this.Declaration,
                String = this.String
            };
        }
    }

    public class BinaryOperator : Expression
    {
        public BinaryOperator(Expression lhs, Expression rhs, string opcodeStr)
        {
            Class = StatementClass.BinaryOperator;
            LHS = lhs;
            RHS = rhs;
            OpcodeStr = opcodeStr;
        }

        public Expression LHS { get; set; }
        public Expression RHS { get; set; }
        public string OpcodeStr { get; set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitExpression(this);
        }

        public override Expression Clone()
        {
            return new BinaryOperator(LHS.Clone(), RHS.Clone(), OpcodeStr)
            {
                DebugText = this.DebugText,
                Declaration = this.Declaration,
                String = this.String
            };
        }
    }

    public class CallExpr : Expression
    {
        public CallExpr()
        {
            Class = StatementClass.Call;
            Arguments = new List<Expression>();
        }

        public List<Expression> Arguments { get; private set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitExpression(this);
        }

        public override Expression Clone()
        {
            var clone = new CallExpr
            {
                DebugText = this.DebugText,
                Declaration = this.Declaration,
                String = this.String
            };
            clone.Arguments.AddRange(Arguments.Select(a => a.Clone()));
            return clone;
        }
    }

    public class CXXConstructExpr : Expression
    {
        public CXXConstructExpr()
        {
            Class = StatementClass.ConstructorReference;
            Arguments = new List<Expression>();
        }

        public List<Expression> Arguments { get; private set; }

        public override T Visit<T>(IExpressionVisitor<T> visitor)
        {
            return visitor.VisitExpression(this);
        }

        public override Expression Clone()
        {
            var clone = new CXXConstructExpr
            {
                DebugText = this.DebugText,
                Declaration = this.Declaration,
                String = this.String
            };
            clone.Arguments.AddRange(Arguments.Select(a => a.Clone()));
            return clone;
        }
    }

    public interface IExpressionVisitor<out T>
    {
        T VisitExpression(Expression exp);
    }
}