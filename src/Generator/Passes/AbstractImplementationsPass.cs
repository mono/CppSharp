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

        private Class AddInternalImplementation(Class @class)
        {
            var internalImpl = GetInternalImpl(@class);

            var abstractMethods = GetRelevantAbstractMethods(@class);

            foreach (var abstractMethod in abstractMethods)
            {
                internalImpl.Methods.Add(new Method(abstractMethod));
                var @delegate = new TypedefDecl { Name = abstractMethod.Name + "Delegate" };
                @delegate.QualifiedType = abstractMethod.GetFunctionType();
                @delegate.IgnoreFlags = abstractMethod.IgnoreFlags;
                internalImpl.Typedefs.Add(@delegate);
            }

            internalImpl.Layout = new ClassLayout(@class.Layout);
            FillVTable(@class, abstractMethods, internalImpl);

            foreach (var method in internalImpl.Methods)
            {
                method.IsPure = false;
                method.IsOverride = true;
                method.IsSynthetized = true;
            }
            return internalImpl;
        }

        private static Class GetInternalImpl(Declaration @class)
        {
            var internalImpl = new Class();
            internalImpl.Name = @class.Name + "Internal";
            internalImpl.Access = AccessSpecifier.Private;
            internalImpl.Namespace = @class.Namespace;
            var @base = new BaseClassSpecifier { Type = new TagType(@class) };
            internalImpl.Bases.Add(@base);
            return internalImpl;
        }

        private static List<Method> GetRelevantAbstractMethods(Class @class)
        {
            var abstractMethods = GetAbstractMethods(@class);
            var overriddenMethods = GetOverriddenMethods(@class);
            var paramTypeCmp = new ParameterTypeComparer();
            for (int i = abstractMethods.Count - 1; i >= 0; i--)
            {
                var @abstract = abstractMethods[i];
                if (overriddenMethods.Find(m => m.Name == @abstract.Name &&
                    m.ReturnType.Type == @abstract.ReturnType.Type &&
                    m.Parameters.Count == @abstract.Parameters.Count &&
                    m.Parameters.SequenceEqual(@abstract.Parameters, paramTypeCmp)) != null)
                {
                    abstractMethods.RemoveAt(i);
                }
            }
            return abstractMethods;
        }

        private static List<Method> GetAbstractMethods(Class @class)
        {
            var abstractMethods = @class.Methods.Where(m => m.IsPure).ToList();
            foreach (var @base in @class.Bases)
                abstractMethods.AddRange(GetAbstractMethods(@base.Class));
            return abstractMethods;
        }

        private static List<Method> GetOverriddenMethods(Class @class)
        {
            var abstractMethods = @class.Methods.Where(m => m.IsOverride).ToList();
            foreach (var @base in @class.Bases)
                abstractMethods.AddRange(GetOverriddenMethods(@base.Class));
            return abstractMethods;
        }

        private void FillVTable(Class @class, IList<Method> abstractMethods, Class internalImplementation)
        {
            switch (Driver.Options.Abi)
            {
                case CppAbi.Microsoft:
                    CreateVTableMS(@class, abstractMethods, internalImplementation);
                    break;
                default:
                    CreateVTableItanium(@class, abstractMethods, internalImplementation);
                    break;
            }
        }

        private static void CreateVTableMS(Class @class,
            IList<Method> abstractMethods, Class internalImplementation)
        {
            var vTables = GetVTables(@class);
            for (int i = 0; i < abstractMethods.Count; i++)
            {
                for (int j = 0; j < vTables.Count; j++)
                {
                    var vTable = vTables[j];
                    var k = vTable.Layout.Components.FindIndex(v => v.Method == abstractMethods[i]);
                    if (k >= 0)
                    {
                        var vTableComponent = vTable.Layout.Components[k];
                        vTableComponent.Declaration = internalImplementation.Methods[i];
                        vTable.Layout.Components[k] = vTableComponent;
                        vTables[j] = vTable;
                    }
                }
            }
            internalImplementation.Layout.VFTables.Clear();
            internalImplementation.Layout.VFTables.AddRange(vTables);
        }

        private static void CreateVTableItanium(Class @class,
            IList<Method> abstractMethods, Class internalImplementation)
        {
            var vTableComponents = GetVTableComponents(@class);
            for (int i = 0; i < abstractMethods.Count; i++)
            {
                var j = vTableComponents.FindIndex(v => v.Method == abstractMethods[i]);
                var vTableComponent = vTableComponents[j];
                vTableComponent.Declaration = internalImplementation.Methods[i];
                vTableComponents[j] = vTableComponent;
            }
            internalImplementation.Layout.Layout.Components.Clear();
            internalImplementation.Layout.Layout.Components.AddRange(vTableComponents);
        }

        private static List<VTableComponent> GetVTableComponents(Class @class)
        {
            var vTableComponents = new List<VTableComponent>(
                @class.Layout.Layout.Components);
            foreach (var @base in @class.Bases)
                vTableComponents.AddRange(GetVTableComponents(@base.Class));
            return vTableComponents;
        }

        private static List<VFTableInfo> GetVTables(Class @class)
        {
            var vTables = new List<VFTableInfo>(
                @class.Layout.VFTables);
            foreach (var @base in @class.Bases)
                vTables.AddRange(GetVTables(@base.Class));
            return vTables;
        }
    }
}
