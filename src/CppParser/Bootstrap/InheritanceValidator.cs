using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp
{
    class InheritanceValidator
    {
        private readonly HashSet<string> _processedClasses = new();

        public void ValidateInheritance(IEnumerable<Declaration> declarations)
        {
            foreach (var decl in declarations.OfType<Class>())
            {
                if (!_processedClasses.Add(decl.Name))
                    continue;

                ValidateClassInheritance(decl);
            }
        }

        private static void ValidateClassInheritance(Class @class)
        {
            if (@class.Bases.Count == 0)
                return;

            foreach (var @base in @class.Bases)
            {
                if (@base.Class == null)
                    continue;

                ValidateClassInheritance(@base.Class);
            }

            // Ensure base class properties don't conflict
            foreach (var property in @class.Properties)
            {
                if (@class.GetBaseProperty(property) != null)
                {
                    property.ExplicitlyIgnore();
                }
            }

            // Handle method overrides
            foreach (var method in @class.Methods)
            {
                if (@class.GetBaseMethod(method) != null)
                {
                    method.ExplicitlyIgnore();
                }
            }
        }
    }
}