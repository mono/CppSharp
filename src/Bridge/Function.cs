using System.Collections.Generic;
using System.Linq;

namespace Cxxi
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
        HiddenStructureReturn
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

        public ParameterKind Kind { get; set; }
        public ParameterUsage Usage { get; set; }
        public bool HasDefaultValue { get; set; }

        public bool IsSynthetized
        {
            get { return Kind != ParameterKind.Regular; }
        }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitParameterDecl(this);
        }
    }

    public class Function : Declaration
    {
        public Function()
        {
            Parameters = new List<Parameter>();
            CallingConvention = CallingConvention.Default;
            IsVariadic = false;
            IsInline = false;
        }

        public Type ReturnType { get; set; }
        public List<Parameter> Parameters { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsInline { get; set; }

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

        public bool HasHiddenStructParameter
        {
            get
            {
                return Parameters.Any(param =>
                    param.Kind == ParameterKind.HiddenStructureReturn);
            }
        }

        // Mangled name
        public string Mangled { get; set; }
        
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionDecl(this);
        }
    }
}