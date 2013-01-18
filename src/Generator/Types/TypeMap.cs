using System;
using System.Collections.Generic;
using System.Reflection;

namespace Cxxi.Types
{
    public class MarshalContext
    {
        public string ArgName { get; set; }
        public string ReturnVarName { get; set; }
        public Type ReturnType { get; set; }
        public Parameter Parameter { get; set; }
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

        public virtual string Signature()
        {
            throw new NotImplementedException();
        }

        public virtual string MarshalToNative(MarshalContext ctx)
        {
            throw new NotImplementedException();
        }

        public virtual string MarshalFromNative(MarshalContext ctx)
        {
            throw new NotImplementedException();
        }
    }

    public interface ITypeMapDatabase
    {
        bool FindTypeMap(Declaration decl, out TypeMap typeMap);
        bool FindTypeMap(string name, out TypeMap typeMap);
    }

    public class TypeDatabase : ITypeMapDatabase
    {
        public IDictionary<string, System.Type> TypeMaps { get; set; }

        public TypeDatabase()
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
                var attrs = typeMap.GetCustomAttributes<TypeMapAttribute>();
                if (attrs == null) continue;

                foreach (var attr in attrs)
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

        public bool FindTypeMap(string name, out TypeMap typeMap)
        {
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
