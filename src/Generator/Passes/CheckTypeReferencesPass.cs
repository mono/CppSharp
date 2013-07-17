using CppSharp.AST;

namespace CppSharp.Passes
{
    public class CheckTypeReferencesPass : TranslationUnitPass
    {
        TypeRefsVisitor typeRefs;

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (unit.Ignore)
                return false;

            if (unit.IsSystemHeader)
                return false;

            typeRefs = new TypeRefsVisitor();
            return typeRefs.VisitTranslationUnit(unit);
        }
    }

    public static class CheckTypeReferencesExtensions
    {
        public static void CheckTypeReferences(this PassBuilder builder)
        {
            var pass = new CheckTypeReferencesPass();
            builder.AddPass(pass);
        }
    }
}
