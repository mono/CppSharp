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

    public class ParameterTypeComparer : IEqualityComparer<Parameter>
    {
        public bool Equals(Parameter x, Parameter y)
        {
            return x.QualifiedType == y.QualifiedType;
        }

        public int GetHashCode(Parameter obj)
        {
            return obj.Type.GetHashCode();
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
            Signature = string.Empty;
        }

        public Function(Function function)
            : base(function)
        {
            Parameters = new List<Parameter>();
            ReturnType = function.ReturnType;
            IsReturnIndirect = function.IsReturnIndirect;
            Parameters.AddRange(function.Parameters);
            IsVariadic = function.IsVariadic;
            IsInline = function.IsInline;
            IsPure = function.IsPure;
            OperatorKind = function.OperatorKind;
            CallingConvention = function.CallingConvention;
            SynthKind = function.SynthKind;
            OriginalFunction = function.OriginalFunction;
            Mangled = function.Mangled;
        }

        public QualifiedType ReturnType { get; set; }
        public bool IsReturnIndirect { get; set; }

        public List<Parameter> Parameters { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsInline { get; set; }
        public bool IsPure { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsAmbiguous { get; set; }

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

        public string Signature { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionDecl(this);
        }

        public Type Type { get { return ReturnType.Type; } }
        public QualifiedType QualifiedType { get { return ReturnType; } }

        public virtual QualifiedType GetFunctionType()
        {
            var functionType = new FunctionType
                                {
                                    CallingConvention = this.CallingConvention,
                                    ReturnType = this.ReturnType
                                };
            functionType.Parameters.AddRange(Parameters);
            ReplaceIndirectReturnParamWithRegular(functionType);
            var pointerType = new PointerType { QualifiedPointee = new QualifiedType(functionType) };
            return new QualifiedType(pointerType);
        }

        private static void ReplaceIndirectReturnParamWithRegular(FunctionType functionType)
        {
            for (int i = functionType.Parameters.Count - 1; i >= 0; i--)
            {
                var parameter = functionType.Parameters[i];
                if (parameter.Kind == ParameterKind.IndirectReturnType)
                {
                    var ptrType = new PointerType { QualifiedPointee = new QualifiedType(parameter.Type) };
                    var retParam = new Parameter { Name = parameter.Name, QualifiedType = new QualifiedType(ptrType) };
                    functionType.Parameters.RemoveAt(i);
                    functionType.Parameters.Insert(i, retParam);
                }
            }
        }
    }
}