using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace CppSharp.Types
{
    public class DeclMapDatabase : IDeclMapDatabase
    {
        public IDictionary<Declaration, DeclMap> DeclMaps { get; set; }

        public DeclMapDatabase(BindingContext bindingContext)
        {
            DeclMaps = new Dictionary<Declaration, DeclMap>();
            SetupDeclMaps(bindingContext);
        }

        private void SetupDeclMaps(BindingContext bindingContext)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.FindDerivedTypes(typeof(DeclMap));
                    SetupDeclMaps(types, bindingContext);
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    Diagnostics.Error("Error loading decl maps from assembly '{0}': {1}",
                        assembly.GetName().Name, ex.Message);
                }
            }
        }

        private void SetupDeclMaps(IEnumerable<System.Type> types,
            BindingContext bindingContext)
        {
            foreach (var type in types)
            {
                var attrs = type.GetCustomAttributes(typeof(DeclMapAttribute), true);
                foreach (DeclMapAttribute attr in attrs)
                {
                    if (attr.GeneratorKind == null ||
                        attr.GeneratorKind == bindingContext.Options.GeneratorKind)
                    {
                        var declMap = (DeclMap)Activator.CreateInstance(type);
                        declMap.Context = bindingContext;
                        declMap.DeclMapDatabase = this;

                        var decl = declMap.GetDeclaration();
                        if (decl == null)
                            continue;

                        DeclMaps[decl] = declMap;
                    }
                }
            }
        }

        public bool FindDeclMap(Declaration decl, out DeclMap declMap)
        {
            if (decl.DeclMap != null)
            {
                declMap = decl.DeclMap as DeclMap;
                return true;
            }

            // Looks up the decl in the cache map.
            if (declMaps.ContainsKey(decl))
            {
                declMap = declMaps[decl];
                return declMap.IsEnabled;
            }

            var foundDecl = DeclMaps.Keys.FirstOrDefault(d => d.OriginalPtr == decl.OriginalPtr);
            if (foundDecl != null)
            {
                declMap = DeclMaps[foundDecl];
                declMaps[decl] = declMap;
                return declMap.IsEnabled;
            }

            declMap = null;
            return false;
        }

        private readonly Dictionary<Declaration, DeclMap> declMaps =
            new Dictionary<Declaration, DeclMap>();
    }
}
