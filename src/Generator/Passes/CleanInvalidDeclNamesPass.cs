using System.Collections.Generic;
using System.Linq;
using System.Text;
using CppSharp.AST;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using CppSharp.Generators;

namespace CppSharp.Passes
{
    public class CleanInvalidDeclNamesPass : TranslationUnitPass
    {
        public override bool VisitASTContext(ASTContext context)
        {
            // TODO: Fix this to not need per-generator code.
            generator = Options.IsCLIGenerator ?
               new CLIHeaders(Context, new List<TranslationUnit>()) :
               (CodeGenerator) new CSharpSources(Context);
            return base.VisitASTContext(context);
        }

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
                int order = 0;
                foreach (var field in @class.Layout.Fields)
                    field.Name = CheckName(field.Name, ref order);
            }

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!base.VisitEnumDecl(@enum))
                return false;

            CheckChildrenNames(@enum.Items,
                string.IsNullOrEmpty(@enum.Name) ? 1 : 0);
            CheckEnumName(@enum);
            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            if (!base.VisitFunctionDecl(function))
                return false;

            CheckChildrenNames(function.Parameters);
            return true;
        }

        public override bool VisitDeclarationContext(DeclarationContext context)
        {
            if (!base.VisitDeclarationContext(context))
                return false;

            DeclarationContext currentContext = context;
            int order = -1;
            while (currentContext != null)
            {
                order++;
                currentContext = currentContext.Namespace;
            }
            CheckChildrenNames(context.Declarations, ref order);

            var @class = context as Class;
            if (@class != null)
                CheckChildrenNames(@class.Fields, order);

            return true;
        }

        public override bool VisitFunctionType(FunctionType function, TypeQualifiers quals)
        {
            if (!base.VisitFunctionType(function, quals))
                return false;

            CheckChildrenNames(function.Parameters);
            return true;
        }

        private void CheckChildrenNames(IEnumerable<Declaration> children, int order = 0) =>
            CheckChildrenNames(children, ref order);

        private void CheckChildrenNames(IEnumerable<Declaration> children, ref int order)
        {
            foreach (var child in children)
                child.Name = CheckName(child.Name, ref order);
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
                int order = @enum.Namespace.Enums.Count(e => e != @enum &&
                    string.IsNullOrEmpty(e.Name));
                @enum.Name = CheckName(@enum.Name, ref order);
                return;
            }

            var prefixBuilder = new StringBuilder(prefix);
            prefixBuilder.TrimUnderscores();
            while (@enum.Namespace.Enums.Any(e => e != @enum &&
                e.Name == prefixBuilder.ToString()))
                prefixBuilder.Append('_');
            @enum.Name = prefixBuilder.ToString();
        }

        private string CheckName(string name, ref int order)
        {
            // Generate a new name if the decl still does not have a name
            if (string.IsNullOrWhiteSpace(name))
                return $"_{order++}";

            // Clean up the item name if the first digit is not a valid name.
            if (char.IsNumber(name[0]))
                return '_' + name;

            return generator.SafeIdentifier(name);
        }

        private CodeGenerator generator;
    }
}

