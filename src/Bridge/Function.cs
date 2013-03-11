using System.Collections.Generic;

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

    public class Parameter : Declaration, ITypedDecl
    {
        public Parameter()
        {
            Usage = ParameterUsage.Unknown;
            HasDefaultValue = false;
            Conversion = TypeConversionKind.None;
        }

        public Type Type { get { return QualifiedType.Type; } }
        public QualifiedType QualifiedType { get; set; }
        public ParameterUsage Usage { get; set; }
        public bool HasDefaultValue { get; set; }

        public TypeConversionKind Conversion { get; set; }

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
        public CallingConvention CallingConvention { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsInline { get; set; }
        
        // Mangled name
        public string Mangled { get; set; }
        
        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionDecl(this);
        }
    }
}