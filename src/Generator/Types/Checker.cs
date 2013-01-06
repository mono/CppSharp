using System;
using System.Collections.Generic;
using System.Linq;

namespace Cxxi.Passes
{
    public class Preprocess : TranslationUnitPass
    {
        private int uniqueName;
        private TypeRefsVisitor typeRefs;

        public Preprocess()
        {
            typeRefs = new TypeRefsVisitor();
        }

        public void ProcessLibrary(Library Library)
        {
            if (string.IsNullOrEmpty(Library.Name))
                Library.Name = "";

            // Process everything in the global namespace for now.
            foreach (var unit in Library.TranslationUnits)
            {
                if (unit.ExplicityIgnored)
                    continue;

                if (unit.IsSystemHeader)
                    continue;

                typeRefs = new TypeRefsVisitor();
                ProcessNamespace(unit);
                unit.ForwardReferences = typeRefs.ForwardReferences.ToList();
            }
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

        private void ProcessDeclarations<T>(IEnumerable<T> decls) where T : Declaration
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
            {
                var type = field.Type;
                if (type == null || !IsTypeComplete(type) || IsTypeIgnored(type))
                {
                    field.ExplicityIgnored = true;
                    Console.WriteLine(
                        "Field '{0}' was ignored due to unknown / ignored type",
                        field.Name);
                }
            }
        }

        public override bool ProcessFunction(Function function)
        {
            var ret = function.ReturnType;

            if (ret == null || !IsTypeComplete(ret) || IsTypeIgnored(ret))
            {
                function.ExplicityIgnored = true;
                Console.WriteLine(
                    "Function '{0}' was ignored due to unknown / ignored return decl",
                    function.Name);
            }

            foreach (var param in function.Parameters)
            {
                if (param == null || !IsDeclComplete(param) || IsDeclIgnored(param))
                {
                    function.ExplicityIgnored = true;
                    Console.WriteLine(
                        "Function '{0}' was ignored due to unknown / ignored param",
                        function.Name);
                }

                ProcessDeclaration(param);
            }

            return true;
        }

        private static bool IsTypeComplete(Type type)
        {
            var checker = new TypeCompletionChecker();
            return type.Visit(checker);
        }

        private static bool IsDeclComplete(Declaration decl)
        {
            var checker = new TypeCompletionChecker();
            return decl.Visit(checker);
        }

        private static bool IsTypeIgnored(Type type)
        {
            var checker = new TypeIgnoreChecker();
            return type.Visit(checker);
        }

        private static bool IsDeclIgnored(Declaration decl)
        {
            var checker = new TypeIgnoreChecker();
            return decl.Visit(checker);
        }

        public override bool ProcessMethod(Method method)
        {
            return ProcessFunction(method);
        }

        private void ProcessMethods(List<Method> methods)
        {
            ProcessDeclarations(methods);

            foreach (var method in methods)
                ProcessFunction(method);
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

        private void ProcessFunctions(List<Function> functions)
        {
            ProcessDeclarations(functions);

            foreach (var function in functions)
                ProcessFunction(function);
        }
    }
}

