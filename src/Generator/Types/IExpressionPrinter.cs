using CppSharp.AST;

namespace CppSharp.Types
{
    public interface IExpressionPrinter
    {
        string ToString(Type type);
    }

    public interface IExpressionPrinter<out T> : IExpressionPrinter, IExpressionVisitor<T>
    {
    }
}