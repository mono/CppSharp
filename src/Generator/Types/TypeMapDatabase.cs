using System;
using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using Type = CppSharp.AST.Type;

namespace CppSharp.Types
{
    public class TypeMapDatabase : ITypeMapDatabase
    {
        public IDictionary<string, TypeMap> TypeMaps { get; set; }

        public DriverOptions Options { get; }

        public TypeMapDatabase(ASTContext astContext, DriverOptions options)
        {
            TypeMaps = new Dictionary<string, TypeMap>();
            this.Options = options;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var types = assembly.FindDerivedTypes(typeof(TypeMap));
                    SetupTypeMaps(types, astContext);
                }
                catch (System.Reflection.ReflectionTypeLoadException ex)
                {
                    Diagnostics.Error("Error loading type maps from assembly '{0}': {1}",
                        assembly.GetName().Name, ex.Message);
                }
            }
        }

        private void SetupTypeMaps(IEnumerable<System.Type> types, ASTContext astContext)
        {
            foreach (var type in types)
            {
                var attrs = type.GetCustomAttributes(typeof(TypeMapAttribute), true);
                foreach (TypeMapAttribute attr in attrs)
                {
                    if (attr.GeneratorKind == 0 || attr.GeneratorKind == Options.GeneratorKind)
                    {
                        var typeMap = (TypeMap) Activator.CreateInstance(type);
                        typeMap.ASTContext = astContext;
                        typeMap.Options = Options;
                        typeMap.TypeMapDatabase = this;
                        this.TypeMaps[attr.Type] = typeMap;
                    }
                }
            }
        }

        public bool FindTypeMap(Type type, out TypeMap typeMap)
        {
            if (typeMaps.ContainsKey(type))
            {
                typeMap = typeMaps[type];
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
            bool printExtra = desugared.GetPointee() != null &&
                desugared.GetFinalPointee().Desugar().IsPrimitiveType();
            var typePrinter = new CppTypePrinter
            {
                PrintTypeQualifiers = printExtra,
                PrintTypeModifiers = printExtra,
                PrintLogicalNames = true
            };

            foreach (var resolveTypeDefs in new[] { true, false })
                foreach (var typePrintScopeKind in
                    new[] { TypePrintScopeKind.Local, TypePrintScopeKind.Qualified })
                {
                    typePrinter.ResolveTypedefs = resolveTypeDefs;
                    typePrinter.PrintScopeKind = typePrintScopeKind;
                    if (FindTypeMap(type.Visit(typePrinter), out typeMap))
                    {
                        typeMap.Type = type;
                        typeMaps[type] = typeMap;
                        return true;
                    }
                }

            typeMap = null;
            var typedef = type as TypedefType;
            return typedef != null && FindTypeMap(typedef.Declaration.Type, out typeMap);
        }

        public bool FindTypeMap(Declaration declaration, out TypeMap typeMap) =>
            FindTypeMap(new TagType(declaration), out typeMap);

        private bool FindTypeMap(string name, out TypeMap typeMap) =>
            TypeMaps.TryGetValue(name, out typeMap) && typeMap.IsEnabled;

        private Dictionary<Type, TypeMap> typeMaps = new Dictionary<Type, TypeMap>();
    }
}
