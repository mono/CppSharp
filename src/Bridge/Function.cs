using System;
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

    public class Parameter : Declaration
    {
        public Parameter()
        {
            Usage = ParameterUsage.Unknown;
            HasDefaultValue = false;
            IsConst = false;
            Conversion = TypeConversionKind.None;
        }

        public Type Type { get; set; }
        public ParameterUsage Usage { get; set; }
        public bool HasDefaultValue { get; set; }
        public bool IsConst { get; set; }

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

        public string ToCSharpCallConv()
        {
            switch (CallingConvention)
            {
            case CallingConvention.Default:
                return "Winapi";
            case CallingConvention.C:
                return "Cdecl";
            case CallingConvention.StdCall:
                return "StdCall";
            case CallingConvention.ThisCall:
                return "ThisCall";
            case CallingConvention.FastCall:
                return "FastCall";
            }

            return "Winapi";
        }

        public Type ReturnType { get; set; }
        public List<Parameter> Parameters { get; set; }
        public CallingConvention CallingConvention { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsInline { get; set; }
        
        // Mangled name
        public string Mangled { get; set; }
        
        // Transformed name
        public string FormattedName { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionDecl(this);
        }
    }
}