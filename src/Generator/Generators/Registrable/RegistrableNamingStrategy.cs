using CppSharp.AST;
using CppSharp.Generators.C;
using CppSharp.Generators.Registrable.Lua.Sol;
using System.Collections.Generic;
using System.Text;

namespace CppSharp.Generators.Registrable
{
    public class RegistrableNamingStrategy<T> where T : LuaSolGenerator
    {
        protected T Generator;

        public RegistrableNamingStrategy(T generator)
        {
            Generator = generator;
        }

        public bool IsNestedTemplate(Declaration declaration)
        {
            var currentDeclaration = declaration;
            while (true)
            {
                currentDeclaration = currentDeclaration.OriginalNamespace;
                if (currentDeclaration == null || currentDeclaration is TranslationUnit)
                {
                    break;
                }
                if (Utils.FindDescribedTemplate(currentDeclaration) != null)
                {
                    return true;
                }
            }
            return false;
        }

        public virtual string PrintClassTemplateParameter(Declaration declaration, TemplateParameterOption option)
        {
            var builder = new StringBuilder();
            if (declaration is TypeTemplateParameter typeTemplateParameter)
            {
                if (!option.IgnoreKeyword)
                {
                    builder.Append("typename");
                    if (typeTemplateParameter.IsParameterPack)
                    {
                        builder.Append("...");
                    }
                }
                if (!string.IsNullOrEmpty(typeTemplateParameter.OriginalName))
                {
                    if (!option.IgnoreKeyword)
                    {
                        builder.Append(' ');
                    }
                    if (option.CustomPrefix != null)
                    {
                        builder.Append(option.CustomPrefix);
                    }
                    builder.Append(typeTemplateParameter.OriginalName);
                }
                if (!option.IgnoreKeyword)
                {
                    if (!option.IgnoreDefault)
                    {
                        if (typeTemplateParameter.DefaultArgument.Type != null)
                        {
                            builder.Append(" = ");
                            builder.Append(typeTemplateParameter.DefaultArgument.Type.Visit(new CppTypePrinter(Generator.Context)));
                        }
                    }
                }
                else
                {
                    if (typeTemplateParameter.IsParameterPack)
                    {
                        builder.Append("...");
                    }
                }
            }
            else if (declaration is NonTypeTemplateParameter nonTypeTemplateParameter)
            {
                if (!option.IgnoreKeyword)
                {
                    builder.Append(nonTypeTemplateParameter.Type.Visit(new CppTypePrinter(Generator.Context)));
                    if (nonTypeTemplateParameter.IsParameterPack)
                    {
                        builder.Append("...");
                    }
                }
                if (!string.IsNullOrEmpty(nonTypeTemplateParameter.OriginalName))
                {
                    if (!option.IgnoreKeyword)
                    {
                        builder.Append(' ');
                    }
                    if (option.CustomPrefix != null)
                    {
                        builder.Append(option.CustomPrefix);
                    }
                    builder.Append(nonTypeTemplateParameter.OriginalName);
                }
                if (!option.IgnoreKeyword)
                {
                    if (!option.IgnoreDefault)
                    {
                        if (nonTypeTemplateParameter.DefaultArgument != null)
                        {
                            builder.Append(" = ");
                            builder.Append(nonTypeTemplateParameter.DefaultArgument.ToString());
                        }
                    }
                }
                else
                {
                    if (nonTypeTemplateParameter.IsParameterPack)
                    {
                        builder.Append("...");
                    }
                }
            }
            return builder.ToString();
        }

        public virtual string PrintClassTemplateParameters(List<Declaration> parameters, bool includeEnclosingBrackets, TemplateParameterOption option)
        {
            var builder = new StringBuilder();
            if (includeEnclosingBrackets)
            {
                builder.Append('<');
            }
            for (int i = 0; i < parameters.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(PrintClassTemplateParameter(parameters[i], option));
            }
            if (includeEnclosingBrackets)
            {
                builder.Append('>');
            }
            return builder.ToString();
        }

        public virtual string PrintClassTemplateSpecializationArgument(TemplateArgument templateArgument)
        {
            if (templateArgument.Kind == TemplateArgument.ArgumentKind.Integral)
            {
                return templateArgument.Integral.ToString();
            }
            return templateArgument.Type.Type.Visit(new CppTypePrinter(Generator.Context));
        }

