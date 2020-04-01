using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.AST;
using CppSharp.Generators.CLI;
using CppSharp.Generators.Cpp;
using CppSharp.Generators.CSharp;
using Attribute = System.Attribute;
using Type = CppSharp.AST.Type;

namespace CppSharp.Types
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TypeMapAttribute : Attribute
    {
        public string Type { get; }
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
        public BindingContext Context { get; set; }
        public ITypeMapDatabase TypeMapDatabase { get; set; }

        public bool IsEnabled { get; set; } = true;

        public virtual bool IsIgnored => false;

        public virtual bool IsValueType => false;

        /// <summary>
        /// Determines if the type map performs marshalling or only injects custom code.
        /// </summary>
        public virtual bool DoesMarshalling => true;

        public virtual Type SignatureType(GeneratorKind kind, TypePrinterContext ctx)
        {
            switch (kind)
            {
            case GeneratorKind.CPlusPlus:
                return CppSignatureType(ctx);
            case GeneratorKind.CLI:
                return CLISignatureType(ctx);
            case GeneratorKind.CSharp:
                return CSharpSignatureType(ctx);
            default:
                throw new System.NotImplementedException();
            }
        }

        public virtual void MarshalToNative(GeneratorKind kind, MarshalContext ctx)
        {
            switch (kind)
            {
            case GeneratorKind.CPlusPlus:
                CppMarshalToNative(ctx);
                return;
            case GeneratorKind.CLI:
                CLIMarshalToNative(ctx);
                return;
            case GeneratorKind.CSharp:
                CSharpMarshalToNative(ctx as CSharpMarshalContext);
                return;
            default:
                throw new System.NotImplementedException();
            }
        }

        public virtual void MarshalToManaged(GeneratorKind kind, MarshalContext ctx)
        {
            switch (kind)
            {
            case GeneratorKind.CPlusPlus:
                CppMarshalToManaged(ctx);
                return;
            case GeneratorKind.CLI:
                CLIMarshalToManaged(ctx);
                return;
            case GeneratorKind.CSharp:
                CSharpMarshalToManaged(ctx as CSharpMarshalContext);
                return;
            default:
                throw new System.NotImplementedException();
            }
        }

        #region C# backend

        public virtual Type CSharpSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(object));
        }

        public virtual void CSharpMarshalToNative(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public virtual void CSharpMarshalToManaged(CSharpMarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
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

        public virtual Type CLISignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(object));
        }

        public virtual void CLITypeReference(CLITypeReferenceCollector collector, ASTRecord<Declaration> loc)
        {
        }

        public virtual void CLIMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public virtual void CLIMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }

        #endregion

        #region C++ backend

        public virtual Type CppSignatureType(TypePrinterContext ctx)
        {
            return new CILType(typeof(object));
        }

        public virtual void CppTypeReference(CLITypeReference collector, ASTRecord<Declaration> record)
        {
            throw new NotImplementedException();
        }

        public virtual void CppMarshalToNative(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.Parameter.Name);
        }

        public virtual void CppMarshalToManaged(MarshalContext ctx)
        {
            ctx.Return.Write(ctx.ReturnVarName);
        }

        #endregion 
    }

    public interface ITypeMapDatabase
    {
        bool FindTypeMap(Type decl, out TypeMap typeMap);
        bool FindTypeMap(Declaration declaration, out TypeMap typeMap);
    }
}
