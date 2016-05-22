using System.IO;
using System.Text;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class GenerateInlinesCodePass : TranslationUnitPass
    {
        public override bool VisitLibrary(ASTContext context)
        {
            WriteInlinesIncludes();
            return true;
        }

        private void WriteInlinesIncludes()
        {
            foreach (var module in Driver.Options.Modules)
            {
                var cppBuilder = new StringBuilder();
                foreach (var header in module.Headers)
                    cppBuilder.AppendFormat("#include <{0}>\n", header);
                var cpp = string.Format("{0}.cpp", module.InlinesLibraryName);
                Directory.CreateDirectory(Driver.Options.OutputDir);
                var path = Path.Combine(Driver.Options.OutputDir, cpp);
                File.WriteAllText(path, cppBuilder.ToString());
            }
        }
    }
}
