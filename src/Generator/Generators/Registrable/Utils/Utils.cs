using CppSharp.AST;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CppSharp.Generators.Registrable
{
    public static class Utils
    {
        public static Template FindDescribedTemplate(Declaration declaration)
        {
            foreach (var template in declaration.Namespace.Templates)
            {
                if (template.TemplatedDecl == declaration)
                {
                    return template;
                }
            }
            return null;
        }

        public static DetachmentOption FindDetachmentOption(Declaration declaration)
        {
            return (declaration.Namespace is Class) ? DetachmentOption.Off : DetachmentOption.On;
        }

        public static bool HasPossibleOverload(Function function)
        {
            var parent = function.OriginalNamespace;
            if (parent is Class @class)
            {
                foreach (var item in @class.Methods)
                {
                    if (item.OriginalFunction == null)
                    {
                        if (item != function)
                        {
                            if (item.OriginalName == function.Name)
                            {
                                return true;
                            }
                        }
                    }
                }
                foreach (var item in @class.Functions)
                {
                    if (item.OriginalFunction == null)
                    {
                        if (item != function)
                        {
                            if (item.OriginalName == function.Name)
                            {
                                return true;
                            }
                        }
                    }
                }
                foreach (var item in @class.Templates.Where(template => template is FunctionTemplate).Cast<FunctionTemplate>())
                {
                    var templatedFunction = item.TemplatedFunction;
                    if (templatedFunction.OriginalFunction == null)
                    {
                        if (templatedFunction != function)
                        {
                            if (templatedFunction.OriginalName == function.Name)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            return true;
        }

        public static bool HasPossibleTemplateOverload(Function function)
        {
            var parent = function.OriginalNamespace;
            if (parent is Class @class)
            {
                foreach (var item in @class.Templates.Where(template => template is FunctionTemplate).Cast<FunctionTemplate>())
                {
                    var templatedFunction = item.TemplatedFunction;
                    if (templatedFunction.OriginalFunction == null)
                    {
                        if (templatedFunction != function)
                        {
                            if (templatedFunction.OriginalName == function.Name)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
            return true;
        }

        public static bool HasPossibleTemplateOverload(Method method)
        {
            if (method.Kind == CXXMethodKind.UsingDirective)
            {
                return true;
            }
            return HasPossibleTemplateOverload(method as Function);
        }

        public static bool IsDefaultTemplateParameter(Declaration parameter)
        {
            if (parameter is TypeTemplateParameter typeTemplateParameter)
            {
                return typeTemplateParameter.DefaultArgument.Type != null;
            }
            else if (parameter is NonTypeTemplateParameter nonTypeTemplateParameter)
            {
                return nonTypeTemplateParameter.DefaultArgument != null;
            }
            else if (parameter is TemplateTemplateParameter templateTemplateParameter)
            {
                throw new InvalidOperationException();
            }
            throw new InvalidOperationException();
        }

        public static bool IsDefaultTemplateParameterList(List<Declaration> parameters)
        {
            foreach (var parameter in parameters)
            {
                if (!IsDefaultTemplateParameter(parameter))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
