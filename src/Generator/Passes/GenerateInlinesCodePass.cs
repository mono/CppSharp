using System.IO;
using System.Text;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class GenerateInlinesCodePass : TranslationUnitPass
    {
        public override bool VisitLibrary(ASTContext context)
        {
            Directory.CreateDirectory(Driver.Options.OutputDir);
            WriteInlinesIncludes();
            return true;
        }

        private void WriteInlinesIncludes()
        {
            var cppBuilder = new StringBuilder();
            foreach (var header in Driver.Options.Headers)
                cppBuilder.AppendFormat("#include <{0}>\n", header);
            var cpp = string.Format("{0}.cpp", Driver.Options.InlinesLibraryName);
            var path = Path.Combine(Driver.Options.OutputDir, cpp);
            File.WriteAllText(path, cppBuilder.ToString());
        }
    }
}
