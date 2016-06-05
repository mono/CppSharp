using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MoveNestedTemplateInternalsPass : TranslationUnitPass
    {
        public MoveNestedTemplateInternalsPass()
        {
            Options.VisitClassBases = false;
            Options.VisitClassFields = false;
            Options.VisitClassMethods = false;
            Options.VisitClassProperties = false;
            Options.VisitFunctionParameters = false;
            Options.VisitFunctionReturnType = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceEvents = false;
            Options.VisitNamespaceTypedefs = false;
            Options.VisitNamespaceVariables = false;
            Options.VisitTemplateArguments = false;
        }

        public override bool VisitLibrary(ASTContext context)
        {
            var result = base.VisitLibrary(context);
            foreach (var entry in movedClassTemplates)
            {
                foreach (var template in entry.Value)
                {
                    foreach (var decl in new[] { template, template.TemplatedDecl })
                    {
                        int index = entry.Key.Declarations.IndexOf(decl.Namespace);
                        decl.Namespace.Declarations.Remove(decl);
                        decl.Namespace = entry.Key;
                        entry.Key.Declarations.Insert(index, decl);
                    }
                }
            }
            return result;
        }

        public override bool VisitClassTemplateDecl(ClassTemplate template)
        {
            if (!base.VisitClassTemplateDecl(template) ||
                template.Specializations.Count == 0 ||
                template.Specializations.All(s => s is ClassTemplatePartialSpecialization))
                return false;

            var @class = template.TemplatedDecl.Namespace as Class;
            if (@class == null || @class is ClassTemplateSpecialization ||
                @class.Namespace is Class)
                return false;

            if (movedClassTemplates.ContainsKey(@class.Namespace))
                movedClassTemplates[@class.Namespace].Add(template);
            else
                movedClassTemplates.Add(@class.Namespace, new List<ClassTemplate> { template });

            return true;
        }

        private Dictionary<DeclarationContext, IList<ClassTemplate>> movedClassTemplates =
            new Dictionary<DeclarationContext, IList<ClassTemplate>>();
    }
}
