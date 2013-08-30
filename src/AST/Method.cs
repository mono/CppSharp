namespace CppSharp.AST
{
    public enum CXXMethodKind
    {
        Normal,
        Constructor,
        Destructor,
        Conversion,
        Operator,
        UsingDirective
    }

    public enum CXXOperatorArity
    {
        Unary,
        Binary
    }

    public enum CXXOperatorKind
    {
        None,
        New,
        Delete,
        Array_New,
        Array_Delete,
        Plus,
        Minus,
        Star,
        Slash,
        Percent,
        Caret,
        Amp,
        Pipe,
        Tilde,
        Exclaim,
        Equal,
        Less,
        Greater,
        PlusEqual,
        MinusEqual,
        StarEqual,
        SlashEqual,
        PercentEqual,
        CaretEqual,
        AmpEqual,
        PipeEqual,
        LessLess,
        GreaterGreater,
        LessLessEqual,
        GreaterGreaterEqual,
        EqualEqual,
        ExclaimEqual,
        LessEqual,
        GreaterEqual,
        AmpAmp,
        PipePipe,
        PlusPlus,
        MinusMinus,
        Comma,
        ArrowStar,
        Arrow,
        Call,
        Subscript,
        Conditional
    }

    /// <summary>
    /// Represents a C++ record method declaration.
    /// </summary>
    public class Method : Function
    {
        public Method()
        {
            Access = AccessSpecifier.Public;
        }

        public Method(Method method)
            : base(method)
        {
            Access = method.Access;
            AccessDecl = method.AccessDecl;
            IsVirtual = method.IsVirtual;
            IsConst = method.IsConst;
            IsImplicit = method.IsImplicit;
            IsSynthetized = method.IsSynthetized;
            IsOverride = method.IsOverride;
            IsProxy = method.IsProxy;
            Kind = method.Kind;
            IsDefaultConstructor = method.IsDefaultConstructor;
            IsCopyConstructor = method.IsCopyConstructor;
            IsMoveConstructor = method.IsMoveConstructor;
            Conversion = method.Conversion;
        }

        public AccessSpecifierDecl AccessDecl { get; set; }

        public bool IsVirtual { get; set; }
        public bool IsStatic { get; set; }
        public bool IsConst { get; set; }
        public bool IsImplicit { get; set; }
        public bool IsSynthetized { get; set; }
        public bool IsOverride { get; set; }
        public bool IsProxy { get; set; }

        public CXXMethodKind Kind;

        public bool IsConstructor
        {
            get { return Kind == CXXMethodKind.Constructor; }
        }

        public bool IsDestructor
        {
            get { return Kind == CXXMethodKind.Destructor; }
        }

        public bool IsDefaultConstructor;
        public bool IsCopyConstructor;
        public bool IsMoveConstructor;

        public MethodConversionKind Conversion { get; set; }
    }
}