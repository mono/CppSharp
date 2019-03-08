﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;

namespace CppSharp.Passes
{
    public class CleanInvalidDeclNamesPass : TranslationUnitPass
    {
        private int uniqueName;
        private int globalUniqueName;

        string CheckName(Declaration decl)
        {
            string name = decl.Name;

            // If the decl is a class, then generate a unique name.
            if (string.IsNullOrWhiteSpace(name) && decl is Class)
                return $"_{globalUniqueName++}";

            // Generate a new name if the decl still does not have a name
            if (string.IsNullOrWhiteSpace(name))
                return $"_{uniqueName++}";

            // Clean up the item name if the first digit is not a valid name.
            if (char.IsNumber(name[0]))
                return '_' + name;

            // TODO: Fix this to not need per-generator code.
            var units = new List<TranslationUnit> { new TranslationUnit() };
            if (Options.IsCLIGenerator)
                return new CLIHeaders(Context, units).SafeIdentifier(name);

            return new CSharpSources(Context, units).SafeIdentifier(name);
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (!base.VisitDeclaration(decl))
                return false;

            // Do not clean up namespace names since it can mess up with the
            // names of anonymous or the global namespace.
            if (decl is Namespace)
                return true;

            // types with empty names are assumed to be private
            if (decl is Class && string.IsNullOrWhiteSpace(decl.Name))
            {
                decl.Name = CheckName(decl);
                return true;
            }

            var function = decl as Function;
            var method = function as Method;
            if ((function == null || !function.IsOperator) && !(decl is Enumeration) &&
                (method == null || method.Kind == CXXMethodKind.Normal))
                decl.Name = CheckName(decl);

            return true;
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            return VisitDeclaration(parameter);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            // Remove the class from the Visited list so the base.VisitClassDecl call
            // below does not early out before processing the declaration.
            // We have to do this because we need to call VisitDeclaration first
            // so that the per-class unique naming logic works.
            Visited.Remove(@class);

            uniqueName = 0;
            base.VisitClassDecl(@class);

            if (!@class.IsDependent)
                foreach (var field in @class.Layout.Fields.Where(f => string.IsNullOrEmpty(f.Name)))
                    field.Name = @class.Name == "_" ? "__" : "_";

            if (@class is ClassTemplateSpecialization &&
                !(from c in @class.Namespace.Classes
                  where c.Name == @class.Name && !(@class is ClassTemplateSpecialization) &&
                      c != ((ClassTemplateSpecialization) @class).TemplatedDecl.TemplatedClass
                  select c).Any())
                return true;

            if (@class.Namespace.Classes.Any(d => d != @class && d.Name == @class.Name))
            {
                // we need the new name in each iteration so no point in StringBuilder
                var name = @class.Name;
                do
                {
                    name += '_';
                } while (@class.Namespace.Name == name ||
                    @class.Classes.Any(d => d != @class && d.Name == name));
                @class.Name = name;
            }

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            uniqueName = 0;
            var ret = base.VisitFunctionDecl(function);

            return ret;
        }

        public override bool VisitEvent(Event @event)
        {
            uniqueName = 0;
            var ret = base.VisitEvent(@event);

            return ret;
        }

        public override bool VisitFunctionType(FunctionType type,
            TypeQualifiers quals)
        {
            uniqueName = 0;
            var ret = base.VisitFunctionType(type, quals);

            return ret;
        }

        private void CheckEnumName(Enumeration @enum)
        {
            // If we still do not have a valid name, then try to guess one
            // based on the enum value names.

            if (!string.IsNullOrWhiteSpace(@enum.Name))
                return;

            var prefix = @enum.Items.Select(item => item.Name)
                .ToArray().CommonPrefix();

            // Try a simple heuristic to make sure we end up with a valid name.
            if (prefix.Length < 3)
            {
                @enum.Name = CheckName(@enum);
                return;
            }

            var prefixBuilder = new StringBuilder(prefix);
            prefixBuilder.TrimUnderscores();
            while (@enum.Namespace.Enums.Any(e => e != @enum &&
                e.Name == prefixBuilder.ToString()))
                prefixBuilder.Append('_');
            @enum.Name = prefixBuilder.ToString();
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!base.VisitEnumDecl(@enum))
                return false;

            CheckEnumName(@enum);
            return true;
        }

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            if (!base.VisitEnumItemDecl(item))
                return false;

            item.Name = CheckName(item);
            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!base.VisitFieldDecl(field))
                return false;

            if (field.Class.Fields.Count(c => c.Name.Equals(field.Name)) > 1)
            {
                StringBuilder str = new StringBuilder();
                str.Append(field.Name);
                do
                {
                    str.Append('_');
                } while (field.Class.Fields.Any(c => c.Name.Equals(str.ToString())));
                field.Name = str.ToString();
            }
            return true;
        }
    }
}

