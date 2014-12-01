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
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TypeMapAttribute : Attribute
    {
        public string Type { get; private set; }
        public GeneratorKind? GeneratorKind { get; set; }
        
        public TypeMapAttribute(string type)
        {
            Type = type;
        }

        public TypeMapAttribute(string type, GeneratorKind generatorKind)
        {
            Type = type;
            GeneratorKind = generatorKind;
        }
    }

    /// <summary>
    /// This is similar to the SWIG type map concept, and allows some
    /// freedom and customization when translating between the source and
    /// target language types.
    /// </summary>
    public class TypeMap
    {
        public Type Type { get; set; }
        public Declaration Declaration { get; set; }
        public ITypeMapDatabase TypeMapDatabase { get; set; }

        public virtual bool IsIgnored
        {
            get { return false; }
        }

        public virtual bool IsValueType
        {
            get { return false; }
        }

        /// <summary>
        /// Determines if the type map performs marshalling or only injects custom code.
        /// </summary>
        public virtual bool DoesMarshalling { get { return true; } }

        #region C# backend

        public virtual Type CSharpSignatureType(CSharpTypePrinterContext ctx)
        {
            return new CILType(typeof(object));
        }

        public virtual string CSharpSignature(CSharpTypePrinterContext ctx)
        {
            throw new NotImplementedException();
        }

        public virtual void CSharpMarshalToNative(MarshalContext ctx)
        {
            throw new NotImplementedException();
        }

        public virtual void CSharpMarshalToManaged(MarshalContext ctx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Used to construct a new instance of the mapped type.
        /// </summary>
        /// <returns></returns>
        public virtual string CSharpConstruct()
        {
            return null;
        }

        #endregion

        #region C++/CLI backend

        public virtual Type CLISignatureType(CLITypePrinterContext ctx)
        {
            return new CILType(typeof(object));
        }

        public virtual string CLISignature(CLITypePrinterContext ctx)
        {
            throw new NotImplementedException();
        }

        public virtual void CLITypeReference(CLITypeReferenceCollector collector, ASTRecord<Declaration> loc)
        {
        }

        public virtual void CLIMarshalToNative(MarshalContext ctx)
        {
            throw new NotImplementedException();
        }

        public virtual void CLIMarshalToManaged(MarshalContext ctx)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public interface ITypeMapDatabase
    {
        bool FindTypeMapRecursive(Type type, out TypeMap typeMap);
        bool FindTypeMap(Type decl, out TypeMap typeMap);
        bool FindTypeMap(Declaration decl, out TypeMap typeMap);
        bool FindTypeMap(string name, out TypeMap typeMap);
    }

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
                var types = assembly.FindDerivedTypes(typeof(TypeMap));
                SetupTypeMaps(types, generatorKind);
            }
        }

        private void SetupTypeMaps(IEnumerable<System.Type> types, GeneratorKind generatorKind)
        {
            foreach (var typeMap in types)
            {
                var attrs = typeMap.GetCustomAttributes(typeof(TypeMapAttribute), true);
                foreach (TypeMapAttribute attr in attrs)
                {
                    if (attr.GeneratorKind == null || attr.GeneratorKind == generatorKind)
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

            var typePrinter = new CppTypePrinter(this)
                {
                    PrintScopeKind = CppTypePrintScopeKind.GlobalQualified,
                    PrintLogicalNames = true
                };

            if (FindTypeMap(decl.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            typePrinter.PrintScopeKind = CppTypePrintScopeKind.Qualified;
            if (FindTypeMap(decl.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            typePrinter.PrintScopeKind = CppTypePrintScopeKind.Local;
            if (FindTypeMap(decl.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            return false;
        }

        public bool FindTypeMap(Type type, out TypeMap typeMap)
        {
            var typePrinter = new CppTypePrinter(this);

            var template = type as TemplateSpecializationType;
            if (template != null)
                return FindTypeMap(template.Template.TemplatedDecl, type,
                    out typeMap);

            if (FindTypeMap(type.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            typePrinter.PrintScopeKind = CppTypePrintScopeKind.Qualified;
            if (FindTypeMap(type.Visit(typePrinter), out typeMap))
            {
                typeMap.Type = type;
                return true;
            }

            return false;
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
