using System;
using Cxxi.Types;

namespace Cxxi.Passes
{
    public class ResolveIncompleteDeclsPass : TranslationUnitPass
    {
        private readonly ITypeMapDatabase typeMapDatabase;

        public ResolveIncompleteDeclsPass(ITypeMapDatabase database)
        {
            typeMapDatabase = database;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.Ignore)
                return false;

            if (!@class.IsIncomplete)
                return false;

            if (@class.CompleteDeclaration != null)
                return false;

            @class.CompleteDeclaration = Library.FindCompleteClass(@class.Name);

            if (@class.CompleteDeclaration == null)
                Console.WriteLine("Unresolved declaration: {0}", @class.Name);

            return base.VisitClassDecl(@class);
        }

        public override bool VisitFieldDecl(Field field)
        {
            var type = field.Type;

            string msg;
            if (!HasInvalidType(type, out msg))
                return false;

            field.ExplicityIgnored = true;

            Console.WriteLine("Field '{0}' was ignored due to {1} type",
                field.Name, msg);

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
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
            }

            return true;
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            string msg;
            if (HasInvalidType(typedef.Type, out msg))
            {
                typedef.ExplicityIgnored = true;
                Console.WriteLine("Typedef '{0}' was ignored due to {1} type",
                    typedef.Name, msg);
                return false;
            }

            return true;
        }

        #region Helpers

        /// <remarks>
        /// Checks if a given type is invalid, which can happen for a number of
        /// reasons: incomplete definitions, being explicitly ignored, or also
        /// by being a type we do not know how to handle.
        /// </remarks>
        bool HasInvalidType(Type type, out string msg)
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

        bool HasInvalidDecl(Declaration decl, out string msg)
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

        bool IsTypeIgnored(Type type)
        {
            var checker = new TypeIgnoreChecker(typeMapDatabase);
            return type.Visit(checker);
        }

        bool IsDeclIgnored(Declaration decl)
        {
            var checker = new TypeIgnoreChecker(typeMapDatabase);
            return decl.Visit(checker);
        }

        #endregion
    }

    public static class ResolveIncompleteDeclsExtensions
    {
        public static void ResolveIncompleteDecls(this PassBuilder builder,
            ITypeMapDatabase database)
        {
            var pass = new ResolveIncompleteDeclsPass(database);
            builder.AddPass(pass);
        }
    }
}
