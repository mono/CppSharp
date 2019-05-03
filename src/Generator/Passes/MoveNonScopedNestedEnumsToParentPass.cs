using CppSharp.AST;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass moves old-style (non-scoped) enumerations nested in classes
    /// to the parents of those classes.
    /// </summary>
    /// <remarks>
    /// Such enums are presumably written this way because C++ before 11
    /// could not scope enums and nesting was the only way to do so
    /// in order to prevent conflicts. But target languages don't have
    /// this limitation so we can generate a more sensible API.
    /// </remarks>
    public class MoveNonScopedNestedEnumsToParentPass : TranslationUnitPass
    {
        public override bool VisitASTContext(ASTContext context)
        {
            bool result = base.VisitASTContext(context);

            foreach (var movableEnum in movableEnums)
            {
                DeclarationContext declarationContext = movableEnum.Namespace;
                declarationContext.Declarations.Remove(movableEnum);
                declarationContext.Namespace.Declarations.Add(movableEnum);
                movableEnum.Namespace = declarationContext.Namespace;
            }

            return result;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!base.VisitEnumDecl(@enum))
                return false;

            if (string.IsNullOrEmpty(@enum.Name) ||
                !(@enum.Namespace is Class) ||
                @enum.Access != AccessSpecifier.Public ||
                @enum.IsScoped)
                return false;

            if (@enum.Namespace.Namespace.Declarations.Union(movableEnums).Any(
                    e => e.Name == @enum.Name))
                return false;

            movableEnums.Add(@enum);

            return true;
        }

        private readonly List<Enumeration> movableEnums = new List<Enumeration>();
    }
}
