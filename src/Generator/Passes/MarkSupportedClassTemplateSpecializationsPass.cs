using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace CppSharp.Passes
{
    public class MarkSupportedSpecializationsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || !@class.IsDependent)
                return false;

            foreach (var specialization in @class.Specializations)
            {
                if (IsSupportedStdSpecialization(specialization))
                {
                    MarkForGeneration(specialization);
                    @class.GenerationKind = GenerationKind.Generate;
                }
            }

            return true;
        }

        private static bool IsSupportedStdSpecialization(ClassTemplateSpecialization specialization)
        {
            return IsSupportedStdType(specialization) &&
                specialization.Arguments[0].Type.Type.IsPrimitiveType(PrimitiveType.Char);
        }

        private static bool IsSupportedStdType(Declaration declaration)
        {
            return declaration.Namespace != null &&
                declaration.TranslationUnit.IsSystemHeader &&
                IsNameSpaceStd(declaration.Namespace) &&
                supportedStdTypes.Contains(declaration.OriginalName);
        }

        private static bool IsNameSpaceStd(DeclarationContext declarationContext)
        {
            if (declarationContext == null)
                return false;
            var @namespace = declarationContext as Namespace;
            if (@namespace != null && @namespace.IsInline)
                return IsNameSpaceStd(declarationContext.Namespace);
            return declarationContext.OriginalName == "std";
        }

        private static void MarkForGeneration(ClassTemplateSpecialization specialization)
        {
            specialization.GenerationKind = GenerationKind.Generate;
            Declaration declaration = specialization.TemplatedDecl.TemplatedDecl;
            while (declaration != null)
            {
                declaration.GenerationKind = GenerationKind.Generate;
                declaration = declaration.Namespace;
            }
        }

        private static string[] supportedStdTypes = { "basic_string", "allocator" };
    }
}
