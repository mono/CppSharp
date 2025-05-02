using CppSharp.Generators.C;

namespace CppSharp.Generators.Registrable
{
    public enum ImportedClassTemplateMode
    {
        Direct,
        Indirect,
        Import
    }

    public abstract class RegistrableGeneratorOptions
    {
        public delegate string Delegate(string name);

        public virtual Generator Generator { get; set; }

        public virtual string OutputSubDir { get; }
        public virtual string RootContextType { get; }
        public virtual string RootContextName { get; }
        public virtual string RegisterFunctionName { get; }
        public virtual CInclude? BaseInclude { get; }
        public Delegate BindingIdNamePredicate { get; }
        public Delegate BindingIdValuePredicate { get; }
        public Delegate BindingNamePredicate { get; }
        public string TemplateTypenameState { get; }
        public string TemplateTypenameContext { get; }
        public string TemplateIdentifierState { get; }
        public string TemplateIdentifierContext { get; }
        public string TemplateContextDefaultType { get; }
        public string TemplateContextDefaultValue { get; }
        public ImportedClassTemplateMode ImportedTemplateMode { get; }
        public string CppValidatorFileName { get; }
        public string CmakeVariableHeader { get; }
        public string CmakeVariableSource { get; }
        public string EqualityFunctionTemplateFullyQualifiedName { get; }
        public string StaticCastFunctionTemplateFullyQualifiedName { get; }
        public string DynamicCastFunctionTemplateFullyQualifiedName { get; }

        public virtual string DefaultOutputSubdir => "";
        public abstract string DefaultRootContextType { get; }
        public abstract string DefaultRootContextName { get; }
        public virtual string DefaultRegisterFunctionName => "register_";
        public virtual CInclude? DefaultBaseInclude => null;
        public virtual Delegate DefaultBindingIdNamePredicate => (string name) => $"_cppbind_id_{name}";
        public virtual Delegate DefaultBindingIdValuePredicate => (string name) => $"typeid({name}).name()";
        public virtual Delegate DefaultBindingNamePredicate => (string name) => $"_cppbind_{name}";
        public virtual string DefaultTemplateTypenameState => "CppBindState";
        public virtual string DefaultTemplateTypenameContext => "CppBindContext";
        public virtual string DefaultTemplateIdentifierState => "cpp_bind_state";
        public virtual string DefaultTemplateIdentifierContext => "cpp_bind_context";
        public abstract string DefaultTemplateContextDefaultType { get; }
        public abstract string DefaultTemplateContextDefaultValue { get; }
        public virtual ImportedClassTemplateMode DefaultImportedTemplateMode => ImportedClassTemplateMode.Indirect;
        public virtual string DefaulCppValidatorFileName => "_cppbind_validator_";
        public virtual string DefaultCmakeVariableHeader => "BINDINGS_HEADER";
        public virtual string DefaultCmakeVariableSource => "BINDINGS_SOURCE";
        public virtual string DefaultEqualityFunctionTemplateFullyQualifiedName => null;
        public virtual string DefaultStaticCastFunctionTemplateFullyQualifiedName => null;
        public virtual string DefaultDynamicCastFunctionTemplateFullyQualifiedName => null;

        public RegistrableGeneratorOptions(Generator generator)
        {
            Generator = generator;
            OutputSubDir = DefaultOutputSubdir;
            RootContextType = DefaultRootContextType;
            RootContextName = DefaultRootContextName;
            RegisterFunctionName = DefaultRegisterFunctionName;
            BaseInclude = DefaultBaseInclude;
            BindingIdNamePredicate = DefaultBindingIdNamePredicate;
            BindingIdValuePredicate = DefaultBindingIdValuePredicate;
            BindingNamePredicate = DefaultBindingNamePredicate;
            TemplateTypenameState = DefaultTemplateTypenameState;
            TemplateTypenameContext = DefaultTemplateTypenameContext;
            TemplateIdentifierState = DefaultTemplateIdentifierState;
            TemplateIdentifierContext = DefaultTemplateIdentifierContext;
            TemplateContextDefaultType = DefaultTemplateContextDefaultType;
            TemplateContextDefaultValue = DefaultTemplateContextDefaultValue;
            ImportedTemplateMode = DefaultImportedTemplateMode;
            CppValidatorFileName = DefaulCppValidatorFileName;
            CmakeVariableHeader = DefaultCmakeVariableHeader;
            CmakeVariableSource = DefaultCmakeVariableSource;
            EqualityFunctionTemplateFullyQualifiedName = DefaultEqualityFunctionTemplateFullyQualifiedName;
            StaticCastFunctionTemplateFullyQualifiedName = DefaultStaticCastFunctionTemplateFullyQualifiedName;
            DynamicCastFunctionTemplateFullyQualifiedName = DefaultDynamicCastFunctionTemplateFullyQualifiedName;
        }
    }

    public abstract class TRegistrableGeneratorOptions<TGenerator> : RegistrableGeneratorOptions
        where TGenerator : Generator
    {
        public override TGenerator Generator => (TGenerator)base.Generator;

        public TRegistrableGeneratorOptions(TGenerator generator) : base(generator)
        {
        }
    }
}
