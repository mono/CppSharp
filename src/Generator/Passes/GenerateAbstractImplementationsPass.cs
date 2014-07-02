using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppAbi = CppSharp.Parser.AST.CppAbi;

namespace CppSharp.Passes
{
    /// <summary>
    /// This pass generates internal classes that implement abstract classes.
    /// When the return type of a function is abstract, these internal
    /// classes provide since the real type cannot be resolved while binding
    /// an allocatable class that supports proper polymorphism.
    /// </summary>
    public class GenerateAbstractImplementationsPass : TranslationUnitPass
    {
        /// <summary>
        /// Collects all internal implementations in a unit to be added at
        /// the end because the unit cannot be changed while it's being
        /// iterated though.
        /// </summary>
        private readonly List<Class> internalImpls = new List<Class>();

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            var result = base.VisitTranslationUnit(unit);
            foreach (var internalImpl in internalImpls)
                if (internalImpl.Namespace != null)
                    internalImpl.Namespace.Classes.Add(internalImpl);
                else
                    unit.Classes.AddRange(internalImpls);

            internalImpls.Clear();
            return result;
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            if (!VisitDeclaration(@class))
                return false;

            if (@class.IsAbstract)
            {
                foreach (var ctor in from ctor in @class.Constructors
                                     where ctor.Access == AccessSpecifier.Public
                                     select ctor)
                    ctor.Access = AccessSpecifier.Protected;
                internalImpls.Add(AddInternalImplementation(@class));
            }

            return base.VisitClassDecl(@class);
        }

        private Class AddInternalImplementation(Class @class)
        {
            var internalImpl = GetInternalImpl(@class);

            var abstractMethods = GetRelevantAbstractMethods(@class);

            internalImpl.Methods.AddRange(
                from method in abstractMethods
                select new Method(method)
                {
                    Namespace = internalImpl,
                    OriginalFunction = method,
                    IsPure = false,
                    IsOverride = true,
                    SynthKind = FunctionSynthKind.AbstractImplCall
                });

            internalImpl.Layout = new ClassLayout(@class.Layout);
            FillVTable(@class, abstractMethods, internalImpl);

            return internalImpl;
        }

        private static Class GetInternalImpl(Declaration @class)
        {
            var internalImpl = new Class
                                {
                                    Name = @class.Name + "Internal",
                                    Access = AccessSpecifier.Private,
                                    Namespace = @class.Namespace
                                };

            var @base = new BaseClassSpecifier { Type = new TagType(@class) };
            internalImpl.Bases.Add(@base);

            return internalImpl;
        }

        private static List<Method> GetRelevantAbstractMethods(Class @class)
        {
            var abstractMethods = GetAbstractMethods(@class);
            var overriddenMethods = GetOverriddenMethods(@class);
            var paramTypeCmp = new ParameterTypeComparer();

            for (var i = abstractMethods.Count - 1; i >= 0; i--)
            {
                var @abstract = abstractMethods[i];
                if (overriddenMethods.Find(m => m.Name == @abstract.Name &&
                    m.ReturnType == @abstract.ReturnType &&
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
            var vtables = GetVTables(@class);
            for (int i = 0; i < abstractMethods.Count; i++)
            {
                for (int j = 0; j < vtables.Count; j++)
                {
                    var vTable = vtables[j];
                    var k = vTable.Layout.Components.FindIndex(v => v.Method == abstractMethods[i]);
                    if (k >= 0)
                    {
                        var vTableComponent = vTable.Layout.Components[k];
                        vTableComponent.Declaration = internalImplementation.Methods[i];
                        vTable.Layout.Components[k] = vTableComponent;
                        vtables[j] = vTable;
                    }
                }
            }

            internalImplementation.Layout.VFTables.Clear();
            internalImplementation.Layout.VFTables.AddRange(vtables);
        }

        private static void CreateVTableItanium(Class @class,
            IList<Method> abstractMethods, Class internalImplementation)
        {
            var vtableComponents = GetVTableComponents(@class);
            for (var i = 0; i < abstractMethods.Count; i++)
            {
                var j = vtableComponents.FindIndex(v => v.Method == abstractMethods[i]);
                var vtableComponent = vtableComponents[j];
                vtableComponent.Declaration = internalImplementation.Methods[i];
                vtableComponents[j] = vtableComponent;
            }

            internalImplementation.Layout.Layout.Components.Clear();
            internalImplementation.Layout.Layout.Components.AddRange(vtableComponents);
        }

        private static List<VTableComponent> GetVTableComponents(Class @class)
        {
            var vtableComponents = new List<VTableComponent>(
                @class.Layout.Layout.Components);

            foreach (var @base in @class.Bases)
                vtableComponents.AddRange(GetVTableComponents(@base.Class));

            return vtableComponents;
        }

        private static List<VFTableInfo> GetVTables(Class @class)
        {
            var vtables = new List<VFTableInfo>(
                @class.Layout.VFTables);

            foreach (var @base in @class.Bases)
                vtables.AddRange(GetVTables(@base.Class));

            return vtables;
        }
    }
}
