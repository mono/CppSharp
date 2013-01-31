using System;
using System.Collections.Generic;
using System.Linq;
using Cxxi.Types;

namespace Cxxi.Passes
{
    public class CleanInvalidDeclNamesPass : TranslationUnitPass
    {
        private int uniqueName;
        private TypeRefsVisitor typeRefs;

        public CleanInvalidDeclNamesPass()
        {
            typeRefs = new TypeRefsVisitor();
        }

        public override bool ProcessUnit(TranslationUnit unit)
        {
            if (unit.ExplicityIgnored)
                return false;

            if (unit.IsSystemHeader)
                return false;

            typeRefs = new TypeRefsVisitor();
            ProcessNamespace(unit);
            unit.ForwardReferences = typeRefs.ForwardReferences.ToList();

            return true;
        }

        private void ProcessNamespace(Namespace @namespace)
        {
            ProcessDeclarations(@namespace.Enums);
            ProcessFunctions(@namespace.Functions);
            ProcessClasses(@namespace.Classes);
            ProcessTypedefs(@namespace, @namespace.Typedefs);

            foreach (var inner in @namespace.Namespaces)
                ProcessNamespace(inner);
        }

        public override bool ProcessDeclaration(Declaration decl)
        {
            decl.Visit(typeRefs);

            // Generate a new name if the decl still does not have a name
            if (string.IsNullOrWhiteSpace(decl.Name))
                decl.Name = string.Format("_{0}", uniqueName++);

            StringHelpers.CleanupText(ref decl.DebugText);
            return true;
        }

        private void ProcessDeclarations<T>(IEnumerable<T> decls)
            where T : Declaration
        {
            foreach (T decl in decls)
                ProcessDeclaration(decl);
        }

        private void ProcessClasses(List<Class> classes)
        {
            ProcessDeclarations(classes);

            foreach (var @class in classes)
            {
                ProcessFields(@class.Fields);
                ProcessMethods(@class.Methods);
            }
        }

        private void ProcessFields(List<Field> fields)
        {
            ProcessDeclarations(fields);

            foreach (var field in fields)
                ProcessField(field);
        }

        private void ProcessMethods(List<Method> methods)
        {
            ProcessDeclarations(methods);

            foreach (var method in methods)
                ProcessFunction(method);
        }

        private void ProcessFunctions(List<Function> functions)
        {
            ProcessDeclarations(functions);

            foreach (var function in functions)
                ProcessFunction(function);
        }

        public override bool ProcessFunction(Function function)
        {
            foreach (var param in function.Parameters)
                ProcessDeclaration(param);

            return true;
        }

        private void ProcessTypedefs(Namespace @namespace, List<TypedefDecl> typedefs)
        {
            ProcessDeclarations(typedefs);

            foreach (var typedef in typedefs)
            {
                var @class = @namespace.FindClass(typedef.Name);

                // Clang will walk the typedef'd tag decl and the typedef decl,
                // so we ignore the class and process just the typedef.

                if (@class != null)
                    typedef.ExplicityIgnored = true;

                if (typedef.Type == null)
                    typedef.ExplicityIgnored = true;
            }
        }

        private void CheckEnumName(Enumeration @enum)
        {
            // If we still do not have a valid name, then try to guess one
            // based on the enum value names.

            if (!String.IsNullOrWhiteSpace(@enum.Name))
                return;

            var prefix = @enum.Items.Select(item => item.Name)
                .ToArray().CommonPrefix();

            // Try a simple heuristic to make sure we end up with a valid name.
            if (prefix.Length < 3)
                return;

            prefix = prefix.Trim().Trim(new char[] { '_' });
            @enum.Name = prefix;
        }

        public override bool ProcessEnum(Enumeration @enum)
        {
            CheckEnumName(@enum);
            return base.ProcessEnum(@enum);
        }
    }

    public static class CleanInvalidDeclNamesExtensions
    {
        public static void CleanInvalidDeclNames(this PassBuilder builder)
        {
            var pass = new CleanInvalidDeclNamesPass();
            builder.AddPass(pass);
        }
    }
}

