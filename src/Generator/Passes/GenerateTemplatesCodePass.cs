using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;

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

        public override bool VisitTemplateSpecializationType(TemplateSpecializationType template, TypeQualifiers quals)
        {
            if (AlreadyVisited(template) || template.Template.Access == AccessSpecifier.Private)
                return false;

            if (template.Arguments.Select(a => a.Type.Type.Desugar()).All(t => t.IsAddress() && !t.GetFinalPointee().IsDependent))
            {
                var cppTypePrinter = new CppTypePrinter { PrintScopeKind = CppTypePrintScopeKind.Qualified };
                templateInstantiations.Add(string.Format("{0}<{1}>", template.Template.Name,
                    string.Join(", ", template.Arguments.Select(a => a.Type.Type.Visit(cppTypePrinter)))));
            }

            return true;
        }

        private void WriteTemplateInstantiations()
        {
            foreach (var module in Driver.Options.Modules)
            {
                var cppBuilder = new StringBuilder();
                foreach (var header in module.Headers)
                    cppBuilder.AppendFormat("#include <{0}>\n", header);
                foreach (var templateInstantiation in templateInstantiations)
                    cppBuilder.AppendFormat("\ntemplate class {0};", templateInstantiation);
                var cpp = string.Format("{0}.cpp", module.TemplatesLibraryName);
                Directory.CreateDirectory(Driver.Options.OutputDir);
                var path = Path.Combine(Driver.Options.OutputDir, cpp);
                File.WriteAllText(path, cppBuilder.ToString());
            }
        }

        private HashSet<string> templateInstantiations = new HashSet<string>();
    }
}
