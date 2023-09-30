using System;
using System.Collections.Generic;
using System.Linq;

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
        Zero,
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
        Conditional,
        Coawait,
        Conversion,
        ExplicitConversion
    }

    public enum RefQualifier
    {
        None,
        LValue,
        RValue
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
            IsVirtual = method.IsVirtual;
            IsStatic = method.IsStatic;
            IsConst = method.IsConst;
            IsExplicit = method.IsExplicit;
            IsVolatile = method.IsVolatile;
            IsFinal = method.IsFinal;
            IsProxy = method.IsProxy;
            Kind = method.Kind;
            IsDefaultConstructor = method.IsDefaultConstructor;
            IsCopyConstructor = method.IsCopyConstructor;
            IsMoveConstructor = method.IsMoveConstructor;
            Conversion = method.Conversion;
            SynthKind = method.SynthKind;
            AdjustedOffset = method.AdjustedOffset;
            OverriddenMethods.AddRange(method.OverriddenMethods);
            ConvertToProperty = method.ConvertToProperty;
        }

        public Method(Function function)
            : base(function)
        {

        }

        public bool IsVirtual { get; set; }
        public bool IsStatic { get; set; }
        public bool IsConst { get; set; }
        public bool IsExplicit { get; set; }
        public bool IsVolatile { get; set; }

        public bool IsOverride
        {
            get { return isOverride ?? OverriddenMethods.Any(); }
            set { isOverride = value; }
        }

        public Method BaseMethod => OverriddenMethods.FirstOrDefault();

        // True if the method is final / sealed.
        public bool IsFinal { get; set; }

        public bool IsProxy { get; set; }

        public RefQualifier RefQualifier { get; set; }

        private CXXMethodKind kind;
        public CXXMethodKind Kind
        {
            get { return kind; }
            set
            {
                if (kind != value)
                {
                    kind = value;
                    if (kind == CXXMethodKind.Conversion)
                        OperatorKind = CXXOperatorKind.Conversion;
                }
            }
        }

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

        public QualifiedType ConversionType { get; set; }

        public Class ExplicitInterfaceImpl { get; set; }

        public int AdjustedOffset { get; set; }

        public List<Method> OverriddenMethods { get; } = new List<Method>();

        public bool ConvertToProperty { get; set; }

        public Method GetRootBaseMethod()
        {
            return BaseMethod == null || BaseMethod.BaseMethod == null ?
                BaseMethod : BaseMethod.GetRootBaseMethod();
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitMethodDecl(this);
        }

        private bool? isOverride;

        public bool HasSameSignature(Method other)
        {
            return Parameters.SequenceEqual(other.Parameters, ParameterTypeComparer.Instance);
        }

        public override string ToString()
        {
            return !string.IsNullOrEmpty(DebugText) ? DebugText : Name;
        }
    }
}
