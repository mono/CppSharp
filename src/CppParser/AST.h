/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#pragma once

#include <cstdint>
#include <vector>
#include <map>
#include <string>

#define CS_FLAGS

#if defined(_MSC_VER) && !defined(__clang__)
#define CS_API __declspec(dllexport)
#else
#define CS_API 
#endif

#define VECTOR(type, name) \
    std::vector<type> name; \
    type get##name (unsigned i) { return name[i]; } \
    unsigned get##name##Count () { return name.size(); }

namespace CppSharp { namespace CppParser { namespace AST {

// Types

struct CS_API Type
{
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
    CppSharp::CppParser::AST::Type* Type;
    TypeQualifiers Qualifiers;
};

struct Declaration;

struct CS_API TagType : public Type
{
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

    QualifiedType QualifiedPointee;
    TypeModifier Modifier;
};

struct CS_API MemberPointerType : public Type
{
    QualifiedType Pointee;
};

struct TypedefDecl;

struct CS_API TypedefType : public Type
{
    TypedefDecl* Declaration;
};

struct CS_API AttributedType : public Type
{
    QualifiedType Modified;
    QualifiedType Equivalent;
};

struct CS_API DecayedType : public Type
{
    QualifiedType Decayed;
    QualifiedType Original;
    QualifiedType Pointee;
};

struct CS_API TemplateArgument
{
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
    VECTOR(TemplateArgument, Arguments)
    CppSharp::CppParser::AST::Template* Template;
    Type* Desugared;
};

struct CS_API TemplateParameter
{
    std::string Name;
};

struct CS_API TemplateParameterType : public Type
{
    TemplateParameter Parameter;
};

struct CS_API TemplateParameterSubstitutionType : public Type
{
    QualifiedType Replacement;
};

struct Class;

struct CS_API InjectedClassNameType : public Type
{
    TemplateSpecializationType TemplateSpecialization;
    CppSharp::CppParser::AST::Class* Class;
};

struct CS_API DependentNameType : public Type
{

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
    PrimitiveType Type;
};

// Comments

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

struct FullComment;

struct CS_API RawComment
{
    RawCommentKind Kind;
    std::string Text;
    std::string BriefText;
    CppSharp::CppParser::AST::FullComment* FullComment;
};

// Class layouts

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
    VTableComponentKind Kind;
    unsigned Offset;
    CppSharp::CppParser::AST::Declaration* Declaration;
};

struct CS_API VTableLayout
{
    VECTOR(VTableComponent, Components)
};

struct CS_API VFTableInfo
{
    uint64_t VBTableIndex;
    uint32_t VFPtrOffset;
    uint32_t VFPtrFullOffset;
    VTableLayout Layout;
};

enum struct CppAbi
{
    Itanium,
    Microsoft,
    ARM
};

struct CS_API ClassLayout
{
    CppAbi ABI;
    VECTOR(VFTableInfo, VFTables)
    VTableLayout Layout;
    bool HasOwnVFPtr;
    long VBPtrOffset;
    int Alignment;
    int Size;
    int DataSize;
};

// Declarations

enum struct MacroLocation
{
    Unknown,
    ClassHead,
    ClassBody,
    FunctionHead,
    FunctionParameters,
    FunctionBody,
};

enum struct AccessSpecifier
{
    Private,
    Protected,
    Public
};

struct DeclarationContext;
struct PreprocessedEntity;

struct CS_API Declaration
{
    Declaration();

    AccessSpecifier Access;
    DeclarationContext* _Namespace;
    std::string Name;
    RawComment* Comment;
    std::string DebugText;
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
struct Variable;

struct CS_API DeclarationContext : public Declaration
{
    Declaration* FindAnonymous(uint64_t key);

    CppSharp::CppParser::AST::Namespace* FindNamespace(const std::string& Name);
    CppSharp::CppParser::AST::Namespace* FindNamespace(const std::vector<std::string>&);
    CppSharp::CppParser::AST::Namespace* FindCreateNamespace(const std::string& Name);

