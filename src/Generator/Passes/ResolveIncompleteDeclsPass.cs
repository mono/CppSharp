using System;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class ResolveIncompleteDeclsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (AlreadyVisited(@class))
                return false;

            if (@class.Ignore)
                return false;

            if (!@class.IsIncomplete)
                goto Out;

            if (@class.CompleteDeclaration != null)
                goto Out;

            @class.CompleteDeclaration = Library.FindCompleteClass(
                @class.QualifiedName);

            if (@class.CompleteDeclaration == null)
                Driver.Diagnostics.EmitWarning(DiagnosticId.UnresolvedDeclaration,
                    "Unresolved declaration: {0}", @class.Name);

        Out:

            return base.VisitClassDecl(@class);
        }
    }
}
