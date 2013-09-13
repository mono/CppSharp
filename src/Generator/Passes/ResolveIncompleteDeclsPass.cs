using System;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class ResolveIncompleteDeclsPass : TranslationUnitPass
    {
        public override bool VisitDeclaration(Declaration decl)
        {
            if (AlreadyVisited(decl))
                return false;

            if (decl.Ignore)
                return false;

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            if (!@class.IsIncomplete)
                goto Out;

            if (@class.CompleteDeclaration != null)
                goto Out;

            @class.CompleteDeclaration =
                Library.FindCompleteClass(@class.QualifiedName);

            if (@class.CompleteDeclaration == null)
            {
                @class.IsGenerated = false;
                Driver.Diagnostics.EmitWarning(DiagnosticId.UnresolvedDeclaration,
                    "Unresolved declaration: {0}", @class.Name);
            }

        Out:

            return base.VisitClassDecl(@class);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            if (!@enum.IsIncomplete)
                goto Out;

            if (@enum.CompleteDeclaration != null)
                goto Out;

            @enum.CompleteDeclaration =
                Library.FindCompleteEnum(@enum.QualifiedName);

            if (@enum.CompleteDeclaration == null)
            {
                @enum.IsGenerated = false;
                Driver.Diagnostics.EmitWarning(DiagnosticId.UnresolvedDeclaration,
                    "Unresolved declaration: {0}", @enum.Name);
            }

        Out:

            return base.VisitEnumDecl(@enum);
        }
    }
}