    Class* CreateClass(std::string Name, bool IsComplete);
    Class* FindClass(const std::string& Name);
    Class* FindClass(const std::string& Name, bool IsComplete,
        bool Create = false);

    Enumeration* FindEnum(const std::string& Name, bool Create = false);

    Function* FindFunction(const std::string& Name, bool Create = false);

    TypedefDecl* FindTypedef(const std::string& Name, bool Create = false);

    VECTOR(Namespace*, Namespaces)
    VECTOR(Enumeration*, Enums)
    VECTOR(Function*, Functions)
    VECTOR(Class*, Classes)
    VECTOR(Template*, Templates)
    VECTOR(TypedefDecl*, Typedefs)
    VECTOR(Variable*, Variables)
    std::map<uint64_t, Declaration*> Anonymous;
};

struct CS_API TypedefDecl : public Declaration
{
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
};

struct CS_API Parameter : public Declaration
{
    Parameter() : IsIndirect(false) {}

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
    Function() : IsReturnIndirect(false) {}

    QualifiedType ReturnType;
    bool IsReturnIndirect;

    bool IsVariadic;
    bool IsInline;
    bool IsPure;
    bool IsDeleted;
    CXXOperatorKind OperatorKind;
    std::string Mangled;
    std::string Signature;
    CppSharp::CppParser::AST::CallingConvention CallingConvention;
    VECTOR(Parameter*, Parameters)
};

struct AccessSpecifierDecl;

struct CS_API Method : public Function
{
    AccessSpecifierDecl* AccessDecl;

    bool IsVirtual;
    bool IsStatic;
    bool IsConst;
    bool IsImplicit;
    bool IsOverride;

    CXXMethodKind Kind;

    bool IsDefaultConstructor;
    bool IsCopyConstructor;
    bool IsMoveConstructor;

    QualifiedType ConversionType;
};

struct CS_API Enumeration : public Declaration
{
    struct CS_API Item : public Declaration
    {
        std::string Expression;
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
    std::string Mangled;
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
};

struct CS_API BaseClassSpecifier
{
    AccessSpecifier Access;
    bool IsVirtual;
    CppSharp::CppParser::AST::Type* Type;
};

struct Class;

struct CS_API Field : public Declaration
{
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
    AccessSpecifier Access;
    unsigned Offset;
    CppSharp::CppParser::AST::Class* Class;
};


struct CS_API AccessSpecifierDecl : public Declaration
{

};

struct CS_API Class : public DeclarationContext
{
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

    ClassLayout Layout;
};

struct CS_API Template : public Declaration
{
    Declaration* TemplatedDecl;
    VECTOR(TemplateParameter, Parameters)
};

struct CS_API ClassTemplate : public Template
{
};

struct CS_API ClassTemplateSpecialization : public Class
{
};

struct CS_API ClassTemplatePartialSpecialization : public ClassTemplateSpecialization
{
};

struct CS_API FunctionTemplate : public Template
{
};

struct CS_API Namespace : public DeclarationContext
{

};

struct CS_API PreprocessedEntity : public Declaration
{
    PreprocessedEntity() : Location(MacroLocation::Unknown) {}

    MacroLocation Location;
};

struct CS_API MacroDefinition : public PreprocessedEntity
{
    std::string Expression;
};

struct CS_API MacroExpansion : public PreprocessedEntity
{
    std::string Text;
    MacroDefinition* Definition;
};


struct CS_API TranslationUnit : public Namespace
{
    std::string FileName;
    bool IsSystemHeader;
    VECTOR(Namespace*, Namespaces)
    VECTOR(MacroDefinition*, Macros)
};

struct CS_API NativeLibrary
{
    std::string FileName;
    VECTOR(std::string, Symbols)
};

struct CS_API ASTContext
{
    TranslationUnit* FindOrCreateModule(const std::string& File);
    VECTOR(TranslationUnit*, TranslationUnits)
};

} } }