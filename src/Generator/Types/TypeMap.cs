using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.AST;
using CppSharp.Generators.CLI;
using Attribute = System.Attribute;
using Type = CppSharp.AST.Type;

namespace CppSharp.Types
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TypeMapAttribute : Attribute
    {
        public string Type { get; }
        public string GeneratorKindID { get; set; }

        public TypeMapAttribute(string type) : this(type, null)
        {
            Type = type;
        }

        public TypeMapAttribute(string type, string generatorKindID)
        {
            Type = type;
            GeneratorKindID = generatorKindID;
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
        public BindingContext Context { get; set; }
        public ITypeMapDatabase TypeMapDatabase { get; set; }

        public bool IsEnabled { get; set; } = true;

        public virtual bool IsIgnored => false;

        public virtual bool IsValueType => false;

        /// <summary>
        /// Determines if the type map performs marshalling or only injects custom code.
        /// </summary>
        public virtual bool DoesMarshalling => true;

        public virtual Type SignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(object));
        }

        public virtual void MarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public virtual void MarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }

        #region C# backend

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

        public virtual void CLITypeReference(CLITypeReferenceCollector collector, ASTRecord<Declaration> loc)
        {
        }

        #endregion
    }

    public interface ITypeMapDatabase
    {
        bool FindTypeMap(Type decl, out TypeMap typeMap);
        bool FindTypeMap(Type decl, GeneratorKind kind, out TypeMap typeMap);
        bool FindTypeMap(Declaration declaration, out TypeMap typeMap);
        bool FindTypeMap(Declaration declaration, GeneratorKind kind, out TypeMap typeMap);
    }
}
