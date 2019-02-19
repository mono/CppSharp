using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    public abstract class ExpressionObsolete : Statement
    {
        public string DebugText;

        public abstract TV Visit<TV>(IExpressionVisitorObsolete<TV> visitor);

        public abstract ExpressionObsolete Clone();
    }

    public class BuiltinTypeExpressionObsolete : ExpressionObsolete
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

        public override T Visit<T>(IExpressionVisitorObsolete<T> visitor)
        {
            return visitor.VisitExpression(this);
        }

        public override ExpressionObsolete Clone()
        {
            return new BuiltinTypeExpressionObsolete
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

    public class BinaryOperatorObsolete : ExpressionObsolete
    {
        public BinaryOperatorObsolete(ExpressionObsolete lhs, ExpressionObsolete rhs, string opcodeStr)
        {
            Class = StatementClass.BinaryOperator;
            LHS = lhs;
            RHS = rhs;
            OpcodeStr = opcodeStr;
        }

        public ExpressionObsolete LHS { get; set; }
        public ExpressionObsolete RHS { get; set; }
        public string OpcodeStr { get; set; }

        public override T Visit<T>(IExpressionVisitorObsolete<T> visitor)
        {
            return visitor.VisitExpression(this);
        }

        public override ExpressionObsolete Clone()
        {
            return new BinaryOperatorObsolete(LHS.Clone(), RHS.Clone(), OpcodeStr)
            {
                DebugText = this.DebugText,
                Declaration = this.Declaration,
                String = this.String
            };
        }
    }

    public class CallExprObsolete : ExpressionObsolete
    {
        public CallExprObsolete()
        {
            Class = StatementClass.Call;
            Arguments = new List<ExpressionObsolete>();
        }

        public List<ExpressionObsolete> Arguments { get; private set; }

        public override T Visit<T>(IExpressionVisitorObsolete<T> visitor)
        {
            return visitor.VisitExpression(this);
        }

        public override ExpressionObsolete Clone()
        {
            var clone = new CallExprObsolete
            {
                DebugText = this.DebugText,
                Declaration = this.Declaration,
                String = this.String
            };
            clone.Arguments.AddRange(Arguments.Select(a => a.Clone()));
            return clone;
        }
    }

    public class CXXConstructExprObsolete : ExpressionObsolete
    {
        public CXXConstructExprObsolete()
        {
            Class = StatementClass.ConstructorReference;
            Arguments = new List<ExpressionObsolete>();
        }

        public List<ExpressionObsolete> Arguments { get; private set; }

        public override T Visit<T>(IExpressionVisitorObsolete<T> visitor)
        {
            return visitor.VisitExpression(this);
        }

        public override ExpressionObsolete Clone()
        {
            var clone = new CXXConstructExprObsolete
            {
                DebugText = this.DebugText,
                Declaration = this.Declaration,
                String = this.String
            };
            clone.Arguments.AddRange(Arguments.Select(a => a.Clone()));
            return clone;
        }
    }

    public interface IExpressionVisitorObsolete<out T>
    {
        T VisitExpression(ExpressionObsolete exp);
    }
}