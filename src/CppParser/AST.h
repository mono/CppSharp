/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#pragma once

#include "Helpers.h"
#include "Sources.h"

namespace CppSharp { namespace CppParser { namespace AST {

#pragma region Types

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
    TemplateParameter,
    TemplateParameterSubstitution,
    InjectedClassName,
    DependentName,
    PackExpansion,
    Builtin
};

#define DECLARE_TYPE_KIND(kind) \
    kind##Type();

class CS_API Type
{
public:
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

class Declaration;

class CS_API TagType : public Type
{
public:
    DECLARE_TYPE_KIND(Tag)
    CppSharp::CppParser::AST::Declaration* Declaration;
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
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
    ArraySize SizeType;
    long Size;
    long ElementSize;
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

class CS_API FunctionType : public Type
{
public:
    DECLARE_TYPE_KIND(Function)
    QualifiedType ReturnType;
    CppSharp::CppParser::AST::CallingConvention CallingConvention;
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
    QualifiedType QualifiedPointee;
    TypeModifier Modifier;
};

class CS_API MemberPointerType : public Type
{
public:
    DECLARE_TYPE_KIND(MemberPointer)
    QualifiedType Pointee;
};

class TypedefDecl;

class CS_API TypedefType : public Type
{
public:
    TypedefType();
    TypedefDecl* Declaration;
};

class CS_API AttributedType : public Type
{
public:
    DECLARE_TYPE_KIND(Attributed)
    QualifiedType Modified;
    QualifiedType Equivalent;
};

class CS_API DecayedType : public Type
{
public:
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

class Template;

class CS_API TemplateSpecializationType : public Type
{
public:
    TemplateSpecializationType();
    TemplateSpecializationType(const TemplateSpecializationType&);

    VECTOR(TemplateArgument, Arguments)
    CppSharp::CppParser::AST::Template* Template;
    Type* Desugared;
};

class CS_API TemplateParameter
{
public:
    TemplateParameter();
    TemplateParameter(const TemplateParameter&);

    bool operator==(const TemplateParameter& param) const
    {
        return Name == param.Name;
    }

    STRING(Name)
    bool IsTypeParameter;
};

class CS_API TemplateParameterType : public Type
{
public:
    DECLARE_TYPE_KIND(TemplateParameter)
    TemplateParameter Parameter;
    unsigned int Depth;
    unsigned int Index;
    bool IsParameterPack;
};

class CS_API TemplateParameterSubstitutionType : public Type
{
public:
    DECLARE_TYPE_KIND(TemplateParameterSubstitution)
    QualifiedType Replacement;
};

class Class;

class CS_API InjectedClassNameType : public Type
{
public:
    InjectedClassNameType();
    TemplateSpecializationType* TemplateSpecialization;
    CppSharp::CppParser::AST::Class* Class;
};

class CS_API DependentNameType : public Type
{
public:
    DECLARE_TYPE_KIND(DependentName)
};

class CS_API PackExpansionType : public Type
{
public:
    DECLARE_TYPE_KIND(PackExpansion)
};

enum class PrimitiveType
{
    Null,
    Void,
    Bool,
    WideChar,
    Char,
    UChar,
    Short,
    UShort,
    Int,
    UInt,
    Long,
    ULong,
    LongLong,
    ULongLong,
    Float,
    Double,
    IntPtr
};

class CS_API BuiltinType : public Type
{
public:
    DECLARE_TYPE_KIND(Builtin)
    PrimitiveType Type;
};

#pragma endregion

#pragma region ABI

enum class CppAbi
{
    Itanium,
    Microsoft,
    ARM,
    iOS,
    iOS64
};

enum class VTableComponentKind
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
    ~VTableLayout();
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

enum class DeclarationKind
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
    TranslationUnit,
    Friend,
};

#define DECLARE_DECL_KIND(klass, kind) \
    klass();

enum class AccessSpecifier
{
    Private,
    Protected,
    Public
};

class DeclarationContext;
class RawComment;
class PreprocessedEntity;

class CS_API Declaration
{
public:
    Declaration(DeclarationKind kind);
    Declaration(const Declaration&);
    ~Declaration();

