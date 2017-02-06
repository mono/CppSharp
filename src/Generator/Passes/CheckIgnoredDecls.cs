using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;

namespace CppSharp.Passes
{
    public class CheckIgnoredDeclsPass : TranslationUnitPass
    {
        public bool CheckDeclarationAccess(Declaration decl)
        {
            var generateNonPublicDecls = Options.IsCSharpGenerator;

            switch (decl.Access)
            {
                case AccessSpecifier.Public:
                    return true;
                case AccessSpecifier.Protected:
                    var @class = decl.Namespace as Class;
                    if (@class != null && @class.IsValueType)
                        return false;
                    return generateNonPublicDecls;
                case AccessSpecifier.Private:
                    var method = decl as Method;
                    var isOverride = false;
                    if (method != null && method.IsOverride)
                    {
                        var baseMethod = ((Class) method.Namespace).GetBaseMethod(method);
                        isOverride = baseMethod.IsGenerated;
                    }
                    return generateNonPublicDecls && (isOverride || decl.IsExplicitlyGenerated);
            }

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class) || !@class.IsDependent)
                return false;

            // templates are not supported yet
            foreach (var specialization in @class.Specializations.Where(s => !s.IsExplicitlyGenerated))
                specialization.ExplicitlyIgnore();

            return true;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (AlreadyVisited(decl))
                return false;

            if (decl.GenerationKind == GenerationKind.None)
                return true;

            if (!CheckDeclarationAccess(decl))
            {
                Diagnostics.Debug("Decl '{0}' was ignored due to invalid access",
                    decl.Name);
                decl.GenerationKind = decl is Field ? GenerationKind.Internal : GenerationKind.None;
                return true;
            }

            if (decl.IsDependent)
            {
                decl.GenerationKind = decl is Field ? GenerationKind.Internal : GenerationKind.None;
                Diagnostics.Debug("Decl '{0}' was ignored due to dependent context",
                    decl.Name);
                return true;
            }

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            var type = (field.Type.GetFinalPointee() ?? field.Type).Desugar();

            Declaration decl;
            type.TryGetDeclaration(out decl);
            string msg = "internal";
            if (!(type is FunctionType) && (decl == null ||
                (decl.GenerationKind != GenerationKind.Internal &&
                 !HasInvalidType(type, field.TranslationUnit.Module, out msg))))
                return false;

            field.GenerationKind = GenerationKind.Internal;

            var @class = (Class)field.Namespace;

            var cppTypePrinter = new CppTypePrinter();
            var typeName = field.Type.Visit(cppTypePrinter);

