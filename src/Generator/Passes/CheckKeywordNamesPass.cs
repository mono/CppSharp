using CppSharp.AST;
using CppSharp.Generators.CSharp;

namespace CppSharp.Passes
{
    public class CheckKeywordNamesPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || @class.Layout == null)
                return false;

            foreach (var field in @class.Layout.Fields)
                field.Name = SafeIdentifier(field.Name);

            return true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl) || decl.Ignore)
                return false;

            decl.Name = SafeIdentifier(decl.Name);
            return true;
        }

        private string SafeIdentifier(string id) =>
            Options.IsCLIGenerator ? id : CSharpSources.SafeIdentifier(id);
    }
}
