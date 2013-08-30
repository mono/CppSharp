using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Utils;

namespace CppSharp.Passes
{
    public class AbstractImplementationsPass : TranslationUnitPass
    {
        private readonly List<Class> classes = new List<Class>();

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            bool result = base.VisitTranslationUnit(unit);
            unit.Classes.AddRange(classes);
            classes.Clear();
            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            if (!VisitDeclaration(@class) || AlreadyVisited(@class))
                return false;

            if (@class.IsAbstract)
                @classes.Add(AddInternalImplementation(@class));
            return base.VisitClassDecl(@class);
        }

        private static Class AddInternalImplementation(Class @class)
        {
            var internalImplementation = new Class();
            internalImplementation.Name = @class.Name + "Internal";
            internalImplementation.Access = AccessSpecifier.Private;
            internalImplementation.Namespace = @class.Namespace;
            var @base = new BaseClassSpecifier { Type = new TagType(@class) };
            internalImplementation.Bases.Add(@base);
            var abstractMethods = GetAbstractMethods(@class);
            var overriddenMethods = GetOverriddenMethods(@class);
            var parameterTypeComparer = new ParameterTypeComparer();
            for (int i = abstractMethods.Count - 1; i >= 0; i--)
            {
                Method @abstract = abstractMethods[i];
                if (overriddenMethods.Find(m => m.Name == @abstract.Name &&
                    m.ReturnType.Type == @abstract.ReturnType.Type &&
                    m.Parameters.Count == @abstract.Parameters.Count &&
                    m.Parameters.SequenceEqual(@abstract.Parameters, parameterTypeComparer)) != null)
                {
                    abstractMethods.RemoveAt(i);
                }
            }
            internalImplementation.Methods.AddRange(from abstractMethod in abstractMethods
                                                    select new Method(abstractMethod));
            internalImplementation.Layout = new ClassLayout(@class.Layout);
            var vTableComponents = GetVTableComponents(@class);
            for (int i = 0; i < abstractMethods.Count; i++)
            {
                var vTableComponent = vTableComponents.Find(v => v.Method == abstractMethods[i]);
                VTableComponent copy = new VTableComponent();
                copy.Kind = vTableComponent.Kind;
                copy.Offset = vTableComponent.Offset;
                copy.Declaration = internalImplementation.Methods[i];
                vTableComponents[vTableComponents.IndexOf(vTableComponent)] = copy;
            }
            internalImplementation.Layout.Layout.Components.Clear();
            internalImplementation.Layout.Layout.Components.AddRange(vTableComponents);
            internalImplementation.Layout.VFTables.AddRange(@class.Layout.VFTables);
            foreach (Method method in internalImplementation.Methods)
            {
                method.IsPure = false;
                method.IsOverride = true;
                method.IsSynthetized = true;
            }
            return internalImplementation;
        }

        private static List<Method> GetAbstractMethods(Class @class)
        {
            var abstractMethods = @class.Methods.Where(m => m.IsPure).ToList();
            foreach (BaseClassSpecifier @base in @class.Bases)
                abstractMethods.AddRange(GetAbstractMethods(@base.Class));
            return abstractMethods;
        }

        private static List<Method> GetOverriddenMethods(Class @class)
        {
            var abstractMethods = @class.Methods.Where(m => m.IsOverride).ToList();
            foreach (BaseClassSpecifier @base in @class.Bases)
                abstractMethods.AddRange(GetOverriddenMethods(@base.Class));
            return abstractMethods;
        }

        private static List<VTableComponent> GetVTableComponents(Class @class)
        {
            List<VTableComponent> vTableComponents = new List<VTableComponent>(
                @class.Layout.Layout.Components);
            foreach (BaseClassSpecifier @base in @class.Bases)
                vTableComponents.AddRange(GetVTableComponents(@base.Class));
            return vTableComponents;
        }
    }
}
