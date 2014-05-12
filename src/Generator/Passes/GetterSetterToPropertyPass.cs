using System;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;

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

        Property GetOrCreateProperty(Class @class, string name, QualifiedType type)
        {
            var prop = @class.Properties.FirstOrDefault(property => property.Name == name
                && property.QualifiedType.Equals(type));

            var prop2 = @class.Properties.FirstOrDefault(property => property.Name == name);

            if (prop == null && prop2 != null)
                Driver.Diagnostics.Debug("Property {0}::{1} already exists (type: {2})",
                    @class.Name, name, type.Type.ToString());

            if (prop != null)
                return prop;

            prop = new Property
            {
                Name = name,
                Namespace = @class,
                QualifiedType = type
            };

            @class.Properties.Add(prop);
            return prop;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            if (ASTUtils.CheckIgnoreMethod(method, Driver.Options))
                return false;

            var @class = method.Namespace as Class;

            if (@class == null || @class.IsIncomplete)
                return false;

            if (method.IsConstructor)
                return false;

            if (method.IsSynthetized)
                return false;

            if (IsGetter(method))
            {
                var name = method.Name.Substring("get".Length);
                var prop = GetOrCreateProperty(@class, name, method.ReturnType);
                prop.GetMethod = method;
                prop.Access = method.Access;

                // Do not generate the original method now that we know it is a getter.
                method.GenerationKind = GenerationKind.Internal;

                Driver.Diagnostics.Debug("Getter created: {0}::{1}", @class.Name, name);

                return false;
            }

            if (IsSetter(method) && IsValidSetter(method))
            {
                var name = method.Name.Substring("set".Length);

                var type = method.Parameters[0].QualifiedType;
                var prop = GetOrCreateProperty(@class, name, type);
                prop.SetMethod = method;
                prop.Access = method.Access;

                // Ignore the original method now that we know it is a setter.
                method.GenerationKind = GenerationKind.Internal;

                Driver.Diagnostics.Debug("Setter created: {0}::{1}", @class.Name, name);

                return false;
            }

            return false;
        }

        // Check if a matching getter exist or no other setter exists.
        private bool IsValidSetter(Method method)
        {
            var @class = method.Namespace as Class;
            var name = method.Name.Substring("set".Length);

            if (method.Parameters.Count == 0)
                return false;

            var type = method.Parameters[0].Type;

            var getter = @class.Methods.FirstOrDefault(m => m.Name == "Get" + name
                && m.Type.Equals(type));

            var otherSetter = @class.Methods.FirstOrDefault(m => m.Name == method.Name
                && m.Parameters.Count == 1 
                && !m.Parameters[0].Type.Equals(type));

            return getter != null || otherSetter == null;
        }
    }
}
