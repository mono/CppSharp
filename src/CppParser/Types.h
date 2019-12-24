/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#pragma once

#include "Helpers.h"

namespace CppSharp { namespace CppParser { namespace AST {

enum class TypeKind
{
    Tag,
    Array,
    Function,
    Pointer,
    MemberPointer,
    Typedef,
    Attributed,
    Decayed,
    TemplateSpecialization,
    DependentTemplateSpecialization,
    TemplateParameter,
    TemplateParameterSubstitution,
    InjectedClassName,
    DependentName,
    PackExpansion,
    Builtin,
    UnaryTransform,
    UnresolvedUsing,
    Vector
};

#define DECLARE_TYPE_KIND(kind) \
kind##Type();

class CS_API Type
{
public:
    Type(TypeKind kind);
    Type(const Type&);

    TypeKind kind;
    bool isDependent;
};

struct CS_API TypeQualifiers
{
    bool isConst;
    bool isVolatile;
    bool isRestrict;
};

struct CS_API QualifiedType
{
    QualifiedType();
    Type* type;
    TypeQualifiers qualifiers;
};

class Declaration;

class CS_API TagType : public Type
{
public:
    DECLARE_TYPE_KIND(Tag)
    Declaration* declaration;
};

class CS_API ArrayType : public Type
{
public:
    enum class ArraySize
    {
        Constant,
        Variable,
        Dependent,
        Incomplete
    };

    DECLARE_TYPE_KIND(Array)
    QualifiedType qualifiedType;
    ArraySize sizeType;
    long size;
    long elementSize;
};

class Parameter;

enum class CallingConvention
{
    Default,
    C,
    StdCall,
    ThisCall,
    FastCall,
    Unknown
};

enum class ExceptionSpecType
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
};

class CS_API FunctionType : public Type
{
public:
    DECLARE_TYPE_KIND(Function)
    ~FunctionType();
    QualifiedType returnType;
    CallingConvention callingConvention;
    ExceptionSpecType exceptionSpecType;
    VECTOR(Parameter*, Parameters)
};

class CS_API PointerType : public Type
{
public:
    enum struct TypeModifier
    {
        Value,
        Pointer,
        LVReference,
        RVReference
    };

    DECLARE_TYPE_KIND(Pointer)
    QualifiedType qualifiedPointee;
    TypeModifier modifier;
};

class CS_API MemberPointerType : public Type
{
public:
    DECLARE_TYPE_KIND(MemberPointer)
    QualifiedType pointee;
};

class TypedefNameDecl;

class CS_API TypedefType : public Type
{
public:
    TypedefType();
    TypedefNameDecl* declaration;
};

class CS_API AttributedType : public Type
{
public:
    DECLARE_TYPE_KIND(Attributed)
    QualifiedType modified;
    QualifiedType equivalent;
};

class CS_API DecayedType : public Type
{
public:
    DECLARE_TYPE_KIND(Decayed)
    QualifiedType decayed;
    QualifiedType original;
    QualifiedType pointee;
};

struct CS_API TemplateArgument
{
    TemplateArgument();

    enum struct ArgumentKind
    {
        Type,
        Declaration,
        NullPtr,
        Integral,
        Template,
        TemplateExpansion,
        Expression,
        Pack
    };

    ArgumentKind kind;
    QualifiedType type;
    Declaration* declaration;
    long integral;
};

class Template;

class CS_API TemplateSpecializationType : public Type
{
public:
    TemplateSpecializationType();
    TemplateSpecializationType(const TemplateSpecializationType&);
    ~TemplateSpecializationType();

    VECTOR(TemplateArgument, Arguments)
        Template* _template;
    QualifiedType desugared;
};

class CS_API DependentTemplateSpecializationType : public Type
{
public:
    DependentTemplateSpecializationType();
    DependentTemplateSpecializationType(const DependentTemplateSpecializationType&);
    ~DependentTemplateSpecializationType();

    VECTOR(TemplateArgument, Arguments)
        QualifiedType desugared;
};

class TypeTemplateParameter;

class CS_API TemplateParameterType : public Type
{
public:
    DECLARE_TYPE_KIND(TemplateParameter)
    ~TemplateParameterType();
    TypeTemplateParameter* parameter;
    unsigned int depth;
    unsigned int index;
    bool isParameterPack;
};

class CS_API TemplateParameterSubstitutionType : public Type
{
public:
    DECLARE_TYPE_KIND(TemplateParameterSubstitution)
    QualifiedType replacement;
    TemplateParameterType* replacedParameter;
};

class Class;

class CS_API InjectedClassNameType : public Type
{
public:
    DECLARE_TYPE_KIND(InjectedClassName)
    QualifiedType injectedSpecializationType;
    Class* _class;
};

class CS_API DependentNameType : public Type
{
public:
    DECLARE_TYPE_KIND(DependentName)
    ~DependentNameType();
    QualifiedType qualifier;
    std::string identifier;
};

class CS_API PackExpansionType : public Type
{
public:
    DECLARE_TYPE_KIND(PackExpansion)
};

class CS_API UnaryTransformType : public Type
{
public:
    DECLARE_TYPE_KIND(UnaryTransform)
    QualifiedType desugared;
    QualifiedType baseType;
};

class UnresolvedUsingTypename;

class CS_API UnresolvedUsingType : public Type
{
public:
    DECLARE_TYPE_KIND(UnresolvedUsing)
    UnresolvedUsingTypename* declaration;
};

class CS_API VectorType : public Type
{
public:
    DECLARE_TYPE_KIND(Vector)
    QualifiedType elementType;
    unsigned numElements;
};

enum class PrimitiveType
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
    IntPtr
};

class CS_API BuiltinType : public Type
{
public:
    DECLARE_TYPE_KIND(Builtin)
    PrimitiveType type;
};

} } }
