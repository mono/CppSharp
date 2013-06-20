using System;
using CppSharp.Types;

namespace CppSharp.Passes
{
    public class ResolveIncompleteDeclsPass : TranslationUnitPass
    {
        public ResolveIncompleteDeclsPass()
        {
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.Ignore)
                return false;

            if (!@class.IsIncomplete)
                goto Out;

            if (@class.CompleteDeclaration != null)
                goto Out;

            @class.CompleteDeclaration = Library.FindCompleteClass(
                @class.QualifiedName);

            if (@class.CompleteDeclaration == null)
                Console.WriteLine("Unresolved declaration: {0}", @class.Name);

        Out:

            return base.VisitClassDecl(@class);
        }
    }

    public static class ResolveIncompleteDeclsExtensions
    {
        public static void ResolveIncompleteDecls(this PassBuilder builder)
        {
            var pass = new ResolveIncompleteDeclsPass();
            builder.AddPass(pass);
        }
    }
}
