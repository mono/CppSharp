using System;
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
            foreach (Method abstractMethod in abstractMethods)
            {
                internalImplementation.Methods.Add(new Method(abstractMethod));
                var @delegate = new TypedefDecl { Name = abstractMethod.Name + "Delegate" };
                var pointerType = new PointerType();
                var functionType = new FunctionType();
                functionType.CallingConvention = abstractMethod.CallingConvention;
                functionType.ReturnType = abstractMethod.OriginalReturnType;
                functionType.Parameters.AddRange(abstractMethod.Parameters.Where(
                    p => p.Kind != ParameterKind.IndirectReturnType));
                pointerType.QualifiedPointee = new QualifiedType(functionType);
                @delegate.QualifiedType = new QualifiedType(pointerType);
                @delegate.IgnoreFlags = abstractMethod.IgnoreFlags;
                internalImplementation.Typedefs.Add(@delegate);
            }
            internalImplementation.Layout = new ClassLayout(@class.Layout);
            FillVTable(@class, abstractMethods, internalImplementation);
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
                    VFTableInfo vTable = vTables[j];
                    var k = vTable.Layout.Components.FindIndex(v => v.Method == abstractMethods[i]);
                    if (k >= 0)
                    {
                        VTableComponent vTableComponent = vTable.Layout.Components[k];
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
                VTableComponent vTableComponent = vTableComponents[j];
                vTableComponent.Declaration = internalImplementation.Methods[i];
                vTableComponents[j] = vTableComponent;
            }
            internalImplementation.Layout.Layout.Components.Clear();
            internalImplementation.Layout.Layout.Components.AddRange(vTableComponents);
        }

        private static List<VTableComponent> GetVTableComponents(Class @class)
        {
            List<VTableComponent> vTableComponents = new List<VTableComponent>(
                @class.Layout.Layout.Components);
            foreach (BaseClassSpecifier @base in @class.Bases)
                vTableComponents.AddRange(GetVTableComponents(@base.Class));
            return vTableComponents;
        }

        private static List<VFTableInfo> GetVTables(Class @class)
        {
            List<VFTableInfo> vTables = new List<VFTableInfo>(
                @class.Layout.VFTables);
            foreach (BaseClassSpecifier @base in @class.Bases)
                vTables.AddRange(GetVTables(@base.Class));
            return vTables;
        }
    }
}
