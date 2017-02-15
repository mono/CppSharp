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

    public class CLIBlockKind
    {
        public const int Includes = BlockKind.LAST + 1;
        public const int IncludesForwardReferences = BlockKind.LAST + 2;
        public const int Namespace = BlockKind.LAST + 3;
        public const int ForwardReferences = BlockKind.LAST + 4;
        public const int Enum = BlockKind.LAST + 5;
        public const int EnumItem = BlockKind.LAST + 6;
        public const int Class = BlockKind.LAST + 7;
        public const int Method = BlockKind.LAST + 8;
        public const int MethodBody = BlockKind.LAST + 9;
        public const int Usings = BlockKind.LAST + 10;
        public const int FunctionsClass = BlockKind.LAST + 11;
        public const int Function = BlockKind.LAST + 12;
        public const int Property = BlockKind.LAST + 13;
        public const int Typedef = BlockKind.LAST + 14;
        public const int Variable = BlockKind.LAST + 15;
        public const int Template = BlockKind.LAST + 16;
        public static int Destructor = BlockKind.LAST + 17;
        public static int Finalizer = BlockKind.LAST + 18;
        public static int AccessSpecifier = BlockKind.LAST + 19;
        public static int Fields = BlockKind.LAST + 20;
        public static int Field = BlockKind.LAST + 21;
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

        public static string SafeIdentifier(string proposedName)
        {
            return proposedName;
        }

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

        public void GenerateDeclarationCommon(Declaration decl)
        {
            if (decl.Comment == null)
                return;

            GenerateSummary(decl.Comment.BriefText);
            GenerateDebug(decl);
        }

        public void GenerateSummary(string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
                return;

            PushBlock(BlockKind.BlockComment);
            WriteLine("/// <summary>");
            WriteLine("/// {0}", comment);
            WriteLine("/// </summary>");
            PopBlock();
        }

        public void GenerateInlineSummary(RawComment comment)
        {
            if (comment == null) return;

            if (String.IsNullOrWhiteSpace(comment.BriefText))
                return;

            PushBlock(BlockKind.InlineComment);
            WriteLine("/// <summary> {0} </summary>", comment.BriefText);
            PopBlock();
        }

        public void GenerateDebug(Declaration decl)
        {
            if (Options.GenerateDebugOutput && !string.IsNullOrWhiteSpace(decl.DebugText))
                WriteLine("// DEBUG: " + decl.DebugText);
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