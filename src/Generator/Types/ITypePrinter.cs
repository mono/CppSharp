namespace Cxxi.Types
{
    public interface ITypePrinter : ITypeVisitor<string>
    {
        Library Library { get; set; }

        string GetArgumentsString(FunctionType function, bool hasNames);
        string GetArgumentString(Parameter arg, bool hasName = true);
        string ToDelegateString(FunctionType function);
    }
}
