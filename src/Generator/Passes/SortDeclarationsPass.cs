﻿using CppSharp.AST;
using CppSharp.Types;

namespace CppSharp.Passes
{
    class SortDeclarationsPass : TranslationUnitPass
    {
        public SortDeclarationsPass()
        {
            
        }

        private static void SortDeclarations(Namespace @namespace)
        {
            @namespace.Classes.Sort((c, c1) =>
                                    (int)(c.DefinitionOrder - c1.DefinitionOrder));

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
