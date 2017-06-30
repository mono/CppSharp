using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.AST;
using CppSharp.Generators.CLI;
using CppSharp.Generators.CSharp;
using Attribute = System.Attribute;
using Type = CppSharp.AST.Type;

namespace CppSharp.Types
{
    public class TypeMapDatabase : ITypeMapDatabase
    {
        public IDictionary<string, System.Type> TypeMaps { get; set; }

        public TypeMapDatabase()
        {
            TypeMaps = new Dictionary<string, System.Type>();
        }

        public void SetupTypeMaps(GeneratorKind generatorKind)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in loadedAssemblies)
            {
                try
                {
                    var types = assembly.FindDerivedTypes(typeof(TypeMap));
                    SetupTypeMaps(types, generatorKind);
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    Diagnostics.Error("Error loading type maps from assembly '{0}': {1}",
                        assembly.GetName().Name, ex.Message);
                }
            }
        }

        private void SetupTypeMaps(IEnumerable<System.Type> types, GeneratorKind generatorKind)
        {
            foreach (var typeMap in types)
            {
                var attrs = typeMap.GetCustomAttributes(typeof(TypeMapAttribute), true);
                foreach (TypeMapAttribute attr in attrs)
                {
                    if (attr.GeneratorKind == 0 || attr.GeneratorKind == generatorKind)
                    {
                        TypeMaps[attr.Type] = typeMap;
                    }
                }
            }
        }

        public bool FindTypeMap(Declaration decl, Type type, out TypeMap typeMap)
        {
            // We try to find type maps from the most qualified to less qualified
            // types. Example: '::std::vector', 'std::vector' and 'vector'

            var typePrinter = new CppTypePrinter { PrintLogicalNames = true };

            if (FindTypeMap(decl.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            typePrinter.PrintScopeKind = TypePrintScopeKind.Qualified;
            if (FindTypeMap(decl.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            typePrinter.ResolveTypedefs = true;
            if (FindTypeMap(decl.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }
            typePrinter.ResolveTypedefs = false;

            typePrinter.PrintScopeKind = TypePrintScopeKind.Local;
            if (FindTypeMap(decl.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            var specialization = decl as ClassTemplateSpecialization;
            if (specialization != null &&
                FindTypeMap(specialization.TemplatedDecl.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            var typedef = decl as TypedefDecl;
            return typedef != null && FindTypeMap(typedef.Type, out typeMap);
        }

        public bool FindTypeMap(Type type, out TypeMap typeMap)
        {
            var typePrinter = new CppTypePrinter
            {
                PrintTypeQualifiers = false,
                PrintTypeModifiers = false,
                PrintLogicalNames = true
            };

            var template = type as TemplateSpecializationType;
            if (template != null)
            {
                var specialization = template.GetClassTemplateSpecialization();
                if (specialization != null && FindTypeMap(specialization, type, out typeMap))
                    return true;
                if (template.Template.TemplatedDecl != null)
                    return FindTypeMap(template.Template.TemplatedDecl, type,
                        out typeMap);
            }

            typePrinter.PrintScopeKind = TypePrintScopeKind.Local;
            if (FindTypeMap(type.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            typePrinter.PrintScopeKind = TypePrintScopeKind.Qualified;
            if (FindTypeMap(type.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            var typedef = type as TypedefType;
            return typedef != null && FindTypeMap(typedef.Declaration, type, out typeMap);
        }

        public bool FindTypeMap(Declaration decl, out TypeMap typeMap)
        {
            return FindTypeMap(decl, null, out typeMap);
        }

        public bool FindTypeMapRecursive(Type type, out TypeMap typeMap)
        {
            while (true)
            {
                if (FindTypeMap(type, out typeMap))
                    return true;

                var desugaredType = type.Desugar();
                if (desugaredType == type)
                    return false;

                type = desugaredType;
            }
        }

        public bool FindTypeMap(string name, out TypeMap typeMap)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                typeMap = null;
                return false;
            }

            System.Type type;
            TypeMaps.TryGetValue(name, out type);

            if (type == null)
            {
                typeMap = null;
                return false;
            }

            typeMap = (TypeMap)Activator.CreateInstance(type);
            typeMap.TypeMapDatabase = this;

            return true;
        }
    }
}
