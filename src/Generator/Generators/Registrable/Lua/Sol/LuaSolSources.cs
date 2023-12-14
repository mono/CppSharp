using CppSharp.AST;
using CppSharp.Generators.C;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CppSharp.Generators.Registrable.Lua.Sol
{
    public class LuaSolSources : CodeGenerator
    {
        protected LuaSolGenerator Generator { get; }
        protected LuaSolGenerationContext GenerationContext { get; }
        protected LuaSolNamingStrategy NamingStrategy => Generator.GeneratorOptions.NamingStrategy;

        public LuaSolSources(LuaSolGenerator generator, IEnumerable<TranslationUnit> units)
            : base(generator.Context, units)
        {
            Generator = generator;
            GenerationContext = new LuaSolGenerationContext();
        }

        public override string FileExtension { get { return "cpp"; } }

        protected virtual bool TemplateAllowed { get { return false; } }

        protected bool NonTemplateAllowed { get { return !TemplateAllowed || GenerationContext.PeekTemplateLevel() != 0; } }

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock(BlockKind.Includes);
            var file = Context.Options.GetIncludePath(TranslationUnit);
            WriteLine($"#include \"{file}\"");

            NewLine();
            PopBlock();

            TranslationUnit.Visit(this);

            PushBlock(BlockKind.Footer);
            PopBlock();
        }

        public virtual void GenerateDeclarationGlobalStateRegistration(Declaration declaration)
        {
            if (declaration.Access != AccessSpecifier.Protected)
            {
                if (declaration.OriginalNamespace is not Class)
                {
                    Write(NamingStrategy.GetBindingContext(declaration, GenerationContext));
                }
                else
                {
                    Write($"{NamingStrategy.GetRootContextName(GenerationContext)}[{NamingStrategy.GetBindingIdValue(declaration.Namespace, GenerationContext)}]");
                }
                Write($"[{NamingStrategy.GetRegistrationNameQuoted(declaration)}] = ");
                Write($"{NamingStrategy.GetRootContextName(GenerationContext)}[{NamingStrategy.GetBindingIdName(declaration)}];");
                NewLine();
            }
        }

        public virtual void GenerateDeclarationContainerList(DeclarationContext declaration)
        {
            List<Declaration> declarations = declaration.Declarations.Where(declaration => declaration is Namespace || declaration is Class || declaration is Enumeration).ToList();
            declarations.Sort((x, y) => x.LineNumberStart.CompareTo(y.LineNumberStart));
            foreach (var item in declarations)
            {
                item.Visit(this);
            };
        }

        #region TranslationUnit

        public virtual string GetTranslationUnitRegistrationFunctionSignature(TranslationUnit translationUnit)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("void ");
            builder.Append(Generator.GeneratorOptions.NamingStrategy.GetRegistrationFunctionName(translationUnit));
            builder.Append("(::sol::state_view& state) {");
            return builder.ToString();
        }

        public virtual void GenerateTranslationUnitNamespaceBegin(TranslationUnit translationUnit)
        {
            PushBlock(BlockKind.Namespace);
            WriteLine($"namespace {TranslationUnit.Module.OutputNamespace} {{");
        }

        public virtual void GenerateTranslationUnitNamespaceEnd(TranslationUnit translationUnit)
        {
            WriteLine($"}}  // namespace {TranslationUnit.Module.OutputNamespace}");
            PopBlock();
        }

        public virtual void GenerateTranslationUnitRegistrationFunctionBegin(TranslationUnit translationUnit)
        {
            PushBlock(BlockKind.Function);
            NewLine();
            WriteLine(GetTranslationUnitRegistrationFunctionSignature(translationUnit));
            Indent();
        }

        public virtual void GenerateTranslationUnitRegistrationFunctionBody(TranslationUnit translationUnit)
        {
            GenerateDeclarationContainerList(translationUnit);

            GenerationContext.Scoped(RegistrableGeneratorContext.IsDetach, DetachmentOption.On, () =>
            {
                foreach (var variable in translationUnit.Variables)
                {
                    variable.Visit(this);
                }

                var methods = translationUnit.Functions.Where(method => !(method.IsOperator));
                var overloads = methods.GroupBy(m => m.Name);
                foreach (var overload in overloads)
                {
                    GenerateFunctions(translationUnit, overload.ToList());
                }

                foreach (var typedef in translationUnit.Typedefs)
                {
                    typedef.Visit(this);
                }
            });
        }

        public virtual void GenerateTranslationUnitRegistrationFunctionEnd(TranslationUnit translationUnit)
        {
            Unindent();
            WriteLine("}");
            NewLine();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public virtual void GenerateTranslationUnit(TranslationUnit translationUnit)
        {
            GenerateTranslationUnitNamespaceBegin(translationUnit);
            GenerateTranslationUnitRegistrationFunctionBegin(translationUnit);
            GenerateTranslationUnitRegistrationFunctionBody(translationUnit);
            GenerateTranslationUnitRegistrationFunctionEnd(translationUnit);
            GenerateTranslationUnitNamespaceEnd(translationUnit);
        }

        public virtual bool CanGenerateTranslationUnit(TranslationUnit unit)
        {
            if (AlreadyVisited(unit))
            {
                return false;
            }
            return true;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            if (!CanGenerateTranslationUnit(unit))
            {
                return false;
            }

            GenerateTranslationUnit(unit);

            return true;
        }

        #endregion

        #region Namespace

        public virtual void GenerateNamespaceDebugName(Namespace @namespace)
        {
            WriteLine($"/* {NamingStrategy.GetFullyQualifiedName(@namespace, FQNOption.IgnoreNone)} */");
        }

        public virtual void GenerateNamespaceHeader(Namespace @namespace)
        {
            WriteLine("{");
            Indent();
        }

        public virtual void GenerateNamespaceBegin(Namespace @namespace)
        {
            Write($"auto {NamingStrategy.GetBindingName(@namespace)} = ");
            Write(NamingStrategy.GetBindingContextNamespacePredicate(
                NamingStrategy.GetBindingContext(@namespace, GenerationContext),
                @namespace.Name)
            );
            WriteLine(";");
        }

        public virtual void GenerateNamespaceBody(Namespace @namespace)
        {
            GenerateNamespaceDeclarationList(@namespace, DetachmentOption.Off);
        }

        public virtual void GenerateNamespaceDeclarationList(Namespace @namespace, DetachmentOption detachment)
        {
            GenerateNamespaceContainerList(@namespace);
            GenerateNamespaceTemplates(@namespace);
            GenerateNamespaceTypedefs(@namespace);
            GenerationContext.Scoped(RegistrableGeneratorContext.IsDetach, DetachmentOption.On, () =>
            {
                GenerateNamespaceFunctions(@namespace);
                GenerateNamespaceVariables(@namespace);

                var methods = @namespace.Functions.Where(method => !(method.IsOperator));
                var overloads = methods.GroupBy(m => m.Name);
                foreach (var overload in overloads)
                {
                    GenerateFunctions(@namespace, overload.ToList());
                }

                foreach (var typedef in @namespace.Typedefs)
                {
                    typedef.Visit(this);
                }
            });
        }

        public virtual void GenerateNamespaceContainerList(Namespace @namespace)
        {
            GenerateDeclarationContainerList(@namespace);
        }

        public virtual void GenerateNamespaceTemplates(Namespace @namespace)
        {
        }

        public virtual void GenerateNamespaceTypedefs(Namespace @namespace)
        {
        }

        public virtual void GenerateNamespaceFunctions(Namespace @namespace)
        {
        }

        public virtual void GenerateNamespaceVariables(Namespace @namespace)
        {
            foreach (var variable in @namespace.Variables)
            {
                variable.Visit(this);
            }
        }

        public virtual void GenerateNamespaceEnd(Namespace @namespace)
        {
            //GenerateNamespaceDeclarationList(@namespace, DetachmentOption.On);
        }

        public virtual void GenerateNamespaceGlobalStateRegistration(Namespace @namespace)
        {
        }

        public virtual void GenerateNamespaceFooter(Namespace @namespace)
        {
            Unindent();
            WriteLine("}");
        }

        public virtual void GenerateNamespace(Namespace @namespace)
        {
            GenerateNamespaceDebugName(@namespace);
            GenerateNamespaceHeader(@namespace);
            GenerateNamespaceBegin(@namespace);
            GenerateNamespaceBody(@namespace);
            GenerateNamespaceEnd(@namespace);
            GenerateNamespaceGlobalStateRegistration(@namespace);
            GenerateNamespaceFooter(@namespace);
        }

        public virtual bool CanGenerateNamespace(Namespace @namespace)
        {
            if (AlreadyVisited(@namespace))
            {
                return false;
            }
            else if (@namespace.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return @namespace.IsGenerated;
        }

        public override bool VisitNamespace(Namespace @namespace)
        {
            if (!CanGenerateNamespace(@namespace))
            {
                return false;
            }

            GenerateNamespace(@namespace);

            return true;
        }

        #endregion

        #region Enumeration

        public virtual void GenerateEnumDeclItem(Enumeration enumeration, Enumeration.Item item)
        {
            Write(",");
            NewLine();
            Write($"\"{item.Name}\", {NamingStrategy.GetFullyQualifiedName(item, FQNOption.IgnoreNone)}");
        }

        public virtual void GenerateEnumDeclItemList(Enumeration enumeration, List<Enumeration.Item> items)
        {
            foreach (var item in items)
            {
                GenerateEnumDeclItem(enumeration, item);
            }
        }

        #region Enumeration Anonymous

        public virtual void GenerateEnumDeclAnonymousItem(Enumeration enumeration, Enumeration.Item item)
        {
            WriteLine($"{NamingStrategy.GetRootContextName(GenerationContext)}[\"{item.Name}\"] = {item.OriginalName};");
        }

        public virtual void GenerateEnumDeclAnonymousItemList(Enumeration enumeration, List<Enumeration.Item> items)
        {
            foreach (var item in items)
            {
                GenerateEnumDeclAnonymousItem(enumeration, item);
            }
        }

        public virtual void GenerateEnumDeclAnonymous(Enumeration enumeration)
        {
            GenerateEnumDeclAnonymousItemList(enumeration, enumeration.Items);
        }

        #endregion

        #region Enumeration Non Scoped

        public virtual void GenerateEnumDeclNonScoped(Enumeration enumeration)
        {
            GenerateEnumDeclScoped(enumeration);
            GenerateEnumDeclAnonymous(enumeration);
        }

        #endregion

        #region Enumeration Scoped

        public virtual void GenerateEnumDeclScopedDebugName(Enumeration enumeration)
        {
            WriteLine($"/* {NamingStrategy.GetFullyQualifiedName(enumeration, FQNOption.IgnoreNone)} */");
        }

        public virtual void GenerateEnumDeclScopedHeader(Enumeration enumeration)
        {
            WriteLine("{");
            Indent();
        }

        public virtual void GenerateEnumDeclScopedBindingIdName(Enumeration enumeration)
        {
            WriteLine($"auto {NamingStrategy.GetBindingIdName(enumeration)} = {NamingStrategy.GetBindingIdValue(enumeration, GenerationContext)};");
        }

        public virtual void GenerateEnumDeclScopedBegin(Enumeration enumeration)
        {
            WriteLine($"auto {NamingStrategy.GetBindingName(enumeration)} = {NamingStrategy.GetRootContextName(GenerationContext)}.new_enum<>(");
            Indent();
            Write(NamingStrategy.GetBindingIdName(enumeration));
        }

        public virtual void GenerateEnumDeclScopedItemList(Enumeration enumeration)
        {
            GenerateEnumDeclItemList(enumeration, enumeration.Items);
        }

        public virtual void GenerateEnumDeclScopedBody(Enumeration enumeration)
        {
            GenerateEnumDeclScopedItemList(enumeration);
            GenerateEnumDeclScopedDeclarationList(enumeration, DetachmentOption.Off);
        }

        public virtual void GenerateEnumDeclScopedDeclarationList(Enumeration enumeration, DetachmentOption detachment)
        {
            if (detachment == DetachmentOption.Off)
            {
                GenerateEnumDeclScopedFunctions(enumeration);
                GenerateEnumDeclScopedVariables(enumeration);
            }
            else
            {
                GenerateEnumDeclScopedContainerList(enumeration);
                GenerateEnumDeclScopedTemplates(enumeration);
                GenerateEnumDeclScopedTypedefs(enumeration);
                GenerateEnumDeclScopedFunctions(enumeration);
                GenerateEnumDeclScopedVariables(enumeration);
            }
        }

        public virtual void GenerateEnumDeclScopedContainerList(Enumeration enumeration)
        {
            GenerateDeclarationContainerList(enumeration);
        }

        public virtual void GenerateEnumDeclScopedTemplates(Enumeration enumeration)
        {
        }

        public virtual void GenerateEnumDeclScopedTypedefs(Enumeration enumeration)
        {
        }

        public virtual void GenerateEnumDeclScopedFunctions(Enumeration enumeration)
        {
        }

        public virtual void GenerateEnumDeclScopedVariables(Enumeration enumeration)
        {
        }

        public virtual void GenerateEnumDeclScopedEnd(Enumeration enumeration)
        {
            Unindent();
            NewLine();
            WriteLine(");");
            GenerateEnumDeclScopedDeclarationList(enumeration, DetachmentOption.On);
        }

        public virtual void GenerateEnumDeclScopedGlobalStateRegistration(Enumeration enumeration)
        {
            GenerateDeclarationGlobalStateRegistration(enumeration);
        }

        public virtual void GenerateEnumDeclScopedFooter(Enumeration enumeration)
        {
            Unindent();
            WriteLine("}");
        }

        public virtual void GenerateEnumDeclScoped(Enumeration enumeration)
        {
            GenerateEnumDeclScopedDebugName(enumeration);
            GenerateEnumDeclScopedHeader(enumeration);
            GenerateEnumDeclScopedBindingIdName(enumeration);
            GenerateEnumDeclScopedBegin(enumeration);
            GenerateEnumDeclScopedBody(enumeration);
            GenerateEnumDeclScopedEnd(enumeration);
            GenerateEnumDeclScopedGlobalStateRegistration(enumeration);
            GenerateEnumDeclScopedFooter(enumeration);
        }

        #endregion

        public virtual void GenerateEnumDecl(Enumeration enumeration)
        {
            if (enumeration.IsScoped)
            {
                GenerateEnumDeclScoped(enumeration);
            }
            else
            {
                if (string.IsNullOrEmpty(enumeration.OriginalName))
                {
                    GenerateEnumDeclAnonymous(enumeration);
                }
                else
                {
                    GenerateEnumDeclNonScoped(enumeration);
                }
            }
        }

        public virtual bool CanGenerateEnumDecl(Enumeration enumeration)
        {
            if (AlreadyVisited(enumeration))
            {
                return false;
            }
            else if (enumeration.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return enumeration.IsGenerated;
        }

        public override bool VisitEnumDecl(Enumeration enumeration)
        {
            if (!CanGenerateEnumDecl(enumeration))
            {
                return false;
            }

            GenerateEnumDecl(enumeration);

            return true;
        }

        #endregion

        #region Class

        public virtual void GenerateClassDeclDebugName(Class @class)
        {
            WriteLine($"/* {NamingStrategy.GetFullyQualifiedName(@class, FQNOption.IgnoreNone)} */");
        }

        public virtual void GenerateClassDeclHeader(Class @class)
        {
            WriteLine("{");
            Indent();
        }

        public virtual void GenerateClassDeclBindingIdName(Class @class)
        {
            WriteLine($"auto {NamingStrategy.GetBindingIdName(@class)} = {NamingStrategy.GetBindingIdValue(@class, GenerationContext)};");
        }

        public virtual void GenerateClassDeclBegin(Class @class)
        {
            Write($"auto {NamingStrategy.GetBindingName(@class)} = {NamingStrategy.GetRootContextName(GenerationContext)}.");
            if (TemplateAllowed)
            {
                Write("template ");
            }
            WriteLine($"new_usertype<{NamingStrategy.GetContextualName(@class, GenerationContext, FQNOption.IgnoreNone)}>(");
            Indent();
            Write(NamingStrategy.GetBindingIdName(@class));
        }

        public virtual void GenerateClassDeclBody(Class @class)
        {
            GenerateClassDeclDeclarationList(@class, DetachmentOption.Off);
        }

        public virtual void GenerateClassDeclDeclarationList(Class @class, DetachmentOption detachment)
        {
            if (detachment == DetachmentOption.Off)
            {
                GenerateConstructors(@class, @class.Constructors);

                var methods = @class.Methods.Where(method => !(method.IsConstructor || method.IsDestructor || method.IsOperator));
                var overloads = methods.GroupBy(m => m.Name);
                foreach (var overload in overloads)
                {
                    GenerateMethods(@class, overload.ToList());
                }

                GenerateClassDeclFunctions(@class);
                GenerateClassDeclVariables(@class);
            }
            else
            {
                GenerateClassDeclContainerList(@class);
                GenerateClassDeclTemplates(@class);
                GenerateClassDeclTypedefs(@class);
                GenerateClassDeclFunctions(@class);
                GenerateClassDeclVariables(@class);
            }
        }

        public virtual void GenerateClassDeclContainerList(Class @class)
        {
            GenerateDeclarationContainerList(@class);
        }

        public virtual void GenerateClassDeclTemplates(Class @class)
        {
        }

        public virtual void GenerateClassDeclTypedefs(Class @class)
        {
        }

        public virtual void GenerateClassDeclFunctions(Class @class)
        {
        }

        public virtual void GenerateClassDeclVariables(Class @class)
        {
            foreach (var field in @class.Fields)
            {
                field.Visit(this);
            }
            foreach (var variable in @class.Variables)
            {
                variable.Visit(this);
            }
        }

        public virtual void GenerateClassDeclEnd(Class @class)
        {
            Unindent();
            NewLine();
            WriteLine(");");
            GenerateClassDeclDeclarationList(@class, DetachmentOption.On);
        }

        public virtual void GenerateClassDeclGlobalStateRegistration(Class @class)
        {
            GenerateDeclarationGlobalStateRegistration(@class);
        }

        public virtual void GenerateClassDeclFooter(Class @class)
        {
            Unindent();
            WriteLine("}");
        }

        public virtual void GenerateClassDecl(Class @class)
        {
            GenerateClassDeclDebugName(@class);
            GenerateClassDeclHeader(@class);
            GenerateClassDeclBindingIdName(@class);
            GenerateClassDeclBegin(@class);
            GenerateClassDeclBody(@class);
            GenerateClassDeclEnd(@class);
            GenerateClassDeclGlobalStateRegistration(@class);
            GenerateClassDeclFooter(@class);
        }

        public virtual bool CanGenerateClassDecl(Class @class)
        {
            if (AlreadyVisited(@class))
            {
                return false;
            }
            else if (@class.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            else if (Utils.FindDescribedTemplate(@class) != null)
            {
                return false;
            }
            return @class.IsGenerated;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!CanGenerateClassDecl(@class))
            {
                return false;
            }

            GenerateClassDecl(@class);

            return true;
        }

        #endregion

        #region Field

        #region Field

        public virtual bool CanGenerateFieldDecl(Field field)
        {
            if (AlreadyVisited(field))
            {
                return false;
            }
            else if (field.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return field.IsGenerated;
        }

        public virtual bool GenerateFieldDecl(Field field)
        {
            var isDetach = GenerationContext.PeekIsDetach(DetachmentOption.Off);

            if (isDetach == DetachmentOption.Forced || isDetach == Utils.FindDetachmentOption(field))
            {
                string fieldName = field.Name;
                string fieldNameQuoted = $"\"{fieldName}\"";
                string fieldContextualName = NamingStrategy.GetContextualName(field, GenerationContext, FQNOption.IgnoreNone);

                if (isDetach != DetachmentOption.Off)
                {
                    Write($"{NamingStrategy.GetBindingContext(field, GenerationContext)}[{fieldNameQuoted}] = ");
                }
                else
                {
                    WriteLine(",");
                    Write($"{fieldNameQuoted}, ");
                }
                // TODO : check for typemaps!!!
                {
                    Write($"&{fieldContextualName}");
                }
                if (isDetach != DetachmentOption.Off)
                {
                    WriteLine(";");
                }
            }

            return true;
        }

        #endregion

        #region Bitfield

        public virtual bool CanGenerateFieldDeclBitfield(Field field)
        {
            if (AlreadyVisited(field))
            {
                return false;
            }
            else if (field.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return field.IsGenerated;
        }

        public virtual bool GenerateFieldDeclBitfield(Field field)
        {
            var isDetach = GenerationContext.PeekIsDetach(DetachmentOption.Off);

            if (isDetach == DetachmentOption.Forced || isDetach == Utils.FindDetachmentOption(field))
            {
                string bitfieldOriginalName = field.OriginalName;
                string bitfieldName = field.Name;
                string bitfieldNameQuoted = $"\"{bitfieldName}\"";
                string bitfieldCppContext = NamingStrategy.GetCppContext(field, GenerationContext, FQNOption.IgnoreNone);
                string bitfieldType = field.Type.Visit(new CppTypePrinter(Context));

                if (isDetach != DetachmentOption.Off)
                {
                    Write($"{NamingStrategy.GetBindingContext(field, GenerationContext)}[{bitfieldNameQuoted}] = ");
                }
                else
                {
                    WriteLine(",");
                    Write($"{bitfieldNameQuoted}, ");
                }
                WriteLine("::sol::property(");
                Indent();
                WriteLine($"[]({bitfieldCppContext}& self) {{");
                Indent();
                WriteLine($"return self.{bitfieldOriginalName};");
                Unindent();
                WriteLine("}, ");
                WriteLine($"[]({bitfieldCppContext}& self, {bitfieldType} value) {{");
                Indent();
                WriteLine($"self.{bitfieldOriginalName} = value;");
                Unindent();
                WriteLine("}");
                Unindent();
                Write(")");
                if (isDetach != DetachmentOption.Off)
                {
                    WriteLine(";");
                }
            }

            return true;
        }

        #endregion

        public override bool VisitFieldDecl(Field field)
        {
            if (field.IsBitField)
            {
                if (!CanGenerateFieldDeclBitfield(field))
                {
                    return false;
                }

                return GenerateFieldDeclBitfield(field);
            }
            else
            {
                if (!CanGenerateFieldDecl(field))
                {
                    return false;
                }

                return GenerateFieldDecl(field);
            }
            return false;
        }

        #endregion

        #region Variable

        public virtual bool CanGenerateVariableDecl(Variable variable)
        {
            if (AlreadyVisited(variable))
            {
                return false;
            }
            else if (variable.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return variable.IsGenerated;
        }

        public virtual bool GenerateVariableDecl(Variable variable)
        {
            var isDetach = GenerationContext.PeekIsDetach(DetachmentOption.Off);

            if (isDetach == DetachmentOption.Forced || isDetach == Utils.FindDetachmentOption(variable))
            {
                string variableName = variable.Name;
                string variableNameQuoted = $"\"{variableName}\"";
                string variableBindingContext = NamingStrategy.GetBindingContext(variable, GenerationContext);
                string variableContextualName = NamingStrategy.GetContextualName(variable, GenerationContext, FQNOption.IgnoreNone);
                // TODO: Bug in sol until it gets resolved: we can only bind static class variable by reference.
                if (variable.OriginalNamespace is Class)
                {
                    variableContextualName = $"::std::ref({variableContextualName})";
                }

                // TODO: check for typemaps!!!
                if (isDetach != DetachmentOption.Off)
                {
                    WriteLine($"{variableBindingContext}[{variableNameQuoted}] = ::sol::var({variableContextualName});");
                }
                else
                {
                    WriteLine(",");
                    Write($"{variableNameQuoted}, ::sol::var({variableContextualName})");
                }
            }

            return true;
        }

        public override bool VisitVariableDecl(Variable variable)
        {
            if (!CanGenerateVariableDecl(variable))
            {
                return false;
            }

            return GenerateVariableDecl(variable);
        }

        #endregion

        #region Constructor

        public virtual bool NeedExpansionForConstructors(Class @class, IEnumerable<Method> constructors)
        {
            return false;
        }

        public virtual void GenerateConstructors(Class @class, IEnumerable<Method> constructors)
        {
            var isDetach = GenerationContext.PeekIsDetach();

            List<Method> filteredConstructors = constructors.Where((method) => CanGenerateConstructor(method)).ToList();
            if (filteredConstructors.Any())
            {
                Method constructor = filteredConstructors.First();
                string constructorBindingContext = NamingStrategy.GetBindingContext(constructor, GenerationContext);
                string constructorContextualName = NamingStrategy.GetContextualName(constructor, GenerationContext, FQNOption.IgnoreNone);

                if (isDetach == DetachmentOption.Forced || isDetach == Utils.FindDetachmentOption(constructor))
                {

                    if (isDetach != DetachmentOption.Off)
                    {
                        Write($"{constructorBindingContext}[\"new\"] = ");
                    }
                    else
                    {
                        WriteLine(",");
                        Write($"\"new\", ");
                    }
                    if (NeedExpansionForConstructors(@class, constructors))
                    {
                        Write("::sol::factories(");
                        Indent();
                        for (int i = 0; i < filteredConstructors.Count; i++)
                        {
                            if (i > 0)
                            {
                                Write(",");
                            }
                            NewLine();
                            GenerateConstructor(@class, filteredConstructors[i], true);
                        }
                        Unindent();
                        WriteLine(")");
                    }
                    else
                    {
                        Write("::sol::constructors<");
                        Indent();
                        for (int i = 0; i < filteredConstructors.Count; i++)
                        {
                            if (i > 0)
                            {
                                Write(",");
                            }
                            NewLine();
                            GenerateConstructor(@class, filteredConstructors[i], false);
                        }
                        Unindent();
                        NewLine();
                        Write(">()");
                    }
                    if (isDetach != DetachmentOption.Off)
                    {
                        WriteLine(";");
                    }
                }
            }
        }

        public virtual bool CanGenerateConstructor(Method constructor)
        {
            if (AlreadyVisited(constructor))
            {
                return false;
            }
            else if (constructor.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return constructor.IsGenerated;
        }

        public virtual void GenerateConstructor(Class @class, Method constructor, bool doExpand)
        {
            if (doExpand)
            {
                // TODO: Implement when ready
            }
            else
            {
                Write(NamingStrategy.GetCppContext(constructor, GenerationContext, FQNOption.IgnoreNone));
                Write("(");
                var needsComma = false;
                foreach (var parameter in constructor.Parameters)
                {
                    if (needsComma)
                    {
                        Write(", ");
                    }
                    else
                    {
                        needsComma = true;
                    }
                    Write(parameter.Type.Visit(new CppTypePrinter(Context)));
                }
                if (constructor.IsVariadic)
                {
                    if (needsComma)
                    {
                        Write(", ");
                    }
                    Write("...");
                }
                Write(")");
            }
        }

        #endregion

        #region Function

        public virtual bool NeedExpansionForFunctions(Declaration declaration, IEnumerable<Function> functions)
        {
            return false;
        }

        public virtual void GenerateFunctions(Declaration declaration, IEnumerable<Function> functions)
        {
            var isDetach = GenerationContext.PeekIsDetach();

            List<Function> filteredFunctions = functions.Where((function) => CanGenerateFunction(function)).ToList();
            if (filteredFunctions.Any())
            {
                Function function = filteredFunctions.First();
                string functionName = function.Name;
                string functionNameQuoted = $"\"{functionName}\"";
                string functionBindingContext = NamingStrategy.GetBindingContext(function, GenerationContext);
                string functionContextualName = NamingStrategy.GetContextualName(function, GenerationContext, FQNOption.IgnoreNone);

                if (isDetach == DetachmentOption.Forced || isDetach == Utils.FindDetachmentOption(function))
                {

                    if (isDetach != DetachmentOption.Off)
                    {
                        Write($"{functionBindingContext}[{functionNameQuoted}] = ");
                    }
                    else
                    {
                        WriteLine(",");
                        Write($"{functionNameQuoted}, ");
                    }
                    if (filteredFunctions.Count == 1)
                    {
                        GenerateFunction(declaration, filteredFunctions.First());
                    }
                    else
                    {
                        Write("::sol::overload(");
                        Indent();
                        for (int i = 0; i < filteredFunctions.Count; i++)
                        {
                            if (i > 0)
                            {
                                Write(",");
                            }
                            NewLine();
                            GenerateFunction(declaration, filteredFunctions[i]);
                        }
                        Unindent();
                        NewLine();
                        Write(")");
                    }
                    if (isDetach != DetachmentOption.Off)
                    {
                        WriteLine(";");
                    }
                }
            }
        }

        public virtual bool CanGenerateFunction(Function function)
        {
            if (AlreadyVisited(function))
            {
                return false;
            }
            else if (function.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return function.IsGenerated;
        }

        public virtual void GenerateFunction(Declaration declaration, Function function)
        {
            {
                Write("static_cast<");
                Write(function.ReturnType.Visit(new CppTypePrinter(Context)));
                Write("(");
                Write("*)");
                Write("(");
                var needsComma = false;
                foreach (var parameter in function.Parameters)
                {
                    if (needsComma)
                    {
                        Write(", ");
                    }
                    else
                    {
                        needsComma = true;
                    }
                    Write(parameter.Type.Visit(new CppTypePrinter(Context)));
                }
                if (function.IsVariadic)
                {
                    if (needsComma)
                    {
                        Write(", ");
                    }
                    Write("...");
                }
                Write(")");
                Write(">(&");
                Write(NamingStrategy.GetContextualName(function, GenerationContext, FQNOption.IgnoreNone));
                Write(")");
            }
        }

        #endregion

        #region Method

        public virtual bool NeedExpansionForMethods(Class @class, IEnumerable<Method> methods)
        {
            return false;
        }

        public virtual void GenerateMethods(Class @class, IEnumerable<Method> methods)
        {
            var isDetach = GenerationContext.PeekIsDetach();

            List<Method> filteredMethods = methods.Where((method) => CanGenerateMethod(method)).ToList();
            if (filteredMethods.Any())
            {
                Method method = filteredMethods.First();
                string methodName = method.Name;
                string methodNameQuoted = $"\"{methodName}\"";
                string methodBindingContext = NamingStrategy.GetBindingContext(method, GenerationContext);
                string methodContextualName = NamingStrategy.GetContextualName(method, GenerationContext, FQNOption.IgnoreNone);

                if (isDetach == DetachmentOption.Forced || isDetach == Utils.FindDetachmentOption(method))
                {

                    if (isDetach != DetachmentOption.Off)
                    {
                        Write($"{methodBindingContext}[{methodNameQuoted}] = ");
                    }
                    else
                    {
                        WriteLine(",");
                        Write($"{methodNameQuoted}, ");
                    }
                    if (filteredMethods.Count == 1)
                    {
                        GenerateMethod(@class, filteredMethods.First());
                    }
                    else
                    {
                        Write("::sol::overload(");
                        Indent();
                        for (int i = 0; i < filteredMethods.Count; i++)
                        {
                            if (i > 0)
                            {
                                Write(",");
                            }
                            NewLine();
                            GenerateMethod(@class, filteredMethods[i]);
                        }
                        Unindent();
                        NewLine();
                        Write(")");
                    }
                    if (isDetach != DetachmentOption.Off)
                    {
                        WriteLine(";");
                    }
                }
            }
        }

        public virtual bool CanGenerateMethod(Method method)
        {
            if (AlreadyVisited(method))
            {
                return false;
            }
            else if (method.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return method.IsGenerated;
        }

        public virtual void GenerateMethod(Class @class, Method method)
        {
            {
                Write("static_cast<");
                Write(method.ReturnType.Visit(new CppTypePrinter(Context)));
                Write("(");
                Write("*)");
                Write("(");
                var needsComma = false;
                foreach (var parameter in method.Parameters)
                {
                    if (needsComma)
                    {
                        Write(", ");
                    }
                    else
                    {
                        needsComma = true;
                    }
                    Write(parameter.Type.Visit(new CppTypePrinter(Context)));
                }
                if (method.IsVariadic)
                {
                    if (needsComma)
                    {
                        Write(", ");
                    }
                    Write("...");
                }
                Write(")");
                Write(">(&");
                Write(NamingStrategy.GetContextualName(method, GenerationContext, FQNOption.IgnoreNone));
                Write(")");
            }
        }

        #endregion

        #region Typedef

        public virtual bool CanGenerateTypedefNameDecl(TypedefNameDecl typedef)
        {
            if (AlreadyVisited(typedef))
            {
                return false;
            }
            else if (typedef.Access != AccessSpecifier.Public)
            {
                return false;
            }
            else if (!NonTemplateAllowed)
            {
                return false;
            }
            return typedef.IsGenerated;
        }

        public virtual void GenerateTypedefNameDecl(TypedefNameDecl typedef)
        {
            var type = typedef.Type;
            if (type is TemplateSpecializationType templateSpecializationType)
            {
                string typedefName = typedef.Name;
                string typedefNameQuoted = $"\"{typedefName}\"";
                string typedefRegistrationFunctionName = NamingStrategy.GetFullyQualifiedName(templateSpecializationType.GetClassTemplateSpecialization(), new FQNOption()
                {
                    IgnoreTemplateTypenameKeyword = true
                });
                string typedefBindingContext = NamingStrategy.GetBindingContext(typedef, GenerationContext);
                string typedefRootContextName = NamingStrategy.GetRootContextName(GenerationContext);

                WriteLine($"global{typedefRegistrationFunctionName}{{}}({typedefRootContextName}, {typedefBindingContext}, {typedefNameQuoted}); /* directly */");
            }
        }

        public override bool VisitTypedefNameDecl(TypedefNameDecl typedef)
        {
            if (!CanGenerateTypedefNameDecl(typedef))
            {
                return false;
            }

            GenerateTypedefNameDecl(typedef);

            return true;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return VisitTypedefNameDecl(typedef);
        }

        public override bool VisitTypeAliasDecl(TypeAlias typeAlias)
        {
            return VisitTypedefNameDecl(typeAlias);
        }

        #endregion
    }
}
