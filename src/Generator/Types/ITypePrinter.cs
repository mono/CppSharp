using System.Collections.Generic;
using System.Linq;

namespace Cxxi.Types
{
    public enum TypePrinterContextKind
    {
        Normal,
        Template
    }

    public abstract class TypePrinterContext
    {
        protected TypePrinterContext()
        {
            Kind = TypePrinterContextKind.Normal;
        }

        protected TypePrinterContext(TypePrinterContextKind kind)
        {
            Kind = kind;
        }

        public string GetTemplateParameterList()
        {
            var paramsList = new List<string>();
            if (Kind == TypePrinterContextKind.Template)
            {
                var template = Declaration as Template;
                paramsList = template.Parameters.Select(param => param.Name)
                    .ToList();
            }
            else
            {
                var type = Type.Desugar() as TemplateSpecializationType;
                foreach (var arg in type.Arguments)
                {
                    if (arg.Kind != TemplateArgument.ArgumentKind.Type)
                        continue;
                    paramsList.Add(arg.Type.ToString());
                }
            }

            return string.Join(", ", paramsList);
        }

        public TypePrinterContextKind Kind;
        public Declaration Declaration;
        public Type Type;
    }

    public interface ITypePrinter : ITypeVisitor<string>
    {
        string VisitParameters(IEnumerable<Parameter> @params, bool hasNames);
        string VisitParameter(Parameter param, bool hasName = true);

        string VisitDelegate(FunctionType function);
    }
}
