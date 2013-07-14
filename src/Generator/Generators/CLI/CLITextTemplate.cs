using System;
using System.Collections.Generic;

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
        public IncludeKind Kind;

        public override string ToString()
        {
            return string.Format(Kind == IncludeKind.Angled ?
                "#include <{0}>" : "#include \"{0}\"", File);
        }
    }

    public enum CLIBlockKind
    {
        Unknown,
        Header,
        Includes,
        IncludesForwardReferences,
        Namespace,
        ForwardReferences,
        Footer,
        Enum,
        Typedef,
        Class,
        Method,
        Usings
    }

    public class CLIBlock : IBlock<CLIBlock, CLIBlockKind>
    {
        public CLIBlockKind Kind { get; set; }
        public List<CLIBlock> Blocks { get; set; }
        public CLIBlock Parent { get; set; }

        public TextGenerator Text { get; set; }
        public Declaration Declaration { get; set; }

        public CLIBlock()
        {
            Blocks = new List<CLIBlock>();
            Kind = CLIBlockKind.Unknown;
        }

        public override string ToString()
        {
            return Kind.ToString();
        }
    }

    /// <summary>
    /// There are two implementation
    /// for source (CLISourcesTemplate) and header (CLIHeadersTemplate)
    /// files.
    /// </summary>
    public abstract class CLITextTemplate : BlockGenerator<CLIBlockKind, CLIBlock>
    {
        public CLITypePrinter TypePrinter { get; set; }

        public ISet<Include> Includes;

        protected CLITextTemplate(Driver driver, TranslationUnit unit)
            : base(driver, unit)
        {
            TypePrinter = new CLITypePrinter(driver);
            Includes = new HashSet<Include>();
        }

        public abstract override string FileExtension { get; }

        public abstract override void GenerateBlocks();

        #region Helpers

        public static string SafeIdentifier(string proposedName)
        {
            return proposedName;
        }

        public string QualifiedIdentifier(Declaration decl)
        {
            if (Options.GenerateLibraryNamespace)
                return string.Format("{0}::{1}", Options.OutputNamespace, decl.QualifiedName);
            return string.Format("{0}", decl.QualifiedName);
        }

        public void GenerateSummary(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return;

            // Wrap the comment to the line width.
            var maxSize = (int)(Options.MaxIndent - CurrentIndent.Count - "/// ".Length);
            var lines = StringHelpers.WordWrapLines(comment, maxSize);

            WriteLine("/// <summary>");
            foreach (string line in lines)
                WriteLine(string.Format("/// {0}", line.TrimEnd()));
            WriteLine("/// </summary>");
        }

        public void GenerateInlineSummary(string comment)
        {
            if (String.IsNullOrWhiteSpace(comment))
                return;
            WriteLine("/// <summary> {0} </summary>", comment);
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
                types.Add(TypePrinter.VisitParameter(param));
            return string.Join(", ", types);
        }

        #endregion
    }
}