using System;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.C;
using Attribute = System.Attribute;

namespace CppSharp.Types
{
    /// <summary>
    /// Declaration maps allow customization of generated code, either
    /// partially or fully, depending on how its setup.
    /// </summary>
    public abstract class DeclMap
    {
        public BindingContext Context { get; set; }
        public IDeclMapDatabase DeclMapDatabase { get; set; }

        public bool IsEnabled { get; set; } = true;

        public virtual bool IsIgnored => false;

        public Declaration Declaration { get; set; }
        public DeclarationContext DeclarationContext { get; set; }

        public abstract Declaration GetDeclaration();

        public virtual void Generate(CCodeGenerator generator)
        {

        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class DeclMapAttribute : Attribute
    {
        public GeneratorKind GeneratorKind { get; set; }

        public DeclMapAttribute()
        {
        }

        public DeclMapAttribute(GeneratorKind generatorKind)
        {
            GeneratorKind = generatorKind;
        }
    }

    public interface IDeclMapDatabase
    {
        bool FindDeclMap(Declaration declaration, out DeclMap declMap);
    }
}
