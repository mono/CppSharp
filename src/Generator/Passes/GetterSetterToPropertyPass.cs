using System;
using CppSharp.AST;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass checks for compatible combinations of getters/setters methods
    /// and creates matching properties that call back into the methods.
    /// </summary>
    public class GetterSetterToPropertyPass : TranslationUnitPass
    {
        public GetterSetterToPropertyPass()
        {
            Options.VisitClassFields = false;
            Options.VisitClassProperties = false;
            Options.VisitNamespaceEnums = false;
            Options.VisitNamespaceTemplates = false;
            Options.VisitNamespaceTypedefs = false;
            Options.VisitNamespaceEvents = false;
            Options.VisitNamespaceVariables = false;
            Options.VisitFunctionParameters = false;
            Options.VisitTemplateArguments = false;
        }

        static bool IsSetter(Function method)
        {
            var isRetVoid = method.ReturnType.Type.IsPrimitiveType(
                PrimitiveType.Void);

            var isSetter = method.OriginalName.StartsWith("set",
                StringComparison.InvariantCultureIgnoreCase);

            return isRetVoid && isSetter && method.Parameters.Count == 1;
        }

        static bool IsGetter(Function method)
        {
            var isRetVoid = method.ReturnType.Type.IsPrimitiveType(
                PrimitiveType.Void);

            var isGetter = method.OriginalName.StartsWith("get",
                StringComparison.InvariantCultureIgnoreCase);

            return !isRetVoid && isGetter && method.Parameters.Count == 0;
        }

        public override bool VisitMethodDecl(Method method)
        {
            //var expansions = method.PreprocessedEntities.OfType<MacroExpansion>();
            //if (expansions.Any(e => e.Text.Contains("ACCESSOR")))
            //    System.Diagnostics.Debugger.Break();

            if (!IsGetter(method))
                return false;

            var @class = method.Namespace as Class;
            foreach (var classMethod in @class.Methods)
            {
                if (!IsSetter(classMethod))
                    continue;

                if (classMethod.Parameters[0].Type.Equals(method.ReturnType.Type))
                    continue;

                var getName = method.Name.Substring("get".Length);
                var setName = classMethod.Name.Substring("set".Length);

                if (getName != setName)
                    continue;

                // We found a compatible pair of methods, create a property.
                var prop = new Property
                {
                    Name = getName,
                    Namespace = @class,
                    QualifiedType = method.ReturnType
                };

                // Ignore the original methods now that we have a property.
                method.ExplicityIgnored = true;
                classMethod.ExplicityIgnored = true;

                @class.Properties.Add(prop);

                Driver.Diagnostics.EmitMessage(DiagnosticId.PropertySynthetized,
                    "Getter/setter property created: {0}::{1}", @class.Name,
                    getName);
                return true;
            }

            return false;
        }
    }
}