        public virtual string PrintClassTemplateSpecializationArguments(List<TemplateArgument> arguments, bool includeEnclosingBrackets)
        {
            var builder = new StringBuilder();
            if (includeEnclosingBrackets)
            {
                builder.Append('<');
            }
            for (int i = 0; i < arguments.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(PrintClassTemplateSpecializationArgument(arguments[i]));
            }
            if (includeEnclosingBrackets)
            {
                builder.Append('>');
            }
            return builder.ToString();
        }

        public virtual string GetQualifiedName(Declaration declaration, FQNOption option)
        {
            if (declaration is TranslationUnit)
            {
                return "";
            }

            var name = declaration.OriginalName;
            var currentDeclaration = declaration;

            if (currentDeclaration is ClassTemplateSpecialization specialization)
            {
                if (!option.IgnoreTemplateParameters)
                {
                    name = ($"{name}{PrintClassTemplateSpecializationArguments(specialization.Arguments, true)}");
                }
            }
            else
            {
                Template template = null;
                if (currentDeclaration is not ClassTemplate)
                {
                    var describedTemplate = Utils.FindDescribedTemplate(currentDeclaration);
                    if (describedTemplate is ClassTemplate)
                    {
                        template = (ClassTemplate)describedTemplate;
                    }
                }
                if (template != null)
                {
                    if (!option.IgnoreTemplateParameters)
                    {
                        name = ($"{name}{PrintClassTemplateParameters(template.Parameters, true, TemplateParameterOption.AsArgument)}");
                    }
                }
            }

            return name;
        }

        public virtual string GetFullyQualifiedName(Declaration declaration, FQNOption option)
        {
            if (declaration is TranslationUnit)
            {
                return "";
            }

            var name = new StringBuilder();
            var currentDeclaration = declaration;
            var needsTypename = false;
            var depth = 0;

            while (true)
            {
                if (currentDeclaration == null || currentDeclaration is TranslationUnit)
                {
                    break;
                }
                depth += 1;
                var currentName = new StringBuilder();
                currentName.Append(currentDeclaration.OriginalName);

                if (currentDeclaration is ClassTemplateSpecialization specialization)
                {
                    if (!option.IgnoreTemplateTemplateKeyword)
                    {
                        if (IsNestedTemplate(currentDeclaration))
                        {
                            currentName.Insert(0, "template ");
                        }
                    }
                    if (!option.IgnoreTemplateParameters)
                    {
                        if (depth > 1)
                        {
                            needsTypename = true;
                        }
                        currentName.Append(PrintClassTemplateSpecializationArguments(specialization.Arguments, true));
                    }
                }
                else
                {
                    if (currentDeclaration is not ClassTemplate template)
                    {
                        template = (ClassTemplate)Utils.FindDescribedTemplate(currentDeclaration);
                    }
                    if (template != null)
                    {
                        if (!option.IgnoreTemplateTemplateKeyword)
                        {
                            if (IsNestedTemplate(currentDeclaration))
                            {
                                currentName.Insert(0, "template ");
                            }
                        }
                        if (!option.IgnoreTemplateParameters)
                        {
                            if (depth > 1)
                            {
                                needsTypename = true;
                            }
                            currentName.Append($"{name}{PrintClassTemplateParameters(template.Parameters, true, TemplateParameterOption.AsArgument)}");
                        }
                    }
                }

                if (name.Length != 0)
                {
                    name.Insert(0, "::");
                }
                name.Insert(0, currentName);
                currentDeclaration = currentDeclaration.OriginalNamespace;
            }
            if (!option.IgnoreGlobalNamespace)
            {
                name.Insert(0, "::");
            }
            if (!option.IgnoreTemplateTypenameKeyword)
            {
                if (needsTypename)
                {
                    name.Insert(0, "typename ");
                }
            }

            return name.ToString();
        }

