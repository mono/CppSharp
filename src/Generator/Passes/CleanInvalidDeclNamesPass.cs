using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class CleanInvalidDeclNamesPass : TranslationUnitPass
    {
        public CleanInvalidDeclNamesPass() => VisitOptions.ClearFlags(
            VisitFlags.ClassBases | VisitFlags.EventParameters |
            VisitFlags.FunctionReturnType | VisitFlags.TemplateArguments);

        public override bool VisitClassDecl(Class @class)
        {
            if (!base.VisitClassDecl(@class))
                return false;

            if (Options.GeneratorKind == GeneratorKind.CLI &&
                string.IsNullOrEmpty(@class.OriginalName))
            {
                @class.ExplicitlyIgnore();
                return true;
            }

            if (@class.Layout != null)
            {
                var fields = @class.Layout.Fields.Where(
                    f => string.IsNullOrEmpty(f.Name)).ToList();

                for (int i = 0; i < fields.Count; i++)
                    fields[i].Name = $"_{i}";
            }

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!base.VisitDeclaration(field))
                return false;

            field.Type.Visit(this, field.QualifiedType.Qualifiers);

            Class @class;
            if (field.Type.TryGetClass(out @class) && string.IsNullOrEmpty(@class.OriginalName))
            {
                @class.Name = field.Name;

                foreach (var item in @class.Fields.Where(f => f.Name == @class.Name))
                    Rename(item);

                Rename(field);
            }

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!base.VisitEnumDecl(@enum))
                return false;

            Check(@enum.Items);

            foreach (Enumeration.Item item in @enum.Items.Where(i => char.IsNumber(i.Name[0])))
                item.Name = $"_{item.Name}";
            CheckEnumName(@enum);
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            Check(function.Parameters);
            return true;
        }

        public override bool VisitDeclarationContext(DeclarationContext context)
        {
            if (!base.VisitDeclarationContext(context))
                return false;

            Check(context.Declarations);

            var @class = context as Class;
            if (@class != null)
                Check(@class.Fields);

            return true;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            if (!base.VisitFunctionType(function, quals))
                return false;

            Check(function.Parameters);
            return true;
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
                return;

            var prefixBuilder = new StringBuilder(prefix);
            prefixBuilder.TrimUnderscores();
            while (@enum.Namespace.Enums.Any(e => e != @enum &&
                e.Name == prefixBuilder.ToString()))
                prefixBuilder.Append('_');
            @enum.Name = prefixBuilder.ToString();
        }

        private static void Rename(Field field)
        {
            var nameBuilder = new StringBuilder(field.Name);
            for (int i = 0; i < nameBuilder.Length; i++)
            {
                if (!char.IsUpper(nameBuilder[i]))
                    break;
                nameBuilder[i] = char.ToLowerInvariant(nameBuilder[i]);
            }
            field.Name = nameBuilder.ToString();
        }

        private static void Check(IEnumerable<Declaration> decls)
        {
            var anonymousDecls = decls.Where(p => string.IsNullOrEmpty(p.Name)).ToList();
            for (int i = 0; i < anonymousDecls.Count; i++)
            {
                var anonymousDecl = anonymousDecls[i];

                if (anonymousDecl.Namespace != null && anonymousDecl.Namespace.Name == anonymousDecl.Name)
                    anonymousDecl.Name = $"__{i}";
                else
                    anonymousDecl.Name = $"_{i}";
            }
        }
    }
}