            Diagnostics.Debug("Field '{0}::{1}' was ignored due to {2} type '{3}'",
                @class.Name, field.Name, msg, typeName);

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!VisitDeclaration(function) || function.IsSynthetized)
                return false;

            var ret = function.OriginalReturnType;

            string msg;
            if (HasInvalidType(ret.Type, function.TranslationUnit.Module, out msg))
            {
                function.ExplicitlyIgnore();
                Diagnostics.Debug("Function '{0}' was ignored due to {1} return decl",
                    function.Name, msg);
                return false;
            }

            foreach (var param in function.Parameters)
            {
                if (HasInvalidDecl(param, out msg))
                {
                    function.ExplicitlyIgnore();
                    Diagnostics.Debug("Function '{0}' was ignored due to {1} param",
                        function.Name, msg);
                    return false;
                }

                if (HasInvalidType(param.Type, function.TranslationUnit.Module, out msg))
                {
                    function.ExplicitlyIgnore();
                    Diagnostics.Debug("Function '{0}' was ignored due to {1} param",
                        function.Name, msg);
                    return false;
                }

                var decayedType = param.Type.Desugar() as DecayedType;
                if (decayedType != null)
                {
                    function.ExplicitlyIgnore();
                    Diagnostics.Debug("Function '{0}' was ignored due to unsupported decayed type param",
                        function.Name);
                    return false;
                }

                if (param.Kind == ParameterKind.IndirectReturnType)
                {
                    Class retClass;
                    param.Type.Desugar().TryGetClass(out retClass);
                    if (retClass == null)
                    {
                        function.ExplicitlyIgnore();
                        Diagnostics.Debug(
                            "Function '{0}' was ignored due to an indirect return param not of a tag type",
                            function.Name);
                        return false;
                    }
                }
            }

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!CheckIgnoredBaseOverridenMethod(method))
                return false;

            return base.VisitMethodDecl(method);
        }

        bool CheckIgnoredBaseOverridenMethod(Method method)
        {
            var @class = method.Namespace as Class;

            if (!method.IsVirtual)
                return true;

            Class ignoredBase;
            if (!HasIgnoredBaseClass(method, @class, out ignoredBase))
                return true;

            Diagnostics.Debug(
                "Virtual method '{0}' was ignored due to ignored base '{1}'",
                method.QualifiedOriginalName, ignoredBase.Name);

            method.ExplicitlyIgnore();
            return false;
        }

        static bool HasIgnoredBaseClass(INamedDecl @override, Class @class,
            out Class ignoredBase)
        {
            var isIgnored = false;
            ignoredBase = null;

            foreach (var baseClassSpec in @class.Bases)
            {
                if (!baseClassSpec.IsClass)
                    continue;

                var @base = baseClassSpec.Class;
                if (!@base.Methods.Exists(m => m.Name == @override.Name))
                    continue;

                ignoredBase = @base;
                isIgnored |= !@base.IsDeclared
                    || HasIgnoredBaseClass(@override, @base, out ignoredBase);

                if (isIgnored)
                    break;
            }

            return isIgnored;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            if (!VisitDeclaration(typedef))
                return false;

            string msg;
            if (HasInvalidType(typedef.Type, typedef.TranslationUnit.Module, out msg))
            {
                typedef.ExplicitlyIgnore();
                Diagnostics.Debug("Typedef '{0}' was ignored due to {1} type",
                    typedef.Name, msg);
                return false;
            }

            return true;
        }

        public override bool VisitProperty(Property property)
        {
            if (!VisitDeclaration(property))
                return false;

            string msg;
            if (HasInvalidDecl(property, out msg))
            {
                property.ExplicitlyIgnore();
                Diagnostics.Debug("Property '{0}' was ignored due to {1} decl",
                    property.Name, msg);
                return false;
            }

            if (HasInvalidType(property.Type, property.TranslationUnit.Module, out msg))
            {
                property.ExplicitlyIgnore();
                Diagnostics.Debug("Property '{0}' was ignored due to {1} type",
                    property.Name, msg);
                return false;
            }

            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            if (!VisitDeclaration(variable))
                return false;

            string msg;
            if (HasInvalidDecl(variable, out msg))
            {
                variable.ExplicitlyIgnore();
                Diagnostics.Debug("Variable '{0}' was ignored due to {1} decl",
                    variable.Name, msg);
                return false;
            }

            if (HasInvalidType(variable.Type, variable.TranslationUnit.Module, out msg))
            {
                variable.ExplicitlyIgnore();
                Diagnostics.Debug("Variable '{0}' was ignored due to {1} type",
                    variable.Name, msg);
                return false;
            }

            return true;
        }

        public override bool VisitEvent(Event @event)
        {
            if (!VisitDeclaration(@event))
                return false;

            string msg;
            if (HasInvalidDecl(@event, out msg))
            {
                @event.ExplicitlyIgnore();
                Diagnostics.Debug("Event '{0}' was ignored due to {1} decl",
                    @event.Name, msg);
                return false;
            }

            foreach (var param in @event.Parameters)
            {
                if (HasInvalidDecl(param, out msg))
                {
                    @event.ExplicitlyIgnore();
                    Diagnostics.Debug("Event '{0}' was ignored due to {1} param",
                        @event.Name, msg);
                    return false;
                }

                if (HasInvalidType(param.Type, @event.TranslationUnit.Module, out msg))
                {
                    @event.ExplicitlyIgnore();
                    Diagnostics.Debug("Event '{0}' was ignored due to {1} param",
                        @event.Name, msg);
                    return false;
                }
            }

            return true;
        }

        #region Helpers

        /// <remarks>
        /// Checks if a given type is invalid, which can happen for a number of
        /// reasons: incomplete definitions, being explicitly ignored, or also
        /// by being a type we do not know how to handle.
        /// </remarks>
        private bool HasInvalidType(Type type, Module module, out string msg)
        {
            if (type == null)
            {
                msg = "null";
                return true;
            }

            if (!IsTypeComplete(type))
            {
                msg = "incomplete";
                return true;
            }

            if (IsTypeIgnored(type))
            {
                msg = "ignored";
                return true;
            }

            if (Options.DoAllModulesHaveLibraries() &&
                module != Options.SystemModule && IsTypeExternal(module, type))
            {
                msg = "external";
                return true;
            }

            msg = null;
            return false;
        }

        private bool HasInvalidDecl(Declaration decl, out string msg)
        {
            if (decl == null)
            {
                msg = "null";
                return true;
            }

            if (decl.IsIncomplete)
            {
                msg = "incomplete";
                return true;
            }

            if (IsDeclIgnored(decl))
            {
                msg = "ignored";
                return true;
            }

            msg = null;
            return false;
        }

        private bool IsTypeComplete(Type type)
        {
            TypeMap typeMap;
            if (TypeMaps.FindTypeMap(type, out typeMap) && !typeMap.IsIgnored)
                return true;

            var desugared = type.Desugar();
            var finalType = (desugared.GetFinalPointee() ?? desugared).Desugar();

            var templateSpecializationType = finalType as TemplateSpecializationType;
            if (templateSpecializationType != null)
                finalType = templateSpecializationType.Desugared.Type;

            Declaration decl;
            if (!finalType.TryGetDeclaration(out decl)) return true;
            return !decl.IsIncomplete || decl.CompleteDeclaration != null;
        }

        private bool IsTypeExternal(Module module, Type type)
        {
            Declaration declaration;
            if (!(type.GetFinalPointee() ?? type).TryGetDeclaration(out declaration))
                return false;

            declaration = declaration.CompleteDeclaration ?? declaration;
            if (declaration.Namespace == null || declaration.TranslationUnit.Module == null)
                return false;

            return declaration.TranslationUnit.Module.Dependencies.Contains(module);
        }

        private bool IsTypeIgnored(Type type)
        {
            var checker = new TypeIgnoreChecker(TypeMaps);
            type.Visit(checker);

            return checker.IsIgnored;
        }

        private bool IsDeclIgnored(Declaration decl)
        {
            var parameter = decl as Parameter;
            if (parameter != null && parameter.Type.Desugar().IsPrimitiveType(PrimitiveType.Null))
                return true;

            TypeMap typeMap;
            return TypeMaps.FindTypeMap(decl, out typeMap) ? typeMap.IsIgnored : decl.Ignore;
        }

        #endregion
    }
}
