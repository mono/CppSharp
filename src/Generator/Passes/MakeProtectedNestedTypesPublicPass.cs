using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MakeProtectedNestedTypesPublicPass : TranslationUnitPass
    {
        public MakeProtectedNestedTypesPublicPass()
            => VisitOptions.ResetFlags(VisitFlags.Default);

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            (from d in ((IEnumerable<Declaration>)@class.Classes).Concat(@class.Enums)
             where d.Access == AccessSpecifier.Protected
             select d).All(d => { d.Access = AccessSpecifier.Public; return true; });

            return true;
        }
    }
}