    DeclarationKind Kind;
    AccessSpecifier Access;
    DeclarationContext* _Namespace;
    SourceLocation Location;
    int LineNumberStart;
    int LineNumberEnd;
    STRING(Name)
    RawComment* Comment;
    STRING(DebugText)
    bool IsIncomplete;
    bool IsDependent;
    Declaration* CompleteDeclaration;
    unsigned DefinitionOrder;
    VECTOR(PreprocessedEntity*, PreprocessedEntities)
    void* OriginalPtr;
    std::string USR;
};

class Class;
class Enumeration;
class Function;
class TypedefDecl;
class Namespace;
class Template;
class ClassTemplate;
class FunctionTemplate;
class Variable;
class Friend;

class CS_API DeclarationContext : public Declaration
{
public:
    DeclarationContext(DeclarationKind kind);

    CS_IGNORE Declaration* FindAnonymous(const std::string& USR);

    CS_IGNORE CppSharp::CppParser::AST::Namespace* FindNamespace(const std::string& Name);
    CS_IGNORE CppSharp::CppParser::AST::Namespace* FindNamespace(const std::vector<std::string>&);
    CS_IGNORE CppSharp::CppParser::AST::Namespace* FindCreateNamespace(const std::string& Name);

    CS_IGNORE Class* CreateClass(std::string Name, bool IsComplete);
    CS_IGNORE Class* FindClass(const std::string& Name);
    CS_IGNORE Class* FindClass(const std::string& Name, bool IsComplete,
        bool Create = false);

    CS_IGNORE ClassTemplate* FindClassTemplate(const std::string& USR);
    CS_IGNORE FunctionTemplate* FindFunctionTemplate(const std::string& USR);

    CS_IGNORE Enumeration* FindEnum(void* OriginalPtr);
    CS_IGNORE Enumeration* FindEnum(const std::string& Name, bool Create = false);
    CS_IGNORE Enumeration* FindEnumWithItem(const std::string& Name);

    CS_IGNORE Function* FindFunction(const std::string& USR);

    CS_IGNORE TypedefDecl* FindTypedef(const std::string& Name, bool Create = false);

    CS_IGNORE Variable* FindVariable(const std::string& USR);

    CS_IGNORE Friend* FindFriend(const std::string& USR);

    VECTOR(Namespace*, Namespaces)
    VECTOR(Enumeration*, Enums)
    VECTOR(Function*, Functions)
    VECTOR(Class*, Classes)
    VECTOR(Template*, Templates)
    VECTOR(TypedefDecl*, Typedefs)
    VECTOR(Variable*, Variables)
    VECTOR(Friend*, Friends)

    std::map<std::string, Declaration*> Anonymous;

    bool IsAnonymous;
};

class CS_API TypedefDecl : public Declaration
{
public:
    DECLARE_DECL_KIND(TypedefDecl, Typedef)
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
};

class CS_API Friend : public Declaration
{
public:
    DECLARE_DECL_KIND(Friend, Friend)
    CppSharp::CppParser::AST::Declaration* Declaration;
};

enum class StatementClass
{
    Any,
    BinaryOperator,
    DeclRefExprClass,
    CXXConstructExprClass,
    CXXOperatorCallExpr,
    ImplicitCastExpr,
    ExplicitCastExpr,
};

class CS_API Statement
{
public:
    Statement(const std::string& str, StatementClass Class = StatementClass::Any, Declaration* decl = 0);
    StatementClass Class;
    Declaration* Decl;
    STRING(String)
};

class CS_API Expression : public Statement
{
public:
    Expression(const std::string& str, StatementClass Class = StatementClass::Any, Declaration* decl = 0);
};

class CS_API BinaryOperator : public Expression
{
public:
    BinaryOperator(const std::string& str, Expression* lhs, Expression* rhs, const std::string& opcodeStr);
    Expression* LHS;
    Expression* RHS;
    STRING(OpcodeStr)
};

class CS_API CXXConstructExpr : public Expression
{
public:
    CXXConstructExpr(const std::string& str, Declaration* decl = 0);
    VECTOR(Expression*, Arguments)
};

class CS_API Parameter : public Declaration
{
public:
    Parameter();
    ~Parameter();

    CppSharp::CppParser::AST::QualifiedType QualifiedType;
    bool IsIndirect;
    bool HasDefaultValue;
    unsigned int Index;
    Expression* DefaultArgument;
};

enum class CXXMethodKind
{
    Normal,
    Constructor,
    Destructor,
    Conversion,
    Operator,
    UsingDirective
};

enum class CXXOperatorKind
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

class FunctionTemplateSpecialization;

class CS_API Function : public Declaration
{
public:
    Function();

    QualifiedType ReturnType;
    bool IsReturnIndirect;
	bool HasThisReturn;

