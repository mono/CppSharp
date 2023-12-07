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
        private readonly BindingContext Context;
        private readonly Dictionary<GeneratorKind, Dictionary<Type, TypeMap>> typeMapsCache = new();

        public Dictionary<GeneratorKind, Dictionary<string, TypeMap>> GlobalTypeMaps { get; private set; }
        public Dictionary<string, TypeMap> TypeMaps => TypeMapsByKind(GlobalTypeMaps, Context.Options.GeneratorKind);

        public TypeMapDatabase(BindingContext bindingContext)
        {
            Context = bindingContext;
            GlobalTypeMaps = new Dictionary<GeneratorKind, Dictionary<string, TypeMap>>();
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

        public static Dictionary<T, TypeMap> TypeMapsByKind<T>(Dictionary<GeneratorKind, Dictionary<T, TypeMap>> typeMapsDictionary, GeneratorKind kind)
        {
            if (!typeMapsDictionary.TryGetValue(kind, out Dictionary<T, TypeMap> typeMap))
            {
                typeMap = new Dictionary<T, TypeMap>();
                typeMapsDictionary.Add(kind, typeMap);
            }
            return typeMap;
        }

        public bool FindTypeMap(Type type, out TypeMap typeMap) =>
            FindTypeMap(type, Context.Options.GeneratorKind, out typeMap);

        public bool FindTypeMap(Type type, GeneratorKind kind, out TypeMap typeMap)
        {
            var typeMaps = TypeMapsByKind(typeMapsCache, kind);
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
                    FindTypeMap(specialization, kind, out typeMap))
                    return true;

                if (template.Template.TemplatedDecl != null)
                {
                    if (FindTypeMap(template.Template.TemplatedDecl, kind, out typeMap))
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
                    typePrinter.PushScope(typePrintScopeKind);
                    var typeName = type.Visit(typePrinter);
                    typePrinter.PopScope();
                    if (FindTypeMap(typeName, kind, out typeMap))
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
            FindTypeMap(declaration, Context.Options.GeneratorKind, out typeMap);

        public bool FindTypeMap(Declaration declaration, GeneratorKind kind, out TypeMap typeMap) =>
            FindTypeMap(new TagType(declaration), kind, out typeMap);

        public bool FindTypeMap(string name, GeneratorKind kind, out TypeMap typeMap) =>
            TypeMapsByKind(GlobalTypeMaps, kind).TryGetValue(name, out typeMap) && typeMap.IsEnabled;

        private void SetupTypeMaps(IEnumerable<System.Type> types, BindingContext bindingContext)
        {
            foreach (var type in types)
            {
                var attrs = type.GetCustomAttributes(typeof(TypeMapAttribute), true);
                foreach (TypeMapAttribute attr in attrs)
                {
                    var kind = string.IsNullOrEmpty(attr.GeneratorKindID) ? Context.Options.GeneratorKind : GeneratorKind.FindGeneratorKindByID(attr.GeneratorKindID);
                    var typeMaps = TypeMapsByKind(GlobalTypeMaps, kind);
                    // Custom types won't be overwritten by CppSharp ones.
                    if (!typeMaps.ContainsKey(attr.Type))
                    {
                        var typeMap = (TypeMap)Activator.CreateInstance(type);
                        typeMap.Context = bindingContext;
                        typeMap.TypeMapDatabase = this;
                        typeMaps.Add(attr.Type, typeMap);
                    }
                }
            }
        }
    }
}
