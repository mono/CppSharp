using System;
using System.Collections.Generic;
using System.IO;

namespace CppSharp.Generators.CLI
{
    public struct CLIForwardReference
    {
        public Declaration Declaration;
        public Namespace Namespace;
        public string Text;
    }

    public class CLIForwardReferencePrinter : IDeclVisitor<bool>
    {
        public readonly IList<string> Includes;
        public readonly IList<CLIForwardReference> Refs;
        private readonly TypeRefsVisitor TypeRefs;
        private TypeReference currentTypeReference;

        public CLIForwardReferencePrinter(TypeRefsVisitor typeRefs)
        {
            Includes = new List<string>();
            Refs = new List<CLIForwardReference>();
            TypeRefs = typeRefs;
        }

        public void Process()
        {
            foreach (var typeRef in TypeRefs.References)
            {
                currentTypeReference = typeRef;
                typeRef.Declaration.Visit(this);
            }

            foreach (var baseClass in TypeRefs.Bases)
                VisitBaseClass(baseClass);
        }

        public void VisitBaseClass(Class @class)
        {
            if (@class.IsIncomplete)
                @class = @class.CompleteDeclaration as Class;

            if (@class == null)
                return;

            Includes.Add(GetHeaderFromDecl(@class));
        }

        public bool VisitDeclaration(Declaration decl)
        {
            throw new NotImplementedException();
        }

        public bool VisitClassDecl(Class @class)
        {
            var completeDecl = @class.CompleteDeclaration as Class;
            if (@class.IsIncomplete && completeDecl != null)
                return VisitClassDecl(completeDecl);

            if (@class.IsValueType)
            {
                Refs.Add(new CLIForwardReference()
                    {
                        Declaration = @class,
                        Namespace = currentTypeReference.Namespace,
                        Text = string.Format("value struct {0};", @class.Name)
                    });

                return true;
            }

            Refs.Add(new CLIForwardReference()
            {
                Declaration = @class,
                Namespace = currentTypeReference.Namespace,
                Text = string.Format("ref class {0};", @class.Name)
            });

            return true;
        }

        public bool VisitFieldDecl(Field field)
        {
            Class @class;
            if (field.Type.IsTagDecl(out @class))
            {
                if (@class.IsValueType)
                    Includes.Add(GetHeaderFromDecl(@class));
                else
                    VisitClassDecl(@class);

                return true;
            }

            Enumeration @enum;
            if (field.Type.IsTagDecl(out @enum))
                return VisitEnumDecl(@enum);

            Includes.Add(GetHeaderFromDecl(field));
            return true;
        }

        public bool VisitFunctionDecl(Function function)
        {
            throw new NotImplementedException();
        }

        public bool VisitMethodDecl(Method method)
        {
            throw new NotImplementedException();
        }

        public bool VisitParameterDecl(Parameter parameter)
        {
            throw new NotImplementedException();
        }

        public string GetHeaderFromDecl(Declaration decl)
        {
            var @namespace = decl.Namespace;
            var unit = @namespace.TranslationUnit;

            if (unit.Ignore)
                return string.Empty;

            if (unit.IsSystemHeader)
                return string.Empty;

            return Path.GetFileNameWithoutExtension(unit.FileName);
        }

        public bool VisitTypedefDecl(TypedefDecl typedef)
        {
            FunctionType function;
            if (typedef.Type.IsPointerTo<FunctionType>(out function))
            {
                Includes.Add(GetHeaderFromDecl(typedef));
                return true;
            }

            throw new NotImplementedException();
        }

        public bool VisitEnumDecl(Enumeration @enum)
        {
            Includes.Add(GetHeaderFromDecl(@enum));

            if (@enum.Type.IsPrimitiveType(PrimitiveType.Int32))
            {
                Refs.Add(new CLIForwardReference()
                {
                    Declaration = @enum,
                    Namespace = currentTypeReference.Namespace,
                    Text = string.Format("enum struct {0};", @enum.Name)
                });

                return true;
            }

            Refs.Add(new CLIForwardReference()
            {
                Declaration = @enum,
                Namespace = currentTypeReference.Namespace,
                Text = string.Format("enum struct {0} : {1};", @enum.Name, @enum.Type)
            });
            return true;
        }

        public bool VisitVariableDecl(Variable variable)
        {
            throw new NotImplementedException();
        }

        public bool VisitClassTemplateDecl(ClassTemplate template)
        {
            throw new NotImplementedException();
        }

        public bool VisitFunctionTemplateDecl(FunctionTemplate template)
        {
            throw new NotImplementedException();
        }

        public bool VisitMacroDefinition(MacroDefinition macro)
        {
            throw new NotImplementedException();
        }

        public bool VisitNamespace(Namespace @namespace)
        {
            throw new NotImplementedException();
        }

        public bool VisitEvent(Event @event)
        {
            throw new NotImplementedException();
        }

        public bool VisitProperty(Property property)
        {
            throw new NotImplementedException();
        }
    }
}