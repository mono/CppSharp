using CppSharp.AST;
using System;

namespace CppSharp.Passes
{
    /// <summary>
    /// Fixes a so far irreproducible bug where parameters in a template have names
    /// different from the ones the respective parameters have in the specializations.
    /// </summary>
    public class MatchParamNamesWithInstantiatedFromPass : TranslationUnitPass
    {
        public MatchParamNamesWithInstantiatedFromPass() => VisitOptions.ResetFlags(
            VisitFlags.ClassMethods | VisitFlags.NamespaceFunctions |
            VisitFlags.ClassTemplateSpecializations);

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function) || function.InstantiatedFrom == null ||
                (function.Namespace is ClassTemplateSpecialization specialization &&
                 specialization.SpecializationKind == TemplateSpecializationKind.ExplicitSpecialization))
                return false;
            int parameters = Math.Min(function.Parameters.Count, function.InstantiatedFrom.Parameters.Count);
            for (int i = 0; i < parameters; i++)
                function.InstantiatedFrom.Parameters[i].Name = function.Parameters[i].Name;

            return true;
        }
    }
}