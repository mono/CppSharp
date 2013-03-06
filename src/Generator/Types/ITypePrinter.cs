using System.Collections.Generic;

namespace Cxxi.Types
{
    public interface ITypePrinter : ITypeVisitor<string>
    {
        Library Library { get; set; }

        string VisitParameters(IEnumerable<Parameter> @params, bool hasNames);
        string VisitParameter(Parameter param, bool hasName = true);

        string VisitDelegate(FunctionType function);
    }
}