        public virtual string GetContextualName(Declaration declaration, RegistrableGeneratorContext context, FQNOption option)
        {
            Class @class = null;
            Template template = null;
            if (declaration is Class)
            {
                @class = declaration as Class;
                template = Utils.FindDescribedTemplate(declaration);
            }
            else if (declaration is ClassTemplate classTemplate)
            {
                @class = classTemplate.TemplatedClass;
                template = classTemplate;
            }
            if (@class is Class)
            {
                if (template is ClassTemplate)
                {
                    // TODO: check if ClassTemplate is collapsible
                }

                if (@class.Access == AccessSpecifier.Protected)
                {
                    return GetCppContext(@class, context, FQNOption.IgnoreNone);
                }
                return GetFullyQualifiedName(declaration, option);
            }
            return GetCppContext(declaration, context, new FQNOption(false, true, false, false)) + "::" + GetQualifiedName(declaration, option);
        }

        public virtual string GetRegistrationFunctionName(Declaration declaration, bool isRecusrive = false)
        {
            if (declaration is TranslationUnit translationUnit)
            {
                return isRecusrive ? "" : $"register_{translationUnit.FileNameWithoutExtension}";
            }

            var name = declaration.OriginalName;
            var currentDeclaration = declaration;
            while (true)
            {
                currentDeclaration = currentDeclaration.OriginalNamespace;
                if (currentDeclaration == null || currentDeclaration is TranslationUnit)
                {
                    break;
                }
                name = currentDeclaration.OriginalName + "_" + name;
            }
            return name;
        }

        public virtual string GetRegistrationNameQuoted(Declaration declaration)
        {
            return $"\"{declaration.Name}\"";
        }

        public virtual string GetBindingIdName(Declaration declaration)
        {
            return Generator.GeneratorOptions.BindingIdNamePredicate(GetRegistrationFunctionName(declaration));
        }

        public virtual string GetBindingIdValue(Declaration declaration, RegistrableGeneratorContext context)
        {
            return Generator.GeneratorOptions.BindingIdValuePredicate(GetContextualName(declaration, context, FQNOption.IgnoreNone));
        }

        public virtual string GetBindingName(Declaration declaration)
        {
            return Generator.GeneratorOptions.BindingNamePredicate(GetRegistrationFunctionName(declaration));
        }

        public virtual string GetRootContextName(RegistrableGeneratorContext context)
        {
            if (context != null)
            {
                var rootContextName = context.PeekRootContextName();
                if (rootContextName != null)
                {
                    return rootContextName;
                }
            }
            return "state";
        }

        public virtual string GetBindingContextNamespacePredicate(string state, string key)
        {
            return $"get_namespace({state}, \"{key}\")";
        }

        public virtual string GetBindingContext(Declaration declaration, RegistrableGeneratorContext context)
        {
            if (context != null)
            {
                var rootContextName = context.PeekRootContextName();
                if (rootContextName != null)
                {
                    return rootContextName;
                }
            }
            if (declaration.Namespace is TranslationUnit)
            {
                return GetRootContextName(context);
            }
            else
            {
                var name = GetRootContextName(context);
                var currentDeclaration = declaration.Namespace;
                var parentList = new List<Declaration>();
                while (true)
                {
                    if (currentDeclaration == null || currentDeclaration is TranslationUnit)
                    {
                        break;
                    }
                    parentList.Insert(0, currentDeclaration);
                    currentDeclaration = currentDeclaration.Namespace;
                }
                foreach (var parent in parentList)
                {
                    name = GetBindingContextNamespacePredicate(name, parent.Name);
                }
                return name;
            }
        }

        public virtual string GetCppContext(Declaration entity, RegistrableGeneratorContext context, FQNOption option)
        {
            if (context != null)
            {
                var cppContext = context.PeekCppContext();
                if (cppContext != null)
                {
                    return cppContext.GetFullQualifiedName(option);
                }
            }
            return GetFullyQualifiedName(entity.OriginalNamespace, option);
        }

        public virtual string GetMembershipScopeName(Function function, RegistrableGeneratorContext context)
        {
            if (function is Method method)
            {
                return GetCppContext(method, context, new FQNOption()
                {
                    IgnoreTemplateTypenameKeyword = true
                }) + "::";
            }
            return "";
        }

        public virtual string GetClassTemplateName(Declaration declaration)
        {
            return $"functor_{GetRegistrationFunctionName(declaration)}";
        }
    }
}
