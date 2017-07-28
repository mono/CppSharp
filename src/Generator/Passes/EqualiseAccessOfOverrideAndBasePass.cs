using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class EqualiseAccessOfOverrideAndBasePass : TranslationUnitPass
    {
        public EqualiseAccessOfOverrideAndBasePass()
        {
            VisitOptions.VisitClassBases = false;
            VisitOptions.VisitClassFields = false;
            VisitOptions.VisitClassProperties = false;
            VisitOptions.VisitEventParameters = false;
            VisitOptions.VisitFunctionParameters = false;
            VisitOptions.VisitFunctionReturnType = false;
            VisitOptions.VisitNamespaceEnums = false;
            VisitOptions.VisitNamespaceEvents = false;
            VisitOptions.VisitNamespaceVariables = false;
            VisitOptions.VisitTemplateArguments = false;
        }

        public override bool VisitASTContext(ASTContext context)
        {
            var result = base.VisitASTContext(context);

            foreach (var baseOverride in basesOverrides)
            {
                var access = baseOverride.Value.Max(o => o.Access);
                foreach (var @override in baseOverride.Value)
                    @override.Access = access;
            }

            return result;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || !method.IsOverride)
                return false;

            var baseMethod = method.GetRootBaseMethod();
            if (!baseMethod.IsGenerated)
                return false;

            HashSet<Method> overrides;
            if (basesOverrides.ContainsKey(baseMethod))
                overrides = basesOverrides[baseMethod];
            else
                overrides = basesOverrides[baseMethod] = new HashSet<Method> { baseMethod };
            overrides.Add(method);

            return true;
        }

        private Dictionary<Method, HashSet<Method>> basesOverrides = new Dictionary<Method, HashSet<Method>>();
    }
}
