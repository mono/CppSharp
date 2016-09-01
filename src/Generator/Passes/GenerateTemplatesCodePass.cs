using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class GenerateTemplatesCodePass : TranslationUnitPass
    {
        public override bool VisitLibrary(ASTContext context)
        {
            base.VisitLibrary(context);
            WriteTemplateInstantiations();
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || !@class.IsDependent)
                return false;

            var cppTypePrinter = new CppTypePrinter
            {
                PrintScopeKind = CppTypePrintScopeKind.Qualified,
                PrintLogicalNames = true
            };
            foreach (var specialization in @class.Specializations.Where(s => !s.IsDependent && !s.Ignore))
            {
                var cppCode = specialization.Visit(cppTypePrinter);
                var module = specialization.TranslationUnit.Module;
                if (templateInstantiations.ContainsKey(module))
                    templateInstantiations[module].Add(cppCode);
                else
                    templateInstantiations.Add(module, new HashSet<string> { cppCode });
            }
            return true;
        }

        private void WriteTemplateInstantiations()
        {
            foreach (var module in Options.Modules.Where(m => templateInstantiations.ContainsKey(m)))
            {
                var cppBuilder = new StringBuilder();
                if (module == Options.SystemModule)
                {
                    cppBuilder.Append("#include <string>\n");
                    cppBuilder.Append("#include <vector>\n");
                    cppBuilder.Append("#include <map>\n");
                    cppBuilder.Append("#include <unordered_map>\n");
                }
                else
                    foreach (var header in module.Headers)
                        cppBuilder.AppendFormat("#include <{0}>\n", header);
                foreach (var templateInstantiation in templateInstantiations[module])
                    cppBuilder.AppendFormat("\ntemplate class {0}{1};",
                        Platform.IsWindows ? "__declspec(dllexport) " : string.Empty, templateInstantiation);
                var cpp = string.Format("{0}.cpp", module.TemplatesLibraryName);
                Directory.CreateDirectory(Options.OutputDir);
                var path = Path.Combine(Options.OutputDir, cpp);
                File.WriteAllText(path, cppBuilder.ToString());
            }
        }

        private Dictionary<Module, HashSet<string>> templateInstantiations = new Dictionary<Module, HashSet<string>>();
    }
}
