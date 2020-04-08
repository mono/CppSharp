using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.C;
using Type = CppSharp.AST.Type;

namespace CppSharp.Types
{
    public class TypeMapDatabase : ITypeMapDatabase
    {
        public IDictionary<string, TypeMap> TypeMaps { get; set; }
        private readonly BindingContext Context;

        public TypeMapDatabase(BindingContext bindingContext)
        {
            Context = bindingContext;
            TypeMaps = new Dictionary<string, TypeMap>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.FindDerivedTypes(typeof(TypeMap));
                    SetupTypeMaps(types, bindingContext);
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    Diagnostics.Error("Error loading type maps from assembly '{0}': {1}",
                        assembly.GetName().Name, ex.Message);
                }
            }
        }

        private void SetupTypeMaps(IEnumerable<System.Type> types,
            BindingContext bindingContext)
        {
            foreach (var type in types)
            {
                var attrs = type.GetCustomAttributes(typeof(TypeMapAttribute), true);
                foreach (TypeMapAttribute attr in attrs)
                {
                    if (attr.GeneratorKind == 0 ||
                        attr.GeneratorKind == bindingContext.Options.GeneratorKind)
                    {
                        var typeMap = (TypeMap) Activator.CreateInstance(type);
                        typeMap.Context = bindingContext;
                        typeMap.TypeMapDatabase = this;
                        this.TypeMaps[attr.Type] = typeMap;
                    }
                }
            }
        }

        public bool FindTypeMap(Type type, out TypeMap typeMap)
        {
            // Looks up the type in the cache map.
            if (typeMaps.ContainsKey(type))
            {
                typeMap = typeMaps[type];
                typeMap.Type = type;
                return typeMap.IsEnabled;
            }

            var template = type as TemplateSpecializationType;
            if (template != null)
            {
                var specialization = template.GetClassTemplateSpecialization();
                if (specialization != null &&
                    FindTypeMap(specialization, out typeMap))
                    return true;

                if (template.Template.TemplatedDecl != null)
                {
                    if (FindTypeMap(template.Template.TemplatedDecl,
                        out typeMap))
                    {
                        typeMap.Type = type;
                        return true;
                    }

                    return false;
                }
            }

            Type desugared = type.Desugar();
            desugared = (desugared.GetFinalPointee() ?? desugared).Desugar();

            bool printExtra = desugared.IsPrimitiveType() ||
                (desugared.GetFinalPointee() ?? desugared).Desugar().IsPrimitiveType();

            var typePrinter = new CppTypePrinter(Context)
            {
                ResolveTypeMaps = false,
                PrintTypeQualifiers = printExtra,
                PrintTypeModifiers = printExtra,
                PrintLogicalNames = true
            };

            typePrinter.PushContext(TypePrinterContextKind.Native);

            foreach (var resolveTypeDefs in new[] { false, true })
            {
                foreach (var typePrintScopeKind in
                    new[] { TypePrintScopeKind.Local, TypePrintScopeKind.Qualified })
                {
                    typePrinter.ResolveTypedefs = resolveTypeDefs;
                    typePrinter.ScopeKind = typePrintScopeKind;
                    var typeName = type.Visit(typePrinter);
                    if (FindTypeMap(typeName, out typeMap))
                    {
                        typeMap.Type = type;
                        typeMaps[type] = typeMap;
                        return true;
                    }
                }
            }

            typeMap = null;
            return false;
        }

        public bool FindTypeMap(Declaration declaration, out TypeMap typeMap) =>
            FindTypeMap(new TagType(declaration), out typeMap);

        public bool FindTypeMap(string name, out TypeMap typeMap) =>
            TypeMaps.TryGetValue(name, out typeMap) && typeMap.IsEnabled;

        private Dictionary<Type, TypeMap> typeMaps = new Dictionary<Type, TypeMap>();
    }
}
