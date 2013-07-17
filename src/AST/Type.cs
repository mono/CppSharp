using System;
using System.Collections.Generic;

namespace CppSharp.AST
{
    /// <summary>
    /// Represents a C++ type.
    /// </summary>
    public abstract class Type
    {
        public static Func<Type, string> TypePrinterDelegate;

        protected Type()
        {
        }

        public bool IsPrimitiveType(out PrimitiveType primitive)
        {
            var builtin = this as BuiltinType;
            if (builtin != null)
            {
                primitive = builtin.Type;
                return true;
            }

            primitive = PrimitiveType.Null;
            return false;
        }

        public bool IsPrimitiveType(PrimitiveType primitive)
        {
            PrimitiveType type;
            if (!IsPrimitiveType(out type))
                return false;

            return primitive == type;
        }

        public bool IsEnumType()
        {
            var tag = this as TagType;
            
            if (tag == null)
                return false;

            return tag.Declaration is Enumeration;
        }

        public bool IsPointer()
        {
            var pointer = this as PointerType;
            if (pointer == null)
                return false;
            return pointer.Modifier == PointerType.TypeModifier.Pointer;
        }

        public bool IsReference()
        {
            var pointer = this as PointerType;
            if (pointer == null)
                return false;
            return pointer.IsReference;
        }

        public bool IsPointerToPrimitiveType(PrimitiveType primitive)
        {
            var ptr = this as PointerType;
            if (ptr == null)
                return false;
            return ptr.Pointee.IsPrimitiveType(primitive);
        }

        public bool IsPointerTo<T>(out T type) where T : Type
        {
            var ptr = this as PointerType;
            
            if (ptr == null)
            {
                type = null;
                return false;
            }
            
            type = ptr.Pointee as T;
            return type != null;
        }

        public bool IsTagDecl<T>(out T decl) where T : Declaration
        {
            var tag = this as TagType;
            
            if (tag == null)
            {
                decl = null;
                return false;
            }

            decl = tag.Declaration as T;
            return decl != null;
        }

        public Type Desugar()
        {
            var type = this as TypedefType;

            if (type != null)
            {
                var decl = type.Declaration.Type;

                if (decl != null)
                    return decl.Desugar();
            }

            return this;
        }

        public abstract T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals
            = new TypeQualifiers());

        public override string ToString()
        {
            return TypePrinterDelegate(this);
        }
    }

    /// <summary>
    /// Represents C++ type qualifiers.
    /// </summary>
    public struct TypeQualifiers
    {
        public bool IsConst;
        public bool IsVolatile;
        public bool IsRestrict;
    }

    /// <summary>
    /// Represents a qualified C++ type.
    /// </summary>
    public struct QualifiedType
    {
        public QualifiedType(Type type) : this()
        {
            Type = type;
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

        public Declaration Declaration;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitTagType(this, quals);
        }
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

        public ArrayType()
        {
        }

        // Type of the array elements.
        public Type Type;

        // Size type of array.
        public ArraySize SizeType;

        // In case of a constant size array.
        public long Size;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitArrayType(this, quals);
        }
    }

    /// <summary>
    /// Represents an C/C++ function type.
    /// </summary>
    public class FunctionType : Type
    {
        // Return type of the function.
        public QualifiedType ReturnType;

        // Argument types.
        public List<Parameter> Parameters;

        public FunctionType()
        {
            Parameters = new List<Parameter>();
        }

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitFunctionType(this, quals);
        }
    }

    /// <summary>
    /// Represents a C++ pointer/reference type.
    /// </summary>
    public class PointerType : Type
    {
        public PointerType()
        {
            Modifier = TypeModifier.Pointer;
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

        public new bool IsReference
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

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitPointerType(this, QualifiedPointee.Qualifiers);
        }
    }

    /// <summary>
    /// Represents a C++ member function pointer type.
    /// </summary>
    public class MemberPointerType : Type
    {
        public MemberPointerType()
        {

        }

        public Type Pointee;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitMemberPointerType(this, quals);
        }
    }

    /// <summary>
    /// Represents a C/C++ typedef type.
    /// </summary>
    public class TypedefType : Type
    {
        public TypedefType()
        {

        }

        public TypedefDecl Declaration;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitTypedefType(this, quals);
        }
    }

    /// <summary>
    /// Represents a pointer type decayed from an array or function type.
    /// </summary>
    public class DecayedType : Type
    {
        public DecayedType()
        {

        }

        public QualifiedType Decayed;
        public QualifiedType Original;
        public QualifiedType Pointee;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitDecayedType(this, quals);
        }
    }

    /// <summary>
    /// Represents a template argument within a class template specialization.
    /// </summary>
    public struct TemplateArgument
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

        public List<TemplateArgument> Arguments;

        public Template Template;

        public Type Desugared;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTemplateSpecializationType(this, quals);
        }
    }

    /// <summary>
    /// Represents a C++ template parameter type.
    /// </summary>
    public class TemplateParameterType : Type
    {
        public TemplateParameterType()
        {
        }

        public TemplateParameter Parameter;
        public Template Template;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTemplateParameterType(this, quals);
        }
    }

    /// <summary>
    /// Represents the result of substituting a type for a template type parameter.
    /// </summary>
    public class TemplateParameterSubstitutionType : Type
    {
        public TemplateParameterSubstitutionType()
        {

        }

        public QualifiedType Replacement;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitTemplateParameterSubstitutionType(this, quals);
        }
    }

    /// <summary>
    /// The injected class name of a C++ class template or class template partial
    /// specialization.
    /// </summary>
    public class InjectedClassNameType : Type
    {
        public InjectedClassNameType()
        {

        }

        public TemplateSpecializationType TemplateSpecialization;
        public Class Class;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitInjectedClassNameType(this, quals);
        }
    }

    /// <summary>
    /// Represents a qualified type name for which the type name is dependent.
    /// </summary>
    public class DependentNameType : Type
    {
        public DependentNameType()
        {

        }

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitDependentNameType(this, quals);
        }
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

        public System.Type Type;

        public override T Visit<T>(ITypeVisitor<T> visitor,
                                   TypeQualifiers quals = new TypeQualifiers())
        {
            return visitor.VisitCILType(this, quals);
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
        Int8,
        Char = Int8,
        UInt8,
        UChar = UInt8,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
        IntPtr
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

        // Primitive type of built-in type.
        public PrimitiveType Type;

        public override T Visit<T>(ITypeVisitor<T> visitor, TypeQualifiers quals)
        {
            return visitor.VisitBuiltinType(this, quals);
        }
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
        T VisitDecayedType(DecayedType decayed, TypeQualifiers quals);
        T VisitTemplateSpecializationType(TemplateSpecializationType template,
                                          TypeQualifiers quals);
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
        T VisitCILType(CILType type, TypeQualifiers quals);
    }
}