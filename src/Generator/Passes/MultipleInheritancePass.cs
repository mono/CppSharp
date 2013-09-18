using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace CppSharp.Passes
{
    public class MultipleInheritancePass : TranslationUnitPass
    {
        /// <summary>
        /// Collects all interfaces in a unit to be added at the end because the unit cannot be changed while it's being iterated though.
        /// </summary>
        private readonly Dictionary<Class, Class> interfaces = new Dictionary<Class, Class>();

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            bool result = base.VisitTranslationUnit(unit);
            foreach (var @interface in interfaces)
                @interface.Key.Namespace.Classes.Add(@interface.Value);
            interfaces.Clear();
            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            for (int i = 1; i < @class.Bases.Count; i++)
            {
                var @base = @class.Bases[i].Class;
                var name = "I" + @base.Name;
                if (!interfaces.ContainsKey(@base) && @base.Namespace.Classes.All(c => c.Name != name))
                {
                    Class @interface = new Class { Name = name, Namespace = @class.Namespace };
                    @interface.IsAbstract = true;
                    @interface.Methods.AddRange(@class.Methods);
                    @interface.Properties.AddRange(@class.Properties);
                    @interface.Events.AddRange(@class.Events);
                    @interfaces.Add(@class, @interface);
                }
            }
            return base.VisitClassDecl(@class);
        }
    }
}
