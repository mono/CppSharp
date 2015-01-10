using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    class SortDeclarationsPass : TranslationUnitPass
    {
        private static void SortDeclarations(Namespace @namespace)
        {
            @namespace.Declarations = @namespace.Declarations.OrderBy(
                declaration => declaration.DefinitionOrder).ToList();

            foreach (var childNamespace in @namespace.Namespaces)
                SortDeclarations(childNamespace);
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            SortDeclarations(unit);
            return true;
        }
    }
}
