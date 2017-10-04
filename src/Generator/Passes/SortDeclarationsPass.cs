using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class SortDeclarationsPass : TranslationUnitPass
    {
        public override bool VisitNamespace(Namespace @namespace)
        {
            if (!base.VisitNamespace(@namespace) || @namespace.Ignore)
                return false;

            @namespace.Declarations = @namespace.Declarations.OrderBy(
                d => d.DefinitionOrder).ToList();
            return true;
        }
    }
}
