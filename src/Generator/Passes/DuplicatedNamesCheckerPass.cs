using System;
using System.Collections.Generic;

namespace Cxxi.Passes
{
    public class DuplicatedNamesCheckerPass : TranslationUnitPass
    {
        private readonly IDictionary<string, Declaration> names;

        public DuplicatedNamesCheckerPass()
        {
            names = new Dictionary<string, Declaration>();
        }

        public override bool ProcessClass(Class @class)
        {
            if (@class.Ignore) return false;

            names.Clear();

            foreach (var baseClass in @class.Bases)
                ProcessClass(baseClass.Class);

            CheckDuplicates(@class.Fields);
            CheckDuplicates(@class.Methods);
            CheckDuplicates(@class.Properties);

            return true;
        }

        void CheckDuplicates(IEnumerable<Declaration> decls)
        {
            foreach (var decl in decls)
            {
                if (decl.Ignore) continue;
                CheckDuplicate(decl);
            }
        }

        void CheckDuplicate(Declaration decl)
        {
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
