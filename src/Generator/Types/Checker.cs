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

        public override bool ProcessLibrary(Library library)
        {
            if (string.IsNullOrEmpty(library.Name))
                library.Name = "";

            // Process everything in the global namespace for now.
            foreach (var unit in library.TranslationUnits)
            {
                if (unit.ExplicityIgnored)
                    continue;

                if (unit.IsSystemHeader)
                    continue;

                typeRefs = new TypeRefsVisitor();
                ProcessNamespace(unit);
                unit.ForwardReferences = typeRefs.ForwardReferences.ToList();
            }

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
            {
                var type = field.Type;

                string msg;
                if (!HasInvalidType(type, out msg))
                    continue;

                field.ExplicityIgnored = true;
                Console.WriteLine("Field '{0}' was ignored due to {1} type",
                    field.Name,  msg);
            }
        }

        public override bool ProcessFunction(Function function)
        {
            var ret = function.ReturnType;

            string msg;
            if (HasInvalidType(ret, out msg))
            {
                function.ExplicityIgnored = true;
                Console.WriteLine("Function '{0}' was ignored due to {1} return decl",
                    function.Name, msg);
            }

            foreach (var param in function.Parameters)
            {
                if (HasInvalidDecl(param, out msg))
                {
                    function.ExplicityIgnored = true;
                    Console.WriteLine("Function '{0}' was ignored due to {1} param",
                        function.Name, msg);
                }

                ProcessDeclaration(param);
            }

            return true;
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

        #region Helpers

        /// <remarks>
        /// Checks if a given type is invalid, which can happen for a number of
        /// reasons: incomplete definitions, being explicitly ignored, or also
        /// by being a type we do not know how to handle.
        /// </remarks>
        static bool HasInvalidType(Type type, out string msg)
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

            msg = null;
            return false;
        }

        static bool HasInvalidDecl(Declaration decl, out string msg)
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

        static bool IsTypeComplete(Type type)
        {
            var checker = new TypeCompletionChecker();
            return type.Visit(checker);
        }

        static bool IsDeclComplete(Declaration decl)
        {
            var checker = new TypeCompletionChecker();
            return decl.Visit(checker);
        }

        static bool IsTypeIgnored(Type type)
        {
            var checker = new TypeIgnoreChecker();
            return type.Visit(checker);
        }

        static bool IsDeclIgnored(Declaration decl)
        {
            var checker = new TypeIgnoreChecker();
            return decl.Visit(checker);
        }

        #endregion
    }
}