    bool IsVariadic;
    bool IsInline;
    bool IsPure;
    bool IsDeleted;
    CXXOperatorKind OperatorKind;
    STRING(Mangled)
    STRING(Signature)
    CppSharp::CppParser::AST::CallingConvention CallingConvention;
    VECTOR(Parameter*, Parameters)
    FunctionTemplateSpecialization* SpecializationInfo;
};

class AccessSpecifierDecl;

class CS_API Method : public Function
{
public:
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

class CS_API Enumeration : public DeclarationContext
{
public:
    DECLARE_DECL_KIND(Enumeration, Enumeration)

    class CS_API Item : public Declaration
    {
    public:
        DECLARE_DECL_KIND(Item, EnumerationItem)
        Item(const Item&);

        STRING(Expression)
        uint64_t Value;
    };

    enum class CS_FLAGS EnumModifiers
    {
        Anonymous = 1 << 0,
        Scoped = 1 << 1,
        Flags  = 1 << 2,
    };

    EnumModifiers Modifiers;
    CppSharp::CppParser::AST::Type* Type;
    CppSharp::CppParser::AST::BuiltinType* BuiltinType;
    VECTOR(Item, Items)

    Item* FindItemByName(const std::string& Name);
};

class CS_API Variable : public Declaration
{
public:
    DECLARE_DECL_KIND(Variable, Variable)

