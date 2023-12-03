using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.AST;
using CppSharp.Generators.C;
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

        public virtual Type SignatureType(TypePrinterContext ctx, GeneratorKind kind = null)
        {
            kind ??= Context.Options.GeneratorKind;
            switch (kind)
            {
                case var _ when ReferenceEquals(kind, GeneratorKind.C):
                case var _ when ReferenceEquals(kind, GeneratorKind.CPlusPlus):
                    return CppSignatureType(ctx);
                case var _ when ReferenceEquals(kind, GeneratorKind.CLI):
                    return CLISignatureType(ctx);
                case var _ when ReferenceEquals(kind, GeneratorKind.CSharp):
                    return CSharpSignatureType(ctx);
                default:
                    throw new System.NotImplementedException();
            }
        }

        public virtual void MarshalToNative(MarshalContext ctx, GeneratorKind kind = null)
        {
            kind ??= Context.Options.GeneratorKind;
            switch (kind)
            {
                case var _ when ReferenceEquals(kind, GeneratorKind.C):
                case var _ when ReferenceEquals(kind, GeneratorKind.CPlusPlus):
                    CppMarshalToNative(ctx);
                    return;
                case var _ when ReferenceEquals(kind, GeneratorKind.CLI):
                    CLIMarshalToNative(ctx);
                    return;
                case var _ when ReferenceEquals(kind, GeneratorKind.CSharp):
                    CSharpMarshalToNative(ctx as CSharpMarshalContext);
                    return;
                default:
                    throw new System.NotImplementedException();
            }
        }

        public virtual void MarshalToManaged(MarshalContext ctx, GeneratorKind kind = null)
        {
            kind ??= Context.Options.GeneratorKind;
            switch (kind)
            {
                case var _ when ReferenceEquals(kind, GeneratorKind.C):
                case var _ when ReferenceEquals(kind, GeneratorKind.CPlusPlus):
                    CppMarshalToManaged(ctx);
                    return;
                case var _ when ReferenceEquals(kind, GeneratorKind.CLI):
                    CLIMarshalToManaged(ctx);
                    return;
                case var _ when ReferenceEquals(kind, GeneratorKind.CSharp):
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
