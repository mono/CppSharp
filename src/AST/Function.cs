using System.Collections.Generic;
using System.Linq;

namespace CppSharp.AST
{
    public enum CallingConvention
    {
        Default,
        C,
        StdCall,
        ThisCall,
        FastCall,
        Unknown
    }

    public enum ParameterUsage
    {
        In,
        Out,
        InOut,
        Unknown
    }

    public enum ParameterKind
    {
        Regular,
        IndirectReturnType,
        OperatorParameter
    }

    public class Parameter : Declaration, ITypedDecl
    {
        public Parameter()
        {
            Kind = ParameterKind.Regular;
            Usage = ParameterUsage.Unknown;
            HasDefaultValue = false;
        }

        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }
        public bool IsIndirect { get; set; }

        public ParameterKind Kind { get; set; }
        public ParameterUsage Usage { get; set; }
        public bool HasDefaultValue { get; set; }

        public bool IsIn { get { return Usage == ParameterUsage.In; } }
        public bool IsOut { get { return Usage == ParameterUsage.Out; } }
        public bool IsInOut { get { return Usage == ParameterUsage.InOut; } }

        public bool IsSynthetized
        {
            get { return Kind != ParameterKind.Regular; }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitParameterDecl(this);
        }
    }

    public enum FunctionSynthKind
    {
        None,
        NonMemberOperator
    }

    public class Function : Declaration, ITypedDecl, IMangledDecl
    {
        public Function()
        {
            Parameters = new List<Parameter>();
            CallingConvention = CallingConvention.Default;
            IsVariadic = false;
            IsInline = false;
        }

        public QualifiedType ReturnType { get; set; }
        public bool IsReturnIndirect { get; set; }

        public List<Parameter> Parameters { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsInline { get; set; }
        public bool IsPure { get; set; }

        public CXXOperatorKind OperatorKind { get; set; }
        public bool IsOperator { get { return OperatorKind != CXXOperatorKind.None; } }

        public CallingConvention CallingConvention { get; set; }

        public bool IsThisCall
        {
            get { return CallingConvention == CallingConvention.ThisCall; }
        }

        public bool IsStdCall
        {
            get { return CallingConvention == CallingConvention.StdCall; }
        }

        public bool IsFastCall
        {
            get { return CallingConvention == CallingConvention.FastCall; }
        }

        public bool IsCCall
        {
            get { return CallingConvention == CallingConvention.C; }
        }

        public bool HasIndirectReturnTypeParameter
        {
            get
            {
                return Parameters.Any(param =>
                    param.Kind == ParameterKind.IndirectReturnType);
            }
        }

        public QualifiedType OriginalReturnType
        {
            get
            {
                if (!HasIndirectReturnTypeParameter)
                    return ReturnType;

                var hiddenParam = Parameters.Single(param =>
                    param.Kind == ParameterKind.IndirectReturnType);
                return hiddenParam.QualifiedType;
            }
        }

        public FunctionSynthKind SynthKind { get; set; }

        public Function OriginalFunction { get; set; }

        public string Mangled { get; set; }
        
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionDecl(this);
        }

        public Type Type { get { return ReturnType.Type; } }
        public QualifiedType QualifiedType { get { return ReturnType; } }
    }
}