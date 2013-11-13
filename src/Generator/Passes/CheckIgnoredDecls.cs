using System;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class CheckIgnoredDeclsPass : TranslationUnitPass
    {
        public override bool VisitDeclaration(Declaration decl)
        {
            if (decl.ExplicityIgnored)
                return false;

            if (decl.Access == AccessSpecifier.Private)
            {
                Method method = decl as Method;
                if (method == null || !method.IsOverride)
                {
                    decl.ExplicityIgnored = true;
                    return false;
                }
            }

            if (decl.IsDependent)
            {
                decl.ExplicityIgnored = true;
                Log.EmitMessage("Decl '{0}' was ignored due to dependent context",
                    decl.Name);
            }

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            var type = field.Type;

            string msg;
            if (!HasInvalidType(type, out msg))
                return false;

            field.ExplicityIgnored = true;

            Log.EmitMessage("Field '{0}' was ignored due to {1} type",
                field.Name, msg);

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
                function.ExplicityIgnored = true;
                Log.EmitMessage("Function '{0}' was ignored due to {1} return decl",
                    function.Name, msg);
                return false;
            }

            foreach (var param in function.Parameters)
            {
                if (HasInvalidDecl(param, out msg))
                {
                    function.ExplicityIgnored = true;
                    Log.EmitMessage("Function '{0}' was ignored due to {1} param",
                        function.Name, msg);
                    return false;
                }

                if (HasInvalidType(param.Type, out msg))
                {
                    function.ExplicityIgnored = true;
                    Log.EmitMessage("Function '{0}' was ignored due to {1} param",
                        function.Name, msg);
                    return false;
                }

                if (param.Kind == ParameterKind.IndirectReturnType)
                {
                    Class retClass;
                    param.Type.Desugar().IsTagDecl(out retClass);
                    if (retClass == null)
                    {
                        function.ExplicityIgnored = true;
                        Driver.Diagnostics.EmitWarning(
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
            if (!VisitDeclaration(method))
                return false;

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
                    Driver.Diagnostics.EmitMessage(
                        "Virtual method '{0}' was ignored due to ignored base '{1}'",
                        method.QualifiedOriginalName, ignoredBase.Name);

                    method.ExplicityIgnored = true;
                    return false;
                }

                if (method.IsOverride)
                {
                    var baseOverride = @class.GetRootBaseMethod(method);
                    if (baseOverride != null && baseOverride.Ignore)
                    {
                        Driver.Diagnostics.EmitMessage(
                            "Virtual method '{0}' was ignored due to ignored override '{1}'",
                            method.QualifiedOriginalName, baseOverride.Name);

                        method.ExplicityIgnored = true;
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
                isIgnored |= @base.Ignore
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
                typedef.ExplicityIgnored = true;
                Log.EmitMessage("Typedef '{0}' was ignored due to {1} type",
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
                property.ExplicityIgnored = true;
                Log.EmitMessage("Property '{0}' was ignored due to {1} decl",
                    property.Name, msg);
                return false;
            }

            if (HasInvalidType(property.Type, out msg))
            {
                property.ExplicityIgnored = true;
                Log.EmitMessage("Property '{0}' was ignored due to {1} type",
                    property.Name, msg);
                return false;
            }

            if (property.GetMethod != null && !VisitFunctionDecl(property.GetMethod))
            {
                property.ExplicityIgnored = true;
                Log.EmitMessage("Property '{0}' was ignored due to ignored getter",
                                  property.Name, msg);
                return false;
            }

            if (property.SetMethod != null && !VisitFunctionDecl(property.SetMethod))
            {
                property.ExplicityIgnored = true;
                Log.EmitMessage("Property '{0}' was ignored due to ignored setter",
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
                variable.ExplicityIgnored = true;
                Log.EmitMessage("Variable '{0}' was ignored due to {1} decl",
                    variable.Name, msg);
                return false;
            }

            if (HasInvalidType(variable.Type, out msg))
            {
                variable.ExplicityIgnored = true;
                Log.EmitMessage("Variable '{0}' was ignored due to {1} type",
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
                @event.ExplicityIgnored = true;
                Log.EmitMessage("Event '{0}' was ignored due to {1} decl",
                    @event.Name, msg);
                return false;
            }

            foreach (var param in @event.Parameters)
            {
                if (HasInvalidDecl(param, out msg))
                {
                    @event.ExplicityIgnored = true;
                    Log.EmitMessage("Event '{0}' was ignored due to {1} param",
                        @event.Name, msg);
                    return false;
                }

                if (HasInvalidType(param.Type, out msg))
                {
                    @event.ExplicityIgnored = true;
                    Log.EmitMessage("Event '{0}' was ignored due to {1} param",
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
        bool HasInvalidType(AST.Type type, out string msg)
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
            ArrayType arrayType = type as ArrayType;
            PrimitiveType primitive;
            if (arrayType != null && arrayType.SizeType == ArrayType.ArraySize.Constant &&
                !arrayType.Type.Desugar().IsPrimitiveType(out primitive))
            {
                msg = "unsupported";
                return true;
            }

            msg = null;
            return false;
        }

        bool HasInvalidDecl(Declaration decl, out string msg)
        {
            if (decl == null)
            {
                msg = "null";
                return true;
            }

            if (!IsDeclComplete(decl))
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

        static bool IsTypeComplete(AST.Type type)
        {
            var checker = new TypeCompletionChecker();
            return type.Visit(checker);
        }

        static bool IsDeclComplete(Declaration decl)
        {
            var checker = new TypeCompletionChecker();
            return decl.Visit(checker);
        }

        bool IsTypeIgnored(AST.Type type)
        {
            var checker = new TypeIgnoreChecker(Driver.TypeDatabase);
            type.Visit(checker);

            return checker.IsIgnored;
        }

        bool IsDeclIgnored(Declaration decl)
        {
            var checker = new TypeIgnoreChecker(Driver.TypeDatabase);
            decl.Visit(checker);

            return checker.IsIgnored;
        }

        #endregion
    }
}
