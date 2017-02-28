using System;
using System.Collections.Generic;
using CppSharp.AST;

namespace CppSharp.Generators.CLI
{
    public struct Include
    {
        public enum IncludeKind
        {
            Angled,
            Quoted
        }

        public string File;
        public TranslationUnit TranslationUnit;

        public IncludeKind Kind;
        public bool InHeader;

        public override string ToString()
        {
            return string.Format(Kind == IncludeKind.Angled ?
                "#include <{0}>" : "#include \"{0}\"", File);
        }
    }

    /// <summary>
    /// There are two implementation
    /// for source (CLISources) and header (CLIHeaders)
    /// files.
    /// </summary>
    public abstract class CLITemplate : CodeGenerator
    {
        public CLITypePrinter TypePrinter { get; set; }

        public ISet<Include> Includes;

        protected CLITemplate(BindingContext context, IEnumerable<TranslationUnit> units)
            : base(context, units)
        {
            TypePrinter = new CLITypePrinter(context);
            Includes = new HashSet<Include>();
        }

        public abstract override string FileExtension { get; }

        public abstract override void Process();

        #region Helpers

        public string QualifiedIdentifier(Declaration decl)
        {
            if (!string.IsNullOrEmpty(TranslationUnit.Module.OutputNamespace))
            {
                if (string.IsNullOrEmpty(decl.QualifiedName))
                    return string.Format("{0}", decl.TranslationUnit.Module.OutputNamespace);

                return string.Format("{0}::{1}",
                    decl.TranslationUnit.Module.OutputNamespace, decl.QualifiedName);
            }
                
            return decl.QualifiedName;
        }

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
                Write("{0}", TypePrinter.VisitParameter(param));
                if (i < method.Parameters.Count - 1)
                    Write(", ");
            }
        }

        public string GenerateParametersList(List<Parameter> parameters)
        {
            var types = new List<string>();
            foreach (var param in parameters)
                types.Add(TypePrinter.VisitParameter(param).ToString());
            return string.Join(", ", types);
        }

        #endregion
    }
}