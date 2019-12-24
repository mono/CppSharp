﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CppSharp.AST.Extensions;

namespace CppSharp.AST
{
    /// <summary>
    /// Represents a C++ type.
    /// </summary>
    [DebuggerDisplay("{ToString()} [{GetType().Name}]")]
    public abstract class Type : ICloneable
    {
        public static Func<Type, string> TypePrinterDelegate;

        public bool IsDependent { get; set; }

        protected Type()
        {
        }

        protected Type(Type type)
        {
            IsDependent = type.IsDependent;
        }

        public abstract T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals
            = new TypeQualifiers());

        public override string ToString()
        {
            return TypePrinterDelegate(this);
        }

        public abstract object Clone();
    }

    /// <summary>
    /// Represents C++ type qualifiers.
    /// </summary>
    public struct TypeQualifiers
    {
        public bool IsConst;
        public bool IsVolatile;
        public bool IsRestrict;

        public override int GetHashCode() =>
            IsConst.GetHashCode() ^ IsVolatile.GetHashCode() ^
                IsRestrict.GetHashCode();
    }

    /// <summary>
    /// Represents a qualified C++ type.
    /// </summary>
    public struct QualifiedType
    {
        public QualifiedType(Type type)
            : this()
        {
            Type = type;
        }

        public QualifiedType(Type type, TypeQualifiers qualifiers)
            : this()
        {
            Type = type;
            Qualifiers = qualifiers;
        }

        public Type Type { get; set; }
        public TypeQualifiers Qualifiers { get; set; }

        public T Visit<T>(ITypeVisitor<T> visitor)
        {
            return Type.Visit(visitor, Qualifiers);
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is QualifiedType)) return false;

            var type = (QualifiedType) obj;
            return Type.Equals(type.Type) && Qualifiers.Equals(type.Qualifiers);
        }

        public static bool operator ==(QualifiedType left, QualifiedType right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(QualifiedType left, QualifiedType right)
        {
            return !(left == right);
        }

        public override int GetHashCode() => Type == null ?
            Qualifiers.GetHashCode() : Type.GetHashCode() ^ Qualifiers.GetHashCode();
    }

    /// <summary>
    /// Represents a C++ tag type.
    /// </summary>
    public class TagType : Type
    {
        public TagType()
        {
        }

        public TagType(Declaration decl)
        {
            Declaration = decl;
        }

        public TagType(TagType type)
            : base(type)
        {
            Declaration = type.Declaration;
        }

        public Declaration Declaration;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTagType(this, quals);
        }

        public override object Clone()
        {
            return new TagType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as TagType;
            if (type == null) return false;

            return Declaration.Equals(type.Declaration);
        }

        public override int GetHashCode() => Declaration.GetHashCode();
    }

    /// <summary>
    /// Represents an C/C++ array type.
    /// </summary>
    public class ArrayType : Type
    {
        public enum ArraySize
        {
            Constant,
            Variable,
            Dependent,
            Incomplete
        }

        // Type of the array elements.
        public QualifiedType QualifiedType;

        // Size type of array.
        public ArraySize SizeType;

        // In case of a constant size array.
        public long Size;

        // Size of the element type of the array.
        public long ElementSize;

        public ArrayType()
        {
        }

        public ArrayType(ArrayType type)
            : base(type)
        {
            QualifiedType = new QualifiedType((Type) type.QualifiedType.Type.Clone(),
                type.QualifiedType.Qualifiers);
            SizeType = type.SizeType;
            Size = type.Size;
        }

        public Type Type
        {
            get { return QualifiedType.Type; }
        }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitArrayType(this, quals);
        }

        public override object Clone()
        {
            return new ArrayType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as ArrayType;
            if (type == null) return false;
            var equals = QualifiedType.Equals(type.QualifiedType) && SizeType.Equals(type.SizeType);

            if (SizeType == ArraySize.Constant)
                equals &= Size.Equals(type.Size);

            return equals;
        }

        public override int GetHashCode()
        {
            return QualifiedType.GetHashCode() ^ SizeType.GetHashCode() ^
                Size.GetHashCode() ^ ElementSize.GetHashCode();
        }
    }

    public enum ExceptionSpecType
    {
        None,
        DynamicNone,
        Dynamic,
        MSAny,
        BasicNoexcept,
        DependentNoexcept,
        NoexceptFalse,
        NoexceptTrue,
        Unevaluated,
        Uninstantiated,
        Unparsed
    }

    /// <summary>
    /// Represents an C/C++ function type.
    /// </summary>
    public class FunctionType : Type
    {
        // Return type of the function.
        public QualifiedType ReturnType;

        // Argument types.
        public List<Parameter> Parameters { get; } = new List<Parameter>();

        public CallingConvention CallingConvention { get; set; }

        public ExceptionSpecType ExceptionSpecType { get; set; }

        public FunctionType()
        {
        }

        public FunctionType(FunctionType type)
            : base(type)
        {
            ReturnType = new QualifiedType((Type) type.ReturnType.Type.Clone(), type.ReturnType.Qualifiers);
            Parameters.AddRange(type.Parameters.Select(p => new Parameter(p)));
            CallingConvention = type.CallingConvention;
            IsDependent = type.IsDependent;
        }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitFunctionType(this, quals);
        }

        public override object Clone()
        {
            return new FunctionType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as FunctionType;
            if (type == null) return false;

            return ReturnType.Equals(type.ReturnType) && Parameters.SequenceEqual(type.Parameters);
        }

        public override int GetHashCode() =>
            Parameters.Aggregate(ReturnType.GetHashCode(),
                (p1, p2) => p1.GetHashCode() ^ p2.GetHashCode()) ^
                CallingConvention.GetHashCode() ^
            ExceptionSpecType.GetHashCode();
    }

    /// <summary>
    /// Represents a C++ pointer/reference type.
    /// </summary>
    public class PointerType : Type
    {
        public PointerType(QualifiedType pointee = new QualifiedType())
        {
            Modifier = TypeModifier.Pointer;
            QualifiedPointee = pointee;
        }

        /// <summary>
        /// Represents the modifiers on a C++ type reference.
        /// </summary>
        public enum TypeModifier
        {
            Value,
            Pointer,
            // L-value references
            LVReference,
            // R-value references
            RVReference
        }

        public PointerType()
        {
        }

        public PointerType(PointerType type)
            : base(type)
        {
            QualifiedPointee = new QualifiedType((Type) type.QualifiedPointee.Type.Clone(),
                type.QualifiedPointee.Qualifiers);
            Modifier = type.Modifier;
        }

        public bool IsReference
        {
            get
            {
                return Modifier == TypeModifier.LVReference ||
                    Modifier == TypeModifier.RVReference;
            }
        }

        public QualifiedType QualifiedPointee;
        public Type Pointee { get { return QualifiedPointee.Type;  } }

        public TypeModifier Modifier;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitPointerType(this, quals);
        }

        public override object Clone()
        {
            return new PointerType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as PointerType;
            if (type == null) return false;

            return QualifiedPointee.Equals(type.QualifiedPointee)
                && Modifier == type.Modifier;
        }

        public override int GetHashCode() =>
            QualifiedPointee.GetHashCode() ^ Modifier.GetHashCode();
    }

    /// <summary>
    /// Represents a C++ member function pointer type.
    /// </summary>
    public class MemberPointerType : Type
    {
        public QualifiedType QualifiedPointee;

        public MemberPointerType()
        {
        }

        public MemberPointerType(MemberPointerType type)
            : base(type)
        {
            QualifiedPointee = new QualifiedType((Type) type.QualifiedPointee.Type.Clone(),
                type.QualifiedPointee.Qualifiers);
        }

        public Type Pointee
        {
            get { return QualifiedPointee.Type; }
        }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitMemberPointerType(this, quals);
        }

        public override object Clone()
        {
            return new MemberPointerType(this);
        }

        public override bool Equals(object obj)
        {
            var pointer = obj as MemberPointerType;
            if (pointer == null) return false;

            return QualifiedPointee.Equals(pointer.QualifiedPointee);
        }

        public override int GetHashCode() => QualifiedPointee.GetHashCode();
    }

    /// <summary>
    /// Represents a C/C++ typedef type.
    /// </summary>
    public class TypedefType : Type
    {
        public TypedefNameDecl Declaration { get; set; }

        public TypedefType()
        {
        }

        public TypedefType(TypedefNameDecl decl)
        {
            Declaration = decl;
        }

        public TypedefType(TypedefType type)
            : base(type)
        {
            Declaration = type.Declaration;
        }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTypedefType(this, quals);
        }

        public override object Clone()
        {
            return new TypedefType(this);
        }

        public override bool Equals(object obj)
        {
            var typedef = obj as TypedefType;
            if (typedef == null)
                return false;

            return Declaration.OriginalName == typedef.Declaration.OriginalName &&
                   Declaration.Type.Equals(typedef.Declaration.Type);
        }

        public override int GetHashCode() =>
            Declaration.OriginalName.GetHashCode() ^ Declaration.Type.GetHashCode();
    }

    /// <summary>
    /// An attributed type is a type to which a type attribute has been
    /// applied.
    ///
    /// For example, in the following attributed type:
    ///     int32_t __attribute__((vector_size(16)))
    ///
    /// The modified type is the TypedefType for int32_t
    /// The equivalent type is VectorType(16, int32_t)
    /// </summary>
    public class AttributedType : Type
    {
        public QualifiedType Modified;

        public QualifiedType Equivalent;

        public AttributedType()
        {
        }

        public AttributedType(AttributedType type)
            : base(type)
        {
            Modified = new QualifiedType((Type) type.Modified.Type.Clone(), type.Modified.Qualifiers);
            Equivalent = new QualifiedType((Type) type.Equivalent.Type.Clone(), type.Equivalent.Qualifiers);
        }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitAttributedType(this, quals);
        }

        public override object Clone()
        {
            return new AttributedType
            {
                IsDependent = IsDependent,
                Modified = new QualifiedType((Type) Modified.Type.Clone(), Modified.Qualifiers),
                Equivalent = new QualifiedType((Type) Equivalent.Type.Clone(), Equivalent.Qualifiers)
            };
        }

        public override bool Equals(object obj)
        {
            var attributed = obj as AttributedType;
            if (attributed == null) return false;

            return Modified.Equals(attributed.Modified)
                && Equivalent.Equals(attributed.Equivalent);
        }

        public override int GetHashCode() =>
            Modified.GetHashCode() ^ Equivalent.GetHashCode();
    }

    /// <summary>
    /// Represents a pointer type decayed from an array or function type.
    /// </summary>
    public class DecayedType : Type
    {
        public QualifiedType Decayed;
        public QualifiedType Original;
        public QualifiedType Pointee;

        public DecayedType()
        {
        }

        public DecayedType(DecayedType type)
            : base(type)
        {
            Decayed = new QualifiedType((Type) type.Decayed.Type.Clone(),
                type.Decayed.Qualifiers);
            Original = new QualifiedType((Type) type.Original.Type.Clone(),
                type.Original.Qualifiers);
            Pointee = new QualifiedType((Type) type.Pointee.Type.Clone(),
                type.Pointee.Qualifiers);
        }

        public override T Visit<T>(ITypeVisitor<T> visitor,
            TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitDecayedType(this, quals);
        }

        public override object Clone()
        {
            return new DecayedType(this);
        }

        public override bool Equals(object obj)
        {
            var decay = obj as DecayedType;
            if (decay == null) return false;

            return Original.Equals(decay.Original);
        }

        public override int GetHashCode() =>
            Decayed.GetHashCode() ^ Original.GetHashCode() ^
            Pointee.GetHashCode();
    }

    /// <summary>
    /// Represents a template argument within a class template specialization.
    /// </summary>
    public class TemplateArgument
    {
        /// The kind of template argument we're storing.
        public enum ArgumentKind
        {
            /// The template argument is a type.
            Type,

            /// The template argument is a declaration that was provided for a
            /// pointer. reference, or pointer to member non-type template
            /// parameter.
            Declaration,

            /// The template argument is a null pointer or null pointer to member
            /// that was provided for a non-type template parameter.
            NullPtr,

            /// The template argument is an integral value that was provided for
            /// an integral non-type template parameter.
            Integral,

            /// The template argument is a template name that was provided for a
            /// template template parameter.
            Template,

            /// The template argument is a pack expansion of a template name that
            /// was provided for a template template parameter.
            TemplateExpansion,

            /// The template argument is a value- or type-dependent expression.
            Expression,

            /// The template argument is actually a parameter pack.
            Pack
        }

        public ArgumentKind Kind;
        public QualifiedType Type;
        public Declaration Declaration;
        public long Integral;

        public override bool Equals(object obj)
        {
            if (!(obj is TemplateArgument)) return false;
            var arg = (TemplateArgument) obj;

            if (Kind != arg.Kind) return false;

            switch (Kind)
            {
            case ArgumentKind.Type:
                return Type.Equals(arg.Type);
            case ArgumentKind.Declaration:
                return Declaration.Equals(arg.Declaration);
            case ArgumentKind.Integral:
                return Integral.Equals(arg.Integral);
            case ArgumentKind.Expression:
                return true;
            default:
                throw new Exception("Unknown TemplateArgument Kind");
            }
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case ArgumentKind.Type:
                    return Type.ToString();
                case ArgumentKind.Declaration:
                    return Declaration.ToString();
                case ArgumentKind.Integral:
                    return Integral.ToString();
                case ArgumentKind.Expression:
                    return string.Empty;
                default:
                    throw new Exception("Unknown TemplateArgument Kind");
            }
        }
    }

    /// <summary>
    /// Represents a C++ template specialization type.
    /// </summary>
    public class TemplateSpecializationType : Type
    {
        public TemplateSpecializationType()
        {
            Arguments = new List<TemplateArgument>();
        }

        public TemplateSpecializationType(TemplateSpecializationType type)
            : base(type)
        {
            Arguments = type.Arguments.Select(
                t => new TemplateArgument
                {
                    Declaration = t.Declaration,
                    Integral = t.Integral,
                    Kind = t.Kind,
                    Type = new QualifiedType((Type) t.Type.Type.Clone(), t.Type.Qualifiers)
                }).ToList();
            Template = type.Template;
            if (type.Desugared.Type != null)
                Desugared = new QualifiedType((Type) type.Desugared.Type.Clone(),
                    type.Desugared.Qualifiers);
        }

        public List<TemplateArgument> Arguments;

        public Template Template;

        public QualifiedType Desugared;

        public ClassTemplateSpecialization GetClassTemplateSpecialization()
        {
            return GetDeclaration() as ClassTemplateSpecialization;
        }

        private Declaration GetDeclaration()
        {
            var finalType = Desugared.Type.GetFinalPointee() ?? Desugared.Type;

            var tagType = finalType as TagType;
            if (tagType != null)
                return tagType.Declaration;

            var injectedClassNameType = finalType as InjectedClassNameType;
            if (injectedClassNameType == null)
                return null;

            var injectedSpecializationType = (TemplateSpecializationType)
                injectedClassNameType.InjectedSpecializationType.Type;
            return injectedSpecializationType.GetDeclaration();
        }

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTemplateSpecializationType(this, quals);
        }

        public override object Clone()
        {
            return new TemplateSpecializationType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as TemplateSpecializationType;
            if (type == null) return false;

            return Arguments.SequenceEqual(type.Arguments) &&
                ((Template != null && Template.Name == type.Template.Name) ||
                (Desugared.Type != null && Desugared == type.Desugared));
        }

        public override int GetHashCode() =>
            Arguments.Aggregate(Template.GetHashCode(),
                (a1, a2) => a1.GetHashCode() ^ a2.GetHashCode()) ^
                Desugared.GetHashCode();
    }

    /// <summary>
    /// Represents a C++ dependent template specialization type.
    /// </summary>
    public class DependentTemplateSpecializationType : Type
    {
        public DependentTemplateSpecializationType()
        {
            Arguments = new List<TemplateArgument>();
        }

        public DependentTemplateSpecializationType(DependentTemplateSpecializationType type)
            : base(type)
        {
            Arguments = type.Arguments.Select(
                t => new TemplateArgument
                {
                    Declaration = t.Declaration,
                    Integral = t.Integral,
                    Kind = t.Kind,
                    Type = new QualifiedType((Type) t.Type.Type.Clone(), t.Type.Qualifiers)
                }).ToList();
            Desugared = new QualifiedType((Type) type.Desugared.Type.Clone(), type.Desugared.Qualifiers);
        }

        public List<TemplateArgument> Arguments;

        public QualifiedType Desugared;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitDependentTemplateSpecializationType(this, quals);
        }

        public override object Clone()
        {
            return new DependentTemplateSpecializationType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as TemplateSpecializationType;
            if (type == null) return false;

            return Arguments.SequenceEqual(type.Arguments) &&
                Desugared == type.Desugared;
        }

        public override int GetHashCode() =>
            Arguments.Aggregate(Desugared.GetHashCode(),
                (a1, a2) => a1.GetHashCode() ^ a2.GetHashCode());
    }

    /// <summary>
    /// Represents a C++ template parameter type.
    /// </summary>
    public class TemplateParameterType : Type
    {
        public TypeTemplateParameter Parameter;
        public uint Depth;
        public uint Index;
        public bool IsParameterPack;

        public TemplateParameterType()
        {
        }

        public TemplateParameterType(TemplateParameterType type)
            : base(type)
        {
            if (type.Parameter != null)
                Parameter = new TypeTemplateParameter
                {
                    Constraint = type.Parameter.Constraint,
                    Name = type.Parameter.Name
                };
            Depth = type.Depth;
            Index = type.Index;
            IsParameterPack = type.IsParameterPack;
        }

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTemplateParameterType(this, quals);
        }

        public override object Clone()
        {
            return new TemplateParameterType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as TemplateParameterType;
            if (type == null) return false;

            return Parameter == type.Parameter
                && Depth == type.Depth
                && Index == type.Index
                && IsParameterPack == type.IsParameterPack;
        }

        public override int GetHashCode() =>
            Parameter == null ?
            Depth.GetHashCode() ^ Index.GetHashCode() ^
            IsParameterPack.GetHashCode() :
            Parameter.GetHashCode() ^ Depth.GetHashCode() ^
            Index.GetHashCode() ^ IsParameterPack.GetHashCode();
    }

    /// <summary>
    /// Represents the result of substituting a type for a template type parameter.
    /// </summary>
    public class TemplateParameterSubstitutionType : Type
    {
        public QualifiedType Replacement;

        public TemplateParameterType ReplacedParameter { get; set; }

        public TemplateParameterSubstitutionType()
        {
        }

        public TemplateParameterSubstitutionType(TemplateParameterSubstitutionType type)
            : base(type)
        {
            Replacement = new QualifiedType((Type) type.Replacement.Type.Clone(), type.Replacement.Qualifiers);
            ReplacedParameter = (TemplateParameterType) type.ReplacedParameter.Clone();
        }

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTemplateParameterSubstitutionType(this, quals);
        }

        public override object Clone()
        {
            return new TemplateParameterSubstitutionType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as TemplateParameterSubstitutionType;
            if (type == null) return false;

            return ReplacedParameter.Equals(type.ReplacedParameter) &&
                Replacement.Equals(type.Replacement);
        }

        public override int GetHashCode() =>
            ReplacedParameter.GetHashCode() ^ Replacement.GetHashCode();
    }

    /// <summary>
    /// The injected class name of a C++ class template or class template partial
    /// specialization.
    /// </summary>
    public class InjectedClassNameType : Type
    {
        public TemplateSpecializationType TemplateSpecialization;
        public Class Class;

        public InjectedClassNameType()
        {
        }

        public InjectedClassNameType(InjectedClassNameType type)
            : base(type)
        {
            if (type.TemplateSpecialization != null)
                TemplateSpecialization = (TemplateSpecializationType) type.TemplateSpecialization.Clone();
            InjectedSpecializationType = new QualifiedType(
                (Type) type.InjectedSpecializationType.Type.Clone(),
                type.InjectedSpecializationType.Qualifiers);
            Class = type.Class;
        }

        public QualifiedType InjectedSpecializationType { get; set; }

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitInjectedClassNameType(this, quals);
        }

        public override object Clone()
        {
            return new InjectedClassNameType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as InjectedClassNameType;
            if (type == null) return false;
            if (TemplateSpecialization == null || type.TemplateSpecialization == null)
                return TemplateSpecialization == type.TemplateSpecialization;

            return TemplateSpecialization.Equals(type.TemplateSpecialization)
                && Class.Equals(type.Class);
        }

        public override int GetHashCode() =>
            TemplateSpecialization != null && Class != null
                ? TemplateSpecialization.GetHashCode() ^ Class.GetHashCode()
                : TemplateSpecialization != null ? TemplateSpecialization.GetHashCode()
                : Class.GetHashCode();
    }

    /// <summary>
    /// Represents a qualified type name for which the type name is dependent.
    /// </summary>
    public class DependentNameType : Type
    {
        public DependentNameType()
        {
        }

        public DependentNameType(DependentNameType type)
            : base(type)
        {
        }

        public QualifiedType Qualifier { get; set; }

        public string Identifier { get; set; }

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitDependentNameType(this, quals);
        }

        public override object Clone()
        {
            return new DependentNameType(this);
        }

        public override int GetHashCode() =>
            Qualifier.GetHashCode() ^ Identifier.GetHashCode();
    }

    /// <summary>
    /// Represents a CIL type.
    /// </summary>
    public class CILType : Type
    {
        public CILType(System.Type type)
        {
            Type = type;
        }

        public CILType()
        {
        }

        public CILType(CILType type)
            : base(type)
        {
            Type = type.Type;
        }

        public System.Type Type;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitCILType(this, quals);
        }

        public override object Clone()
        {
            return new CILType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as CILType;
            if (type == null) return false;

            return Type == type.Type;
        }

        public override int GetHashCode() => Type.GetHashCode();
    }

    public class PackExpansionType : Type
    {
        public PackExpansionType()
        {
        }

        public PackExpansionType(PackExpansionType type)
            : base(type)
        {
        }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitPackExpansionType(this, quals);
        }

        public override object Clone()
        {
            return new PackExpansionType(this);
        }
    }

    public class UnaryTransformType : Type
    {
        public UnaryTransformType()
        {
        }

        public UnaryTransformType(UnaryTransformType type)
            : base(type)
        {
            Desugared = type.Desugared;
            BaseType = type.BaseType;
        }

        public QualifiedType Desugared { get; set; }
        public QualifiedType BaseType { get; set; }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitUnaryTransformType(this, quals);
        }

        public override object Clone()
        {
            return new UnaryTransformType(this);
        }

        public override int GetHashCode() =>
            Desugared.GetHashCode() ^ BaseType.GetHashCode();
    }

    public class UnresolvedUsingType : Type
    {
        public UnresolvedUsingType()
        {
        }

        public UnresolvedUsingType(UnresolvedUsingType type)
            : base(type)
        {
        }

        public UnresolvedUsingTypename Declaration { get; set; }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitUnresolvedUsingType(this, quals);
        }

        public override object Clone()
        {
            return new UnresolvedUsingType(this);
        }

        public override int GetHashCode() => Declaration.GetHashCode();
    }

    public class VectorType : Type
    {
        public VectorType()
        {
        }

        public VectorType(VectorType type)
            : base(type)
        {
        }

        public QualifiedType ElementType { get; set; }
        public uint NumElements { get; set; }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitVectorType(this, quals);
        }

        public override object Clone()
        {
            return new VectorType(this);
        }

        public override int GetHashCode() =>
            ElementType.GetHashCode() ^ NumElements.GetHashCode();
    }

    public class UnsupportedType : Type
    {
        public UnsupportedType()
        {
        }

        public UnsupportedType(string description)
        {
            Description = description;
        }

        public UnsupportedType(UnsupportedType type)
            : base(type)
        {
        }

        public string Description;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitUnsupportedType(this, quals);
        }

        public override object Clone()
        {
            return new UnsupportedType(this);
        }

        public override int GetHashCode() => Description.GetHashCode();
    }

    public class CustomType : UnsupportedType
    {
        public CustomType(string description) : base(description)
        {
        }
    }

    #region Primitives

    /// <summary>
    /// Represents the C++ built-in types.
    /// </summary>
    public enum PrimitiveType
    {
        Null,
        Void,
        Bool,
        WideChar,
        Char,
        SChar,
        UChar,
        Char16,
        Char32,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        LongLong,
        ULongLong,
        Int128,
        UInt128,
        Half,
        Float,
        Double,
        LongDouble,
        Float128,
        IntPtr,
        UIntPtr,
        String,
        Decimal
    }

    /// <summary>
    /// Represents an instance of a C++ built-in type.
    /// </summary>
    public class BuiltinType : Type
    {
        public BuiltinType()
        {
        }

        public BuiltinType(PrimitiveType type)
        {
            Type = type;
        }

        public BuiltinType(BuiltinType type)
            : base(type)
        {
            Type = type.Type;
        }

        public bool IsUnsigned
        {
            get
            {
                switch (Type)
                {
                case PrimitiveType.Bool:
                case PrimitiveType.UChar:
                case PrimitiveType.UShort:
                case PrimitiveType.UInt:
                case PrimitiveType.ULong:
                case PrimitiveType.ULongLong:
                    return true;
                }

                return false;
            }
        }

        // Primitive type of built-in type.
        public PrimitiveType Type;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitBuiltinType(this, quals);
        }

        public override object Clone()
        {
            return new BuiltinType(this);
        }

        public override bool Equals(object obj)
        {
            var type = obj as BuiltinType;
            if (type == null) return false;

            return Type == type.Type;
        }

        public override int GetHashCode() => Type.GetHashCode();
    }

    #endregion

    public interface ITypeVisitor<out T>
    {
        T VisitTagType(TagType tag, TypeQualifiers quals);
        T VisitArrayType(ArrayType array, TypeQualifiers quals);
        T VisitFunctionType(FunctionType function, TypeQualifiers quals);
        T VisitPointerType(PointerType pointer, TypeQualifiers quals);
        T VisitMemberPointerType(MemberPointerType member, TypeQualifiers quals);
        T VisitBuiltinType(BuiltinType builtin, TypeQualifiers quals);
        T VisitTypedefType(TypedefType typedef, TypeQualifiers quals);
        T VisitAttributedType(AttributedType attributed, TypeQualifiers quals);
        T VisitDecayedType(DecayedType decayed, TypeQualifiers quals);
        T VisitTemplateSpecializationType(TemplateSpecializationType template,
                                          TypeQualifiers quals);
        T VisitDependentTemplateSpecializationType(
            DependentTemplateSpecializationType template, TypeQualifiers quals);
        T VisitPrimitiveType(PrimitiveType type, TypeQualifiers quals);
        T VisitDeclaration(Declaration decl, TypeQualifiers quals);
        T VisitTemplateParameterType(TemplateParameterType param,
            TypeQualifiers quals);
        T VisitTemplateParameterSubstitutionType(
            TemplateParameterSubstitutionType param, TypeQualifiers quals);
        T VisitInjectedClassNameType(InjectedClassNameType injected,
            TypeQualifiers quals);
        T VisitDependentNameType(DependentNameType dependent,
            TypeQualifiers quals);
        T VisitPackExpansionType(PackExpansionType packExpansionType, TypeQualifiers quals);
        T VisitUnaryTransformType(UnaryTransformType unaryTransformType, TypeQualifiers quals);
        T VisitUnresolvedUsingType(UnresolvedUsingType unresolvedUsingType, TypeQualifiers quals);
        T VisitVectorType(VectorType vectorType, TypeQualifiers quals);
        T VisitCILType(CILType type, TypeQualifiers quals);
        T VisitUnsupportedType(UnsupportedType type, TypeQualifiers quals);
    }
}
