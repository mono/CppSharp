using System;
using System.Collections.Generic;

namespace CppSharp.Passes
{
    public class DuplicatedNamesCheckerPass : TranslationUnitPass
    {
        private readonly IDictionary<string, Declaration> names;

        public DuplicatedNamesCheckerPass()
        {
            names = new Dictionary<string, Declaration>();
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.Ignore) return false;

            names.Clear();
            return base.VisitClassDecl(@class);
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (AlreadyVisited(decl))
                return true;

            Visited.Add(decl);

            CheckDuplicate(decl);
            return base.VisitDeclaration(decl);
        }

        public override bool VisitParameterDecl(Parameter parameter)
        {
            return true;
        }

        void CheckDuplicate(Declaration decl)
        {
            if (string.IsNullOrWhiteSpace(decl.Name))
                return;

            Declaration duplicate;

            // If the name is not yet on the map, then add it.
            if (!names.TryGetValue(decl.Name, out duplicate))
            {
                names[decl.Name] = decl;
                return;
            }

            // Else we found a duplicate name and need to change it.
            Console.WriteLine("Found a duplicate named declaration: {0}",
                decl.Name);
        }
    }

    public static class CheckDuplicateNamesExtensions
    {
        public static void CheckDuplicateNames(this PassBuilder builder)
        {
            var pass = new DuplicatedNamesCheckerPass();
            builder.AddPass(pass);
        }
    }
}
