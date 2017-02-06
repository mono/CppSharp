using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class GenerateInlinesCodePass : TranslationUnitPass
    {
        public GenerateInlinesCodePass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceTemplates = false;
            VisitOptions.VisitNamespaceTypedefs = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitASTContext(ASTContext context)
        {
            var result = base.VisitASTContext(context);
            WriteInlines();
            return result;
        }

        private void WriteInlines()
        {
            foreach (var module in Options.Modules.Where(m => inlinesCodeGenerators.ContainsKey(m)))
            {
                var inlinesCodeGenerator = inlinesCodeGenerators[module];
                var cpp = $"{module.InlinesLibraryName}.{inlinesCodeGenerator.FileExtension}";
                Directory.CreateDirectory(Options.OutputDir);
                var path = Path.Combine(Options.OutputDir, cpp);
                File.WriteAllText(path, inlinesCodeGenerator.Generate());
            }
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) || !NeedsSymbol(function))
                return false;

            var module = function.TranslationUnit.Module;
            var inlinesCodeGenerator = GetInlinesCodeGenerator(module);

            if (module == Options.SystemModule)
                return false;

            return function.Visit(inlinesCodeGenerator);
        }

        private bool NeedsSymbol(Function function)
        {
            var mangled = function.Mangled;
            var method = function as Method;
            return function.IsGenerated && !function.IsDeleted && !function.IsDependent &&
                !function.IsPure && (!string.IsNullOrEmpty(function.Body) || function.IsImplicit) &&
                // we don't need symbols for virtual functions anyway
                (method == null || (!method.IsVirtual && !method.IsSynthetized &&
                 (!method.IsConstructor || !((Class) method.Namespace).IsAbstract))) &&
                // we cannot handle nested anonymous types
                (!(function.Namespace is Class) || !string.IsNullOrEmpty(function.Namespace.OriginalName)) &&
                !Context.Symbols.FindSymbol(ref mangled);
        }

        InlinesCodeGenerator GetInlinesCodeGenerator(Module module)
        {
            if (inlinesCodeGenerators.ContainsKey(module))
                return inlinesCodeGenerators[module];
            
            var inlinesCodeGenerator = new InlinesCodeGenerator(Context, module.Units);
            inlinesCodeGenerators[module] = inlinesCodeGenerator;
            inlinesCodeGenerator.Process();

            return inlinesCodeGenerator;
        }

        private Dictionary<Module, InlinesCodeGenerator> inlinesCodeGenerators =
            new Dictionary<Module, InlinesCodeGenerator>();
    }
}
