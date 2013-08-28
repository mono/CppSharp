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
    public class Method : Function, ITypedDecl
    {
        public Method()
        {
            Access = AccessSpecifier.Public;
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