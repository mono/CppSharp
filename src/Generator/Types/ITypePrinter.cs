using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Types
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
                    var argType = arg.Type.Type.IsPointerToPrimitiveType()
                        ? new CILType(typeof(System.IntPtr))
                        : arg.Type.Type;
                    paramsList.Add(argType.ToString());
                }
            }

            return string.Join(", ", paramsList);
        }

        public TypePrinterContextKind Kind;
        public Declaration Declaration;
        public Parameter Parameter;
        public Type Type;
    }

    public interface ITypePrinter
    {
        string ToString(Type type);
    }

    public interface ITypePrinter<out T> : ITypePrinter, ITypeVisitor<T>
    {
        T VisitParameters(IEnumerable<Parameter> @params, bool hasNames);
        T VisitParameter(Parameter param, bool hasName = true);

        T VisitDelegate(FunctionType function);
    }
}
