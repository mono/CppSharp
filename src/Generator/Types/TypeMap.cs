using System;
using System.Collections.Generic;
using System.Reflection;
using Cxxi.Generators.CLI;

namespace Cxxi.Types
{
    public class MarshalContext
    {
        public MarshalContext(Driver driver)
        {
            Driver = driver;
            SupportBefore = new TextGenerator();
            SupportAfter = new TextGenerator();
            Return = new TextGenerator();
        }

        public Driver Driver { get; private set; }

        public CLIMarshalNativeToManagedPrinter MarshalToManaged;
        public CLIMarshalManagedToNativePrinter MarshalToNative;

        public TextGenerator SupportBefore { get; private set; }
        public TextGenerator SupportAfter { get; private set; }
        public TextGenerator Return { get; private set; }

        public string ReturnVarName { get; set; }
        public Type ReturnType { get; set; }

        public string ArgName { get; set; }
        public Parameter Parameter { get; set; }
        public int ParameterIndex { get; set; }
        public Function Function { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TypeMapAttribute : Attribute
    {
        public string Type { get; private set; }
        
        public TypeMapAttribute(string type)
        {
            Type = type;
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

        public virtual bool IsIgnored
        {
            get { return false; }
        }

        public virtual bool IsValueType
        {
            get { return false; }
        }

        #region C# backend

        public virtual string CSharpSignature()
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

        #endregion

        #region C++/CLI backend

        public virtual string CLISignature()
        {
            throw new NotImplementedException();
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
        bool FindTypeMap(Type type, out TypeMap typeMap);
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

        public void SetupTypeMaps()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in loadedAssemblies)
            {
                var types = assembly.FindDerivedTypes(typeof(TypeMap));
                SetupTypeMaps(types);
            }
        }

        private void SetupTypeMaps(IEnumerable<System.Type> types)
        {
            foreach (var typeMap in types)
            {
                var attrs = typeMap.GetCustomAttributes(typeof(TypeMapAttribute));
                if (attrs == null) continue;

                foreach (TypeMapAttribute attr in attrs)
                {
                    Console.WriteLine("Found typemap: {0}", attr.Type);
                    TypeMaps[attr.Type] = typeMap;
                }
            }
        }

        public bool FindTypeMap(Declaration decl, out TypeMap typeMap)
        {
            return FindTypeMap(decl.QualifiedOriginalName, out typeMap);
        }

        public bool FindTypeMap(Type type, out TypeMap typeMap)
        {
            var typePrinter = new CppTypePrinter(this);
            var output = type.Visit(typePrinter);

            if (FindTypeMap(output, out typeMap))
                return true;

            // Try to strip the global scope resolution operator.
            if (output.StartsWith("::"))
                output = output.Substring(2);

            if (FindTypeMap(output, out typeMap))
                return true;

            return false;
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
            return true;
        }
    }
}