    STRING(Mangled)
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
};

class PreprocessedEntity;

struct CS_API BaseClassSpecifier
{
    BaseClassSpecifier();
    AccessSpecifier Access;
    bool IsVirtual;
    CppSharp::CppParser::AST::Type* Type;
    int Offset;
};

class Class;

class CS_API Field : public Declaration
{
public:
    DECLARE_DECL_KIND(Field, Field)
    CppSharp::CppParser::AST::QualifiedType QualifiedType;
    unsigned Offset;
    CppSharp::CppParser::AST::Class* Class;
    bool IsBitField;
    unsigned BitWidth;
};

class CS_API AccessSpecifierDecl : public Declaration
{
public:
    DECLARE_DECL_KIND(AccessSpecifierDecl, AccessSpecifier)
};

class CS_API Class : public DeclarationContext
{
public:
    Class();
    ~Class();

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

class CS_API Template : public Declaration
{
public:
    Template(DeclarationKind kind);
    DECLARE_DECL_KIND(Template, Template)
    Declaration* TemplatedDecl;
    VECTOR(TemplateParameter, Parameters)
};

class ClassTemplateSpecialization;
class ClassTemplatePartialSpecialization;

class CS_API ClassTemplate : public Template
{
public:
    ClassTemplate();
    VECTOR(ClassTemplateSpecialization*, Specializations)
    ClassTemplateSpecialization* FindSpecialization(const std::string& usr);
    ClassTemplatePartialSpecialization* FindPartialSpecialization(const std::string& usr);
};

enum class TemplateSpecializationKind
{
    Undeclared,
    ImplicitInstantiation,
    ExplicitSpecialization,
    ExplicitInstantiationDeclaration,
    ExplicitInstantiationDefinition
};

class CS_API ClassTemplateSpecialization : public Class
{
public:
    ClassTemplateSpecialization();
    ClassTemplate* TemplatedDecl;
    VECTOR(TemplateArgument, Arguments)
    TemplateSpecializationKind SpecializationKind;
};

class CS_API ClassTemplatePartialSpecialization : public ClassTemplateSpecialization
{
public:
    ClassTemplatePartialSpecialization();
};

class CS_API FunctionTemplate : public Template
{
public:
    FunctionTemplate();
    VECTOR(FunctionTemplateSpecialization*, Specializations)
    FunctionTemplateSpecialization* FindSpecialization(const std::string& usr);
};

class CS_API FunctionTemplateSpecialization
{
public:
    FunctionTemplateSpecialization();
    FunctionTemplate* Template;
    VECTOR(TemplateArgument, Arguments)
    Function* SpecializedFunction;
    TemplateSpecializationKind SpecializationKind;
};

class CS_API Namespace : public DeclarationContext
{
public:
    Namespace();
    bool IsInline;
};

enum class MacroLocation
{
    Unknown,
    ClassHead,
    ClassBody,
    FunctionHead,
    FunctionParameters,
    FunctionBody,
};

class CS_API PreprocessedEntity : public Declaration
{
public:
    PreprocessedEntity();
    MacroLocation MacroLocation;
};

class CS_API MacroDefinition : public PreprocessedEntity
{
public:
    MacroDefinition();
    STRING(Expression)
};

class CS_API MacroExpansion : public PreprocessedEntity
{
public:
    MacroExpansion();
    STRING(Text)
    MacroDefinition* Definition;
};

class CS_API TranslationUnit : public Namespace
{
public:
    TranslationUnit();
    STRING(FileName)
    bool IsSystemHeader;
    VECTOR(MacroDefinition*, Macros)
};

enum class ArchType
{
    UnknownArch,
    x86,
    x86_64
};

class CS_API NativeLibrary
{
public:
    NativeLibrary();
    STRING(FileName)
    ArchType ArchType;
    VECTOR_STRING(Symbols)
    VECTOR_STRING(Dependencies)
};

class CS_API ASTContext
{
public:
    ASTContext();
    TranslationUnit* FindOrCreateModule(std::string File);
    VECTOR(TranslationUnit*, TranslationUnits)
};

#pragma endregion

#pragma region Comments

enum struct CommentKind
{
    FullComment,
    BlockContentComment,
    BlockCommandComment,
    ParamCommandComment,
    TParamCommandComment,
    VerbatimBlockComment,
    VerbatimLineComment,
    ParagraphComment,
    HTMLTagComment,
    HTMLStartTagComment,
    HTMLEndTagComment,
    TextComment,
    InlineContentComment,
    InlineCommandComment,
    VerbatimBlockLineComment
};

class CS_API CS_ABSTRACT Comment
{
public:
    Comment(CommentKind kind);
    CommentKind Kind;
};

class CS_API BlockContentComment : public Comment
{
public:
    BlockContentComment();
    BlockContentComment(CommentKind Kind);
};

class CS_API FullComment : public Comment
{
public:
    FullComment();
    VECTOR(BlockContentComment*, Blocks)
};

class CS_API BlockCommandComment : public BlockContentComment
{
public:
    class CS_API Argument
    {
    public:
        Argument();
        STRING(Text)
    };
    BlockCommandComment();
    BlockCommandComment(CommentKind Kind);
    unsigned CommandId;
    VECTOR(Argument, Arguments)
};

class CS_API ParamCommandComment : public BlockCommandComment
{
public:
    enum PassDirection
    {
        In,
        Out,
        InOut
    };
    ParamCommandComment();
    PassDirection Direction;
    unsigned ParamIndex;
};

class CS_API TParamCommandComment : public BlockCommandComment
{
public:
    TParamCommandComment();
    VECTOR(unsigned, Position)
};

class CS_API VerbatimBlockLineComment : public Comment
{
public:
    VerbatimBlockLineComment();
    STRING(Text)
};

class CS_API VerbatimBlockComment : public BlockCommandComment
{
public:
    VerbatimBlockComment();
    VECTOR(VerbatimBlockLineComment*, Lines)
};

class CS_API VerbatimLineComment : public BlockCommandComment
{
public:
    VerbatimLineComment();
    STRING(Text)
};

class CS_API InlineContentComment : public Comment
{
public:
    InlineContentComment();
    InlineContentComment(CommentKind Kind);
};

class CS_API ParagraphComment : public BlockContentComment
{
public:
    ParagraphComment();
    bool IsWhitespace;
    VECTOR(InlineContentComment*, Content)
};

class CS_API InlineCommandComment : public InlineContentComment
{
public:
    enum RenderKind
    {
        RenderNormal,
        RenderBold,
        RenderMonospaced,
        RenderEmphasized
    };
    class CS_API Argument
    {
    public:
        Argument();
        STRING(Text)
    };
    InlineCommandComment();
    RenderKind CommentRenderKind;
    VECTOR(Argument, Arguments)
};

class CS_API HTMLTagComment : public InlineContentComment
{
public:
    HTMLTagComment();
    HTMLTagComment(CommentKind Kind);
};

class CS_API HTMLStartTagComment : public HTMLTagComment
{
public:
    class CS_API Attribute
    {
    public:
        Attribute();
        STRING(Name)
        STRING(Value)
    };
    HTMLStartTagComment();
    STRING(TagName)
    VECTOR(Attribute, Attributes)
};

class CS_API HTMLEndTagComment : public HTMLTagComment
{
public:
    HTMLEndTagComment();
    STRING(TagName)
};

class CS_API TextComment : public InlineContentComment
{
public:
    TextComment();
    STRING(Text)
};

enum class RawCommentKind
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

class CS_API RawComment
{
public:
    RawComment();
    RawCommentKind Kind;
    STRING(Text)
    STRING(BriefText)
    FullComment* FullCommentBlock;
};

#pragma region Commands

#pragma endregion

#pragma endregion

} } }