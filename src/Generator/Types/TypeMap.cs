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
        public GeneratorKind GeneratorKind { get; set; }
        
        public TypeMapAttribute(string type) : this(type, 0)
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

        public virtual Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(object));
        }

        public virtual string CSharpSignature(TypePrinterContext ctx)
        {
            throw new NotImplementedException();
        }

        public virtual void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            throw new NotImplementedException();
        }

        public virtual void CSharpMarshalToManaged(CSharpMarshalContext ctx)
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


}
