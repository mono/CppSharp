using System;
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

        public override bool VisitMethodDecl(Method method)
        {
            if (!base.VisitMethodDecl(method) || !method.OverriddenMethods.Any())
                return false;

            var virtuals = new List<Method>(method.OverriddenMethods);
            virtuals.Add(method);
            AccessSpecifier access = virtuals.Max(o => o.Access);
            foreach (var @virtual in virtuals)
                @virtual.Access = access;

            return true;
        }
    }
}
