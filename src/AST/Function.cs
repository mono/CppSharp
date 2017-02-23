﻿using System.Collections.Generic;
using System.Linq;
using CppSharp.AST.Extensions;

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
        OperatorParameter,
        ImplicitDestructorParameter
    }

    public class Parameter : Declaration, ITypedDecl
    {
        public Parameter()
        {
            Kind = ParameterKind.Regular;
            Usage = ParameterUsage.Unknown;
            HasDefaultValue = false;
        }

        public Parameter(Parameter p)
            : base(p)
        {
            HasDefaultValue = p.HasDefaultValue;
            Index = p.Index;
            IsIndirect = p.IsIndirect;
            Kind = p.Kind;
            QualifiedType = p.QualifiedType;
            Usage = p.Usage;
            DefaultArgument = p.DefaultArgument;
        }

        public Type Type => QualifiedType.Type;
        public QualifiedType QualifiedType { get; set; }
        public bool IsIndirect { get; set; }
        public uint Index { get; set; }

        public ParameterKind Kind { get; set; }
        public ParameterUsage Usage { get; set; }
        public bool HasDefaultValue { get; set; }

        public Expression DefaultArgument { get; set; }

        public bool IsIn => Usage == ParameterUsage.In;
        public bool IsOut => Usage == ParameterUsage.Out;
        public bool IsInOut => Usage == ParameterUsage.InOut;

        public bool IsSynthetized => Kind != ParameterKind.Regular;

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitParameterDecl(this);
        }

        /// <summary>
        /// HACK: in many cases QualifiedType.Qualifiers.IsConst does not work.
        /// It's false in Clang to begin with. I tried fixing it to no avail.
        /// I don't have any more time at the moment.
        /// </summary>
        public bool IsConst
        {
            get { return DebugText.StartsWith("const ", System.StringComparison.Ordinal); }
        }
    }

    public class ParameterTypeComparer : IEqualityComparer<Parameter>
    {
        public static readonly ParameterTypeComparer Instance = new ParameterTypeComparer();

        private ParameterTypeComparer()
        {
        }

        public bool Equals(Parameter x, Parameter y)
        {
            return x.QualifiedType.ResolvesTo(y.QualifiedType);
        }

        public int GetHashCode(Parameter obj)
        {
            return obj.Type.GetHashCode();
        }
    }

    public enum FunctionSynthKind
    {
        None,
        ComplementOperator,
        AbstractImplCall,
        DefaultValueOverload,
        InterfaceInstance,
        AdjustedMethod
    }

    public enum FriendKind
    {
        None,
        Declared,
        Undeclared
    }

    public class Function : Declaration, ITypedDecl, IMangledDecl
    {
        public Function()
        {
            Parameters = new List<Parameter>();
            CallingConvention = CallingConvention.Default;
            Signature = string.Empty;
            Index = null;
        }

        public Function(Function function)
            : base(function)
        {
            Parameters = new List<Parameter>();
            ReturnType = function.ReturnType;
            IsReturnIndirect = function.IsReturnIndirect;
            HasThisReturn = function.HasThisReturn;
            Parameters.AddRange(function.Parameters.Select(p => new Parameter(p)));
            IsVariadic = function.IsVariadic;
            IsInline = function.IsInline;
            IsPure = function.IsPure;
            OperatorKind = function.OperatorKind;
            CallingConvention = function.CallingConvention;
            SynthKind = function.SynthKind;
            OriginalFunction = function.OriginalFunction;
            Mangled = function.Mangled;
            Index = function.Index;
            Signature = function.Signature;
            if (function.SpecializationInfo != null)
            {
                SpecializationInfo = new FunctionTemplateSpecialization(function.SpecializationInfo);
                SpecializationInfo.SpecializedFunction = function;
            }
        }

        public QualifiedType ReturnType { get; set; }
        public bool IsReturnIndirect { get; set; }
        public bool HasThisReturn { get; set; }

        public List<Parameter> Parameters { get; set; }
        public bool IsConstExpr { get; set; }
        public bool IsVariadic { get; set; }
        public bool IsInline { get; set; }
        public bool IsPure { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsAmbiguous { get; set; }
        public FriendKind FriendKind { get; set; }
        public CXXOperatorKind OperatorKind { get; set; }
        public bool IsOperator => OperatorKind != CXXOperatorKind.None;

        public CallingConvention CallingConvention { get; set; }

        public FunctionTemplateSpecialization SpecializationInfo { get; set; }

        public Function InstantiatedFrom { get; set; }

        public QualifiedType FunctionType { get; set; }

        public bool IsThisCall => CallingConvention == CallingConvention.ThisCall;

        public bool IsStdCall => CallingConvention == CallingConvention.StdCall;

        public bool IsFastCall => CallingConvention == CallingConvention.FastCall;

        public bool IsCCall => CallingConvention == CallingConvention.C;

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
            set
            {
                if (HasIndirectReturnTypeParameter)
                    Parameters.Single(p => p.Kind == ParameterKind.IndirectReturnType).QualifiedType = value;
                else
                    ReturnType = value;
            }
        }

        public FunctionSynthKind SynthKind { get; set; }
        public bool IsSynthetized => SynthKind != FunctionSynthKind.None;
        public bool IsNonMemberOperator { get; set; }

        public Function OriginalFunction { get; set; }

        public string Mangled { get; set; }
        public string Signature { get; set; }
        public string Body { get; set; }

        /// <summary>
        /// Keeps an index that de-duplicates native names in the C# backend.
        /// </summary>
        public uint? Index { get; set; }

        public override T Visit<T>(IDeclVisitor<T> visitor)
        {
            return visitor.VisitFunctionDecl(this);
        }

        public Type Type => ReturnType.Type;
        public QualifiedType QualifiedType => ReturnType;
    }
}