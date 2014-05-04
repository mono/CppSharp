/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#pragma once

#include "Helpers.h"

namespace CppSharp { namespace CppParser { namespace AST {

#pragma region Types

enum struct TypeKind
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
    TemplateParameter,
    TemplateParameterSubstitution,
    InjectedClassName,
    DependentName,
    PackExpansion,
    Builtin
};

#define DECLARE_TYPE_KIND(kind) \
    kind##Type();

struct CS_API Type
{
    Type(TypeKind kind);
    Type(const Type&);

    TypeKind Kind;
    bool IsDependent;
};

struct CS_API TypeQualifiers
{
    bool IsConst;
    bool IsVolatile;
    bool IsRestrict;
};

struct CS_API QualifiedType
{
    QualifiedType();
    CppSharp::CppParser::AST::Type* Type;
    TypeQualifiers Qualifiers;
};

struct Declaration;

struct CS_API TagType : public Type
{
    DECLARE_TYPE_KIND(Tag)
    CppSharp::CppParser::AST::Declaration* Declaration;
};

struct CS_API ArrayType : public Type
{
    enum class ArraySize
    {
        Constant,
        Variable,
        Dependent,
        Incomplete
    };

    DECLARE_TYPE_KIND(Array)
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
    ArraySize SizeType;
    long Size;
};

struct Parameter;

enum class CallingConvention
{
    Default,
    C,
    StdCall,
    ThisCall,
    FastCall,
    Unknown
};

struct CS_API FunctionType : public Type
{
    DECLARE_TYPE_KIND(Function)
    QualifiedType ReturnType;
    CppSharp::CppParser::AST::CallingConvention CallingConvention;
    VECTOR(Parameter*, Parameters)
};

struct CS_API PointerType : public Type
{
    enum struct TypeModifier
    {
        Value,
        Pointer,
        LVReference,
        RVReference
    };

    DECLARE_TYPE_KIND(Pointer)
    QualifiedType QualifiedPointee;
    TypeModifier Modifier;
};

struct CS_API MemberPointerType : public Type
{
    DECLARE_TYPE_KIND(MemberPointer)
    QualifiedType Pointee;
};

struct TypedefDecl;

struct CS_API TypedefType : public Type
{
    TypedefType();
    TypedefDecl* Declaration;
};

struct CS_API AttributedType : public Type
{
    DECLARE_TYPE_KIND(Attributed)
    QualifiedType Modified;
    QualifiedType Equivalent;
};

struct CS_API DecayedType : public Type
{
    DECLARE_TYPE_KIND(Decayed)
    QualifiedType Decayed;
    QualifiedType Original;
    QualifiedType Pointee;
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

    ArgumentKind Kind;
    QualifiedType Type;
    CppSharp::CppParser::AST::Declaration* Declaration;
    long Integral;
};

struct Template;

struct CS_API TemplateSpecializationType : public Type
{
    TemplateSpecializationType();
    TemplateSpecializationType(const TemplateSpecializationType&);

    VECTOR(TemplateArgument, Arguments)
    CppSharp::CppParser::AST::Template* Template;
    Type* Desugared;
};

struct CS_API TemplateParameter
{
    TemplateParameter();
    TemplateParameter(const TemplateParameter&);

    bool operator==(const TemplateParameter& param) const
    {
        return Name == param.Name;
    }

    STRING(Name)
};

struct CS_API TemplateParameterType : public Type
{
    DECLARE_TYPE_KIND(TemplateParameter)
    TemplateParameter Parameter;
};

struct CS_API TemplateParameterSubstitutionType : public Type
{
    DECLARE_TYPE_KIND(TemplateParameterSubstitution)
    QualifiedType Replacement;
};

struct Class;

struct CS_API InjectedClassNameType : public Type
{
    InjectedClassNameType();
    TemplateSpecializationType TemplateSpecialization;
    CppSharp::CppParser::AST::Class* Class;
};

struct CS_API DependentNameType : public Type
{
    DECLARE_TYPE_KIND(DependentName)
};

struct CS_API PackExpansionType : public Type
{
    DECLARE_TYPE_KIND(PackExpansion)
};

enum struct PrimitiveType
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
};

struct CS_API BuiltinType : public Type
{
    DECLARE_TYPE_KIND(Builtin)
    PrimitiveType Type;
};

#pragma endregion

#pragma region ABI

enum struct CppAbi
{
    Itanium,
    Microsoft,
    ARM
};

enum struct VTableComponentKind
{
    VCallOffset,
    VBaseOffset,
    OffsetToTop,
    RTTI,
    FunctionPointer,
    CompleteDtorPointer,
    DeletingDtorPointer,
    UnusedFunctionPointer,
};

struct CS_API VTableComponent
{
    VTableComponent();
    VTableComponentKind Kind;
    unsigned Offset;
    CppSharp::CppParser::AST::Declaration* Declaration;
};

struct CS_API VTableLayout
{
    VTableLayout();
    VTableLayout(const VTableLayout&);
    VECTOR(VTableComponent, Components)
};

struct CS_API VFTableInfo
{
    VFTableInfo();
    VFTableInfo(const VFTableInfo&);
    uint64_t VBTableIndex;
    uint32_t VFPtrOffset;
    uint32_t VFPtrFullOffset;
    VTableLayout Layout;
};

struct CS_API ClassLayout
{
    ClassLayout();
    CppAbi ABI;
    VECTOR(VFTableInfo, VFTables)
    VTableLayout Layout;
    bool HasOwnVFPtr;
    long VBPtrOffset;
    int Alignment;
    int Size;
    int DataSize;
};

#pragma endregion

#pragma region Declarations

enum struct DeclarationKind
{
    DeclarationContext,
    Typedef,
    Parameter,
    Function,
    Method,
    Enumeration,
    EnumerationItem,
    Variable,
    Field,
    AccessSpecifier,
    Class,
    Template,
    ClassTemplate,
    ClassTemplateSpecialization,
    ClassTemplatePartialSpecialization,
    FunctionTemplate,
    Namespace,
    PreprocessedEntity,
    MacroDefinition,
    MacroExpansion,
    TranslationUnit
};

#define DECLARE_DECL_KIND(klass, kind) \
    klass();

enum struct AccessSpecifier
{
    Private,
    Protected,
    Public
};

struct DeclarationContext;
struct RawComment;
struct PreprocessedEntity;

struct CS_API Declaration
{
    Declaration(DeclarationKind kind);
    Declaration(const Declaration&);

    DeclarationKind Kind;
    AccessSpecifier Access;
    DeclarationContext* _Namespace;
    STRING(Name)
    RawComment* Comment;
    STRING(DebugText)
    bool IsIncomplete;
    bool IsDependent;
    Declaration* CompleteDeclaration;
    unsigned DefinitionOrder;
    VECTOR(PreprocessedEntity*, PreprocessedEntities)
    void* OriginalPtr;
};

struct Class;
struct Enumeration;
struct Function;
struct TypedefDecl;
struct Namespace;
struct Template;
struct FunctionTemplate;
struct Variable;

struct CS_API DeclarationContext : public Declaration
{
    DECLARE_DECL_KIND(DeclarationContext, DeclarationContext)

    CS_IGNORE Declaration* FindAnonymous(uint64_t key);

    CS_IGNORE CppSharp::CppParser::AST::Namespace* FindNamespace(const std::string& Name);
    CS_IGNORE CppSharp::CppParser::AST::Namespace* FindNamespace(const std::vector<std::string>&);
    CS_IGNORE CppSharp::CppParser::AST::Namespace* FindCreateNamespace(const std::string& Name);

    CS_IGNORE Class* CreateClass(std::string Name, bool IsComplete);
    CS_IGNORE Class* FindClass(const std::string& Name);
    CS_IGNORE Class* FindClass(const std::string& Name, bool IsComplete,
        bool Create = false);

    CS_IGNORE FunctionTemplate* FindFunctionTemplate(void* OriginalPtr);
    CS_IGNORE FunctionTemplate* FindFunctionTemplate(const std::string& Name,
        const std::vector<TemplateParameter>& Params);

    CS_IGNORE Enumeration* FindEnum(const std::string& Name, bool Create = false);

    CS_IGNORE Function* FindFunction(const std::string& Name, bool Create = false);

    CS_IGNORE TypedefDecl* FindTypedef(const std::string& Name, bool Create = false);

    VECTOR(Namespace*, Namespaces)
    VECTOR(Enumeration*, Enums)
    VECTOR(Function*, Functions)
    VECTOR(Class*, Classes)
    VECTOR(Template*, Templates)
    VECTOR(TypedefDecl*, Typedefs)
    VECTOR(Variable*, Variables)
    std::map<uint64_t, Declaration*> Anonymous;

    bool IsAnonymous;
};

struct CS_API TypedefDecl : public Declaration
{
    DECLARE_DECL_KIND(TypedefDecl, Typedef)
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
};

struct CS_API Parameter : public Declaration
{
    Parameter();

    CppSharp::CppParser::AST::QualifiedType QualifiedType;
    bool IsIndirect;
    bool HasDefaultValue;
};

enum struct CXXMethodKind
{
    Normal,
    Constructor,
    Destructor,
    Conversion,
    Operator,
    UsingDirective
};

enum struct CXXOperatorKind
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
    Conditional
};

struct CS_API Function : public Declaration
{
    Function();

    QualifiedType ReturnType;
    bool IsReturnIndirect;

    bool IsVariadic;
    bool IsInline;
    bool IsPure;
    bool IsDeleted;
    CXXOperatorKind OperatorKind;
    STRING(Mangled)
    STRING(Signature)
    CppSharp::CppParser::AST::CallingConvention CallingConvention;
    VECTOR(Parameter*, Parameters)
};

struct AccessSpecifierDecl;

struct CS_API Method : public Function
{
    Method();

    AccessSpecifierDecl* AccessDecl;

    bool IsVirtual;
    bool IsStatic;
    bool IsConst;
    bool IsImplicit;
    bool IsExplicit;
    bool IsOverride;

    CXXMethodKind MethodKind;

    bool IsDefaultConstructor;
    bool IsCopyConstructor;
    bool IsMoveConstructor;

    QualifiedType ConversionType;
};

struct CS_API Enumeration : public Declaration
{
    DECLARE_DECL_KIND(Enumeration, Enumeration)

    struct CS_API Item : public Declaration
    {
        DECLARE_DECL_KIND(Item, EnumerationItem)
        Item(const Item&);

        STRING(Expression)
        uint64_t Value;
    };

    enum struct CS_FLAGS EnumModifiers
    {
        Anonymous = 1 << 0,
        Scoped = 1 << 1,
        Flags  = 1 << 2,
    };

    EnumModifiers Modifiers;
    CppSharp::CppParser::AST::Type* Type;
    CppSharp::CppParser::AST::BuiltinType* BuiltinType;
    VECTOR(Item, Items)
};

struct CS_API Variable : public Declaration
{
    DECLARE_DECL_KIND(Variable, Variable)

    STRING(Mangled)
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
};

struct DeclarationContext;
struct PreprocessedEntity;

struct CS_API BaseClassSpecifier
{
    BaseClassSpecifier();
    AccessSpecifier Access;
    bool IsVirtual;
    CppSharp::CppParser::AST::Type* Type;
};

struct Class;

struct CS_API Field : public Declaration
{
    DECLARE_DECL_KIND(Field, Field)
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
    unsigned Offset;
    CppSharp::CppParser::AST::Class* Class;
};

struct CS_API AccessSpecifierDecl : public Declaration
{
    DECLARE_DECL_KIND(AccessSpecifierDecl, AccessSpecifier)
};

struct CS_API Class : public DeclarationContext
{
    Class();

    VECTOR(BaseClassSpecifier*, Bases)
    VECTOR(Field*, Fields)
    VECTOR(Method*, Methods)
    VECTOR(AccessSpecifierDecl*, Specifiers)

    bool IsPOD;
    bool IsAbstract;
    bool IsUnion;
    bool IsDynamic;
    bool IsPolymorphic;
    bool HasNonTrivialDefaultConstructor;
    bool HasNonTrivialCopyConstructor;
    bool HasNonTrivialDestructor;
    bool IsExternCContext;

    ClassLayout* Layout;
};

struct CS_API Template : public Declaration
{
    DECLARE_DECL_KIND(Template, Template)
    Declaration* TemplatedDecl;
    VECTOR(TemplateParameter, Parameters)
};

struct ClassTemplateSpecialization;
struct ClassTemplatePartialSpecialization;

struct CS_API ClassTemplate : public Template
{
    ClassTemplate();
    VECTOR(ClassTemplateSpecialization*, Specializations)
    ClassTemplateSpecialization* FindSpecialization(void* ptr);
    ClassTemplateSpecialization* FindSpecialization(TemplateSpecializationType type);
    ClassTemplatePartialSpecialization* FindPartialSpecialization(void* ptr);
    ClassTemplatePartialSpecialization* FindPartialSpecialization(TemplateSpecializationType type);
};

enum struct TemplateSpecializationKind
{
    Undeclared,
    ImplicitInstantiation,
    ExplicitSpecialization,
    ExplicitInstantiationDeclaration,
    ExplicitInstantiationDefinition
};

struct CS_API ClassTemplateSpecialization : public Class
{
    ClassTemplateSpecialization();
    ClassTemplate* TemplatedDecl;
    VECTOR(TemplateArgument, Arguments)
    TemplateSpecializationKind SpecializationKind;
};

struct CS_API ClassTemplatePartialSpecialization : public ClassTemplateSpecialization
{
    ClassTemplatePartialSpecialization();
};

struct CS_API FunctionTemplate : public Template
{
    FunctionTemplate();
};

struct CS_API Namespace : public DeclarationContext
{
    Namespace();
    bool IsInline;
};

enum struct MacroLocation
{
    Unknown,
    ClassHead,
    ClassBody,
    FunctionHead,
    FunctionParameters,
    FunctionBody,
};

struct CS_API PreprocessedEntity : public Declaration
{
    PreprocessedEntity();
    MacroLocation Location;
};

struct CS_API MacroDefinition : public PreprocessedEntity
{
    MacroDefinition();
    STRING(Expression)
};

struct CS_API MacroExpansion : public PreprocessedEntity
{
    MacroExpansion();
    STRING(Text)
    MacroDefinition* Definition;
};

struct CS_API TranslationUnit : public Namespace
{
    TranslationUnit();
    STRING(FileName)
    bool IsSystemHeader;
    VECTOR(MacroDefinition*, Macros)
};

struct CS_API NativeLibrary
{
    STRING(FileName)
    VECTOR_STRING(Symbols)
};

struct CS_API ASTContext
{
    ASTContext();
    TranslationUnit* FindOrCreateModule(std::string File);
    VECTOR(TranslationUnit*, TranslationUnits)
};

#pragma endregion

#pragma region Comments

enum struct CommentKind
{
    FullComment,
};

struct CS_API CS_ABSTRACT Comment
{
    Comment(CommentKind kind);
    CommentKind Kind;
};

struct CS_API FullComment : public Comment
{
    FullComment();
};

enum struct RawCommentKind
{
    Invalid,
    OrdinaryBCPL,
    OrdinaryC,
    BCPLSlash,
    BCPLExcl,
    JavaDoc,
    Qt,
    Merged
};

struct CS_API RawComment
{
    RawComment();
    RawCommentKind RawCommentKind;
    STRING(Text)
    STRING(BriefText)
    FullComment* FullComment;
};

#pragma region Commands

#pragma endregion

#pragma endregion

} } }