namespace CppSharp.Passes
{
    public class CheckTypeReferencesPass : TranslationUnitPass
    {
        TypeRefsVisitor typeRefs;

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
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
