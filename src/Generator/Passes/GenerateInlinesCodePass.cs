using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class GenerateInlinesCodePass : TranslationUnitPass
    {
        public GenerateInlinesCodePass()
        {
            this.SkipPatterns = new List<string>();
        }

        public List<string> SkipPatterns { get; private set; }

        public override bool VisitLibrary(ASTContext context)
        {
            bool result = base.VisitLibrary(context);
            Directory.CreateDirectory(Driver.Options.OutputDir);
            WriteInlinesIncludes(context);
            return result;
        }

        private void WriteInlinesIncludes(ASTContext context)
        {
            var cppBuilder = new StringBuilder();
            foreach (var header in from translationUnit in context.TranslationUnits 
                                   where translationUnit.IsValid && !translationUnit.IsSystemHeader &&
                                         translationUnit.GenerationKind == GenerationKind.Generate
                                   let fileName = translationUnit.FileName
                                   where SkipPatterns.All(p => !fileName.EndsWith(p))
                                   orderby fileName
                                   select fileName)
                cppBuilder.AppendFormat("#include \"{0}\"\n", header);
            var cpp = string.Format("{0}.cpp", Driver.Options.InlinesLibraryName);
            var path = Path.Combine(Driver.Options.OutputDir, cpp);
            File.WriteAllText(path, cppBuilder.ToString());
        }
    }
}
