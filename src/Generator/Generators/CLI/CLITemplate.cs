using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators.C;

namespace CppSharp.Generators.CLI
{
    /// <summary>
    /// There are two implementation
    /// for source (CLISources) and header (CLIHeaders)
    /// files.
    /// </summary>
    public abstract class CLITemplate : CCodeGenerator
    {
        protected CLITemplate(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units) => typePrinter = new CLITypePrinter(context);

        public abstract override string FileExtension { get; }

        public abstract override void Process();

        #region Helpers

        public string GetMethodName(Method method)
        {
            if (method.OperatorKind == CXXOperatorKind.Conversion ||
                method.OperatorKind == CXXOperatorKind.ExplicitConversion)
                return "operator " + method.ConversionType;

            if (method.IsConstructor || method.IsDestructor)
            {
                var @class = (Class) method.Namespace;
                return @class.Name;
            }

            return method.Name;
        }

        public void GenerateMethodParameters(Method method)
        {
            for (var i = 0; i < method.Parameters.Count; ++i)
            {
                if (method.Conversion == MethodConversionKind.FunctionToInstanceMethod
                    && i == 0)
                    continue;

                var param = method.Parameters[i];
                Write("{0}", CTypePrinter.VisitParameter(param));
                if (i < method.Parameters.Count - 1)
                    Write(", ");
            }
        }

        public string GenerateParametersList(List<Parameter> parameters)
        {
            var types = new List<string>();
            foreach (var param in parameters)
                types.Add(CTypePrinter.VisitParameter(param).ToString());
            return string.Join(", ", types);
        }

        #endregion
    }
}