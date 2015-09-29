using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Types;

namespace CppSharp.Passes
{
    public class CheckIgnoredDeclsPass : TranslationUnitPass
    {
        public bool CheckDeclarationAccess(Declaration decl)
        {
            var generateNonPublicDecls = Driver.Options.IsCSharpGenerator;

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
                var isOverride = method != null && method.IsOverride;
                return generateNonPublicDecls && (isOverride || decl.IsExplicitlyGenerated);
            }

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
                Log.Debug("Decl '{0}' was ignored due to invalid access",
                    decl.Name);
                decl.GenerationKind = decl is Field ? GenerationKind.Internal : GenerationKind.None;
                return true;
            }

            if (decl.IsDependent)
            {
                decl.GenerationKind = decl is Field ? GenerationKind.Internal : GenerationKind.None;
                Log.Debug("Decl '{0}' was ignored due to dependent context",
                    decl.Name);
                return true;
            }

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            var type = field.Type;

            Declaration decl;
            type.TryGetDeclaration(out decl);
            string msg = "internal";
            if (decl == null || (decl.GenerationKind != GenerationKind.Internal && !HasInvalidType(type, out msg)))
                return false;

            field.GenerationKind = GenerationKind.Internal;

            var @class = (Class)field.Namespace;

            var cppTypePrinter = new CppTypePrinter(Driver.TypeDatabase);
            var typeName = type.Visit(cppTypePrinter);

            Log.Debug("Field '{0}::{1}' was ignored due to {2} type '{3}'",
                @class.Name, field.Name, msg, typeName);

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!VisitDeclaration(function))
                return false;

            var ret = function.ReturnType;

            string msg;
            if (HasInvalidType(ret.Type, out msg))
            {
                function.ExplicitlyIgnore();
                Log.Debug("Function '{0}' was ignored due to {1} return decl",
                    function.Name, msg);
                return false;
            }

            foreach (var param in function.Parameters)
            {
                if (HasInvalidDecl(param, out msg))
                {
                    function.ExplicitlyIgnore();
                    Log.Debug("Function '{0}' was ignored due to {1} param",
                        function.Name, msg);
                    return false;
                }

                if (HasInvalidType(param.Type, out msg))
                {
                    function.ExplicitlyIgnore();
                    Log.Debug("Function '{0}' was ignored due to {1} param",
                        function.Name, msg);
                    return false;
                }

                var decayedType = param.Type.Desugar() as DecayedType;
                if (decayedType != null)
                {
                    function.ExplicitlyIgnore();
                    Log.Debug("Function '{0}' was ignored due to unsupported decayed type param",
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
                        Log.Debug(
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

            if (method.IsVirtual)
            {
                Class ignoredBase;
                if (HasIgnoredBaseClass(method, @class, out ignoredBase))
                {
                    Log.Debug(
                        "Virtual method '{0}' was ignored due to ignored base '{1}'",
                        method.QualifiedOriginalName, ignoredBase.Name);

                    method.ExplicitlyIgnore();
                    return false;
                }

                if (method.IsOverride)
                {
                    var baseOverride = @class.GetBaseMethod(method);
                    if (baseOverride != null && !baseOverride.IsDeclared)
                    {
                        Log.Debug(
                            "Virtual method '{0}' was ignored due to ignored override '{1}'",
                            method.QualifiedOriginalName, baseOverride.Name);

                        method.ExplicitlyIgnore();
                        return false;
                    }
                }
            }

            return true;
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
            if (HasInvalidType(typedef.Type, out msg))
            {
                typedef.ExplicitlyIgnore();
                Log.Debug("Typedef '{0}' was ignored due to {1} type",
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
                Log.Debug("Property '{0}' was ignored due to {1} decl",
                    property.Name, msg);
                return false;
            }

            if (HasInvalidType(property.Type, out msg))
            {
                property.ExplicitlyIgnore();
                Log.Debug("Property '{0}' was ignored due to {1} type",
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
                Log.Debug("Variable '{0}' was ignored due to {1} decl",
                    variable.Name, msg);
                return false;
            }

            if (HasInvalidType(variable.Type, out msg))
            {
                variable.ExplicitlyIgnore();
                Log.Debug("Variable '{0}' was ignored due to {1} type",
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
                Log.Debug("Event '{0}' was ignored due to {1} decl",
                    @event.Name, msg);
                return false;
            }

            foreach (var param in @event.Parameters)
            {
                if (HasInvalidDecl(param, out msg))
                {
                    @event.ExplicitlyIgnore();
                    Log.Debug("Event '{0}' was ignored due to {1} param",
                        @event.Name, msg);
                    return false;
                }

                if (HasInvalidType(param.Type, out msg))
                {
                    @event.ExplicitlyIgnore();
                    Log.Debug("Event '{0}' was ignored due to {1} param",
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
        private bool HasInvalidType(Type type, out string msg)
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

            Class @class;
            var arrayType = type as ArrayType;
            PrimitiveType primitive;
            if (arrayType != null && arrayType.SizeType == ArrayType.ArraySize.Constant &&
                !arrayType.Type.IsPrimitiveType(out primitive) &&
                !arrayType.Type.Desugar().IsPointerToPrimitiveType() &&
                !(arrayType.Type.Desugar().TryGetClass(out @class) && @class.IsRefType))
            {
                msg = "unsupported";
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

        private static bool IsTypeComplete(Type type)
        {
            var desugared = type.Desugar();
            var finalType = (desugared.GetFinalPointee() ?? desugared).Desugar();

            var templateSpecializationType = finalType as TemplateSpecializationType;
            if (templateSpecializationType != null)
                finalType = templateSpecializationType.Desugared;

            Declaration decl;
            if (!finalType.TryGetDeclaration(out decl)) return true;
            return !decl.IsIncomplete;
        }

        private bool IsTypeIgnored(Type type)
        {
            var checker = new TypeIgnoreChecker(Driver.TypeDatabase);
            type.Visit(checker);

            return checker.IsIgnored;
        }

        private bool IsDeclIgnored(Declaration decl)
        {
            var parameter = decl as Parameter;
            if (parameter != null && parameter.Type.Desugar().IsPrimitiveType(PrimitiveType.Null))
                return true;

            TypeMap typeMap;
            return Driver.TypeDatabase.FindTypeMap(decl, out typeMap) ? typeMap.IsIgnored : decl.Ignore;
        }

        #endregion
    }
}
