using System.Collections.Generic;
using System.IO;
using System.Text;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class GenerateInlinesCodePass : TranslationUnitPass
    {
        private TranslationUnit currentUnit;
        private readonly List<string> headers = new List<string>();
        private readonly List<string> mangledInlines = new List<string>(); 

        public override bool VisitLibrary(ASTContext context)
        {
            bool result = base.VisitLibrary(context);
            Directory.CreateDirectory(Driver.Options.OutputDir);
            WriteInlinesIncludes();
            WriteInlinedSymbols();
            return result;
        }

        private void WriteInlinesIncludes()
        {
            var cppBuilder = new StringBuilder();
            headers.Sort();
            foreach (var header in headers)
                cppBuilder.AppendFormat("#include \"{0}\"\n", header);
            var cpp = string.Format("{0}.cpp", Driver.Options.InlinesLibraryName);
            var path = Path.Combine(Driver.Options.OutputDir, cpp);
            File.WriteAllText(path, cppBuilder.ToString());
        }

        private void WriteInlinedSymbols()
        {
            switch (Driver.Options.Abi)
            {
                case CppAbi.Microsoft:
                    var defBuilder = new StringBuilder("EXPORTS\r\n");
                    for (int i = 0; i < mangledInlines.Count; i++)
                        defBuilder.AppendFormat("    {0} @{1}\r\n",
                                                mangledInlines[i], i + 1);
                    var def = string.Format("{0}.def", Driver.Options.InlinesLibraryName);
                    File.WriteAllText(Path.Combine(Driver.Options.OutputDir, def),
                                      defBuilder.ToString());
                    break;
                default:
                    var symbolsBuilder = new StringBuilder();
                    foreach (var mangledInline in mangledInlines)
                        symbolsBuilder.AppendFormat("{0}\n", mangledInline);
                    var txt = string.Format("{0}.txt", Driver.Options.InlinesLibraryName);
                    File.WriteAllText(Path.Combine(Driver.Options.OutputDir, txt),
                                      symbolsBuilder.ToString());
                    break;
            }
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            currentUnit = unit;
            return base.VisitTranslationUnit(unit);
        }

        public override bool VisitFunctionDecl(Function function)
        {
            CheckForSymbols(function);
            return base.VisitFunctionDecl(function);
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            CheckForSymbols(variable);
            return base.VisitVariableDecl(variable);
        }

        private void CheckForSymbols(IMangledDecl mangled)
        {
            string symbol = mangled.Mangled;
            var declaration = (Declaration) mangled;
            if (!declaration.Ignore && AccessValid(declaration) &&
                !Driver.Symbols.FindSymbol(ref symbol) &&
                !currentUnit.FilePath.EndsWith("_impl.h") &&
                !currentUnit.FilePath.EndsWith("_p.h"))
            {
                if (!headers.Contains(currentUnit.FileName))
                    headers.Add(currentUnit.FileName);
                if (!mangledInlines.Contains(mangled.Mangled))
                    mangledInlines.Add(mangled.Mangled);
            }
        }

        private static bool AccessValid(Declaration declaration)
        {
            if (declaration.Access == AccessSpecifier.Private)
            {
                var method = declaration as Method;
                return method != null && method.IsOverride;
            }
            return true;
        }
    }
}
