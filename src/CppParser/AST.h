/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#pragma once

#include "Helpers.h"
#include "Sources.h"
#include <algorithm>

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
    DependentTemplateSpecialization,
    TemplateParameter,
    TemplateParameterSubstitution,
    InjectedClassName,
    DependentName,
    PackExpansion,
    Builtin,
    UnaryTransform,
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
    ComputedNoexcept,
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
    QualifiedType desugared;
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
    VTableComponentKind kind;
    unsigned offset;
    Declaration* declaration;
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
    VTableLayout layout;
};

class CS_API LayoutField
{
public:
    LayoutField();
    LayoutField(const LayoutField& other);
    ~LayoutField();
    unsigned offset;
    STRING(Name)
    QualifiedType qualifiedType;
    void* fieldPtr;
};

class Class;

class CS_API LayoutBase
{
public:
    LayoutBase();
    LayoutBase(const LayoutBase& other);
    ~LayoutBase();
    unsigned offset;
    Class* _class;
};

struct CS_API ClassLayout
{
    ClassLayout();
    CppAbi ABI;
    VECTOR(VFTableInfo, VFTables)
    VTableLayout layout;
    bool hasOwnVFPtr;
    long VBPtrOffset;
    int alignment;
    int size;
    int dataSize;
    VECTOR(LayoutField, Fields)
    VECTOR(LayoutBase, Bases)
};

#pragma endregion

#pragma region Declarations

enum class DeclarationKind
{
    DeclarationContext,
    Typedef,
    TypeAlias,
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
    TypeAliasTemplate,
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
    TemplateTemplateParm,
    TemplateTypeParm,
    NonTypeTemplateParm,
    VarTemplate,
    VarTemplateSpecialization,
    VarTemplatePartialSpecialization,
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

    DeclarationKind kind;
    AccessSpecifier access;
    DeclarationContext* _namespace;
    SourceLocation location;
    int lineNumberStart;
    int lineNumberEnd;
    STRING(Name)
    STRING(USR)
    STRING(DebugText)
    bool isIncomplete;
    bool isDependent;
    bool isImplicit;
    Declaration* completeDeclaration;
    unsigned definitionOrder;
    VECTOR(PreprocessedEntity*, PreprocessedEntities)
    void* originalPtr;
    RawComment* comment;
};

class Class;
class Enumeration;
class Function;
class TypedefDecl;
class TypeAlias;
class Namespace;
class Template;
class TypeAliasTemplate;
class ClassTemplate;
class FunctionTemplate;
class Variable;
class Friend;

class CS_API DeclarationContext : public Declaration
{
public:
    DeclarationContext(DeclarationKind kind);

    CS_IGNORE Declaration* FindAnonymous(const std::string& USR);

    CS_IGNORE Namespace* FindNamespace(const std::string& Name);
    CS_IGNORE Namespace* FindNamespace(const std::vector<std::string>&);
    CS_IGNORE Namespace* FindCreateNamespace(const std::string& Name);

    CS_IGNORE Class* CreateClass(std::string Name, bool IsComplete);
    CS_IGNORE Class* FindClass(const std::string& Name, bool IsComplete);
    CS_IGNORE Class* FindClass(const std::string& Name, bool IsComplete,
        bool Create);

    CS_IGNORE template<typename T> T* FindTemplate(const std::string& USR);

    CS_IGNORE Enumeration* FindEnum(const void* OriginalPtr);
    CS_IGNORE Enumeration* FindEnum(const std::string& Name, bool Create = false);
    CS_IGNORE Enumeration* FindEnumWithItem(const std::string& Name);

    CS_IGNORE Function* FindFunction(const std::string& USR);

    CS_IGNORE TypedefDecl* FindTypedef(const std::string& Name, bool Create = false);

    CS_IGNORE TypeAlias* FindTypeAlias(const std::string& Name, bool Create = false);

    CS_IGNORE Variable* FindVariable(const std::string& USR);

    CS_IGNORE Friend* FindFriend(const std::string& USR);

    VECTOR(Namespace*, Namespaces)
    VECTOR(Enumeration*, Enums)
    VECTOR(Function*, Functions)
    VECTOR(Class*, Classes)
    VECTOR(Template*, Templates)
    VECTOR(TypedefDecl*, Typedefs)
    VECTOR(TypeAlias*, TypeAliases)
    VECTOR(Variable*, Variables)
    VECTOR(Friend*, Friends)

    std::map<std::string, Declaration*> anonymous;

    bool isAnonymous;
};

class CS_API TypedefNameDecl : public Declaration
{
public:
    TypedefNameDecl(DeclarationKind kind);
    ~TypedefNameDecl();
    QualifiedType qualifiedType;
};

class CS_API TypedefDecl : public TypedefNameDecl
{
public:
    DECLARE_DECL_KIND(TypedefDecl, Typedef)
    ~TypedefDecl();
};

class CS_API TypeAlias : public TypedefNameDecl
{
public:
    TypeAlias();
    ~TypeAlias();
    TypeAliasTemplate* describedAliasTemplate;
};

class CS_API Friend : public Declaration
{
public:
    DECLARE_DECL_KIND(Friend, Friend)
    ~Friend();
    Declaration* declaration;
};

enum class StatementClass
{
    Any,
    BinaryOperator,
    CallExprClass,
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
    StatementClass _class;
    Declaration* decl;
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
    ~BinaryOperator();
    Expression* LHS;
    Expression* RHS;
    STRING(OpcodeStr)
};

class CS_API CallExpr : public Expression
{
public:
    CallExpr(const std::string& str, Declaration* decl);
    ~CallExpr();
    VECTOR(Expression*, Arguments)
};

class CS_API CXXConstructExpr : public Expression
{
public:
    CXXConstructExpr(const std::string& str, Declaration* decl = 0);
    ~CXXConstructExpr();
    VECTOR(Expression*, Arguments)
};

class CS_API Parameter : public Declaration
{
public:
    Parameter();
    ~Parameter();

    QualifiedType qualifiedType;
    bool isIndirect;
    bool hasDefaultValue;
    unsigned int index;
    Expression* defaultArgument;
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
    Conditional,
    Coawait
};

class FunctionTemplateSpecialization;

enum class FriendKind
{
    None,
    Declared,
    Undeclared
};

class CS_API Function : public Declaration
{
public:
    Function();
    ~Function();

    QualifiedType returnType;
    bool isReturnIndirect;
	bool hasThisReturn;

    bool isConstExpr;
    bool isVariadic;
    bool isInline;
    bool isPure;
    bool isDeleted;
    FriendKind friendKind;
    CXXOperatorKind operatorKind;
    STRING(Mangled)
    STRING(Signature)
    CallingConvention callingConvention;
    VECTOR(Parameter*, Parameters)
    FunctionTemplateSpecialization* specializationInfo;
    Function* instantiatedFrom;
    QualifiedType qualifiedType;
};

class AccessSpecifierDecl;

enum class RefQualifierKind
{
    None,
    LValue,
    RValue
};

class CS_API Method : public Function
{
public:
    Method();
    ~Method();

    bool isVirtual;
    bool isStatic;
    bool isConst;
    bool isExplicit;
    bool isOverride;

    CXXMethodKind methodKind;

    bool isDefaultConstructor;
    bool isCopyConstructor;
    bool isMoveConstructor;

    QualifiedType conversionType;
    RefQualifierKind refQualifier;
};

class CS_API Enumeration : public DeclarationContext
{
public:
    DECLARE_DECL_KIND(Enumeration, Enumeration)
    ~Enumeration();

    class CS_API Item : public Declaration
    {
    public:
        DECLARE_DECL_KIND(Item, EnumerationItem)
        Item(const Item&);
        ~Item();

        STRING(Expression)
        uint64_t value;
    };

    enum class CS_FLAGS EnumModifiers
    {
        Anonymous = 1 << 0,
        Scoped = 1 << 1,
        Flags  = 1 << 2,
    };

    EnumModifiers modifiers;
    Type* type;
    BuiltinType* builtinType;
    VECTOR(Item*, Items)

    Item* FindItemByName(const std::string& Name);
};

class CS_API Variable : public Declaration
{
public:
    DECLARE_DECL_KIND(Variable, Variable)
    ~Variable();
    STRING(Mangled)
    QualifiedType qualifiedType;
};

class PreprocessedEntity;

struct CS_API BaseClassSpecifier
{
    BaseClassSpecifier();
    AccessSpecifier access;
    bool isVirtual;
    Type* type;
    int offset;
};

class Class;

class CS_API Field : public Declaration
{
public:
    DECLARE_DECL_KIND(Field, Field)
    ~Field();
    QualifiedType qualifiedType;
    Class* _class;
    bool isBitField;
    unsigned bitWidth;
};

class CS_API AccessSpecifierDecl : public Declaration
{
public:
    DECLARE_DECL_KIND(AccessSpecifierDecl, AccessSpecifier)
    ~AccessSpecifierDecl();
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

    bool isPOD;
    bool isAbstract;
    bool isUnion;
    bool isDynamic;
    bool isPolymorphic;
    bool hasNonTrivialDefaultConstructor;
    bool hasNonTrivialCopyConstructor;
    bool hasNonTrivialDestructor;
    bool isExternCContext;

    ClassLayout* layout;
};

class CS_API Template : public Declaration
{
public:
    Template(DeclarationKind kind);
    DECLARE_DECL_KIND(Template, Template)
    Declaration* TemplatedDecl;
    VECTOR(Declaration*, Parameters)
};

template<typename T>
T* DeclarationContext::FindTemplate(const std::string& USR)
{
    auto foundTemplate = std::find_if(Templates.begin(), Templates.end(),
        [&](Template* t) { return t->USR == USR; });

    if (foundTemplate != Templates.end())
        return static_cast<T*>(*foundTemplate);

    return nullptr;
}

class CS_API TypeAliasTemplate : public Template
{
public:
    TypeAliasTemplate();
    ~TypeAliasTemplate();
};

class CS_API TemplateParameter : public Declaration
{
public:
    TemplateParameter(DeclarationKind kind);
    ~TemplateParameter();
    unsigned int depth;
    unsigned int index;
    bool isParameterPack;
};

class CS_API TemplateTemplateParameter : public Template
{
public:
    TemplateTemplateParameter();
    ~TemplateTemplateParameter();

    bool isParameterPack;
    bool isPackExpansion;
    bool isExpandedParameterPack;
};

class CS_API TypeTemplateParameter : public TemplateParameter
{
public:
    TypeTemplateParameter();
    TypeTemplateParameter(const TypeTemplateParameter&);
    ~TypeTemplateParameter();

    QualifiedType defaultArgument;
};

class CS_API NonTypeTemplateParameter : public TemplateParameter
{
public:
    NonTypeTemplateParameter();
    NonTypeTemplateParameter(const NonTypeTemplateParameter&);
    ~NonTypeTemplateParameter();

    Expression* defaultArgument;
    unsigned int position;
    bool isPackExpansion;
    bool isExpandedParameterPack;
};

class ClassTemplateSpecialization;
class ClassTemplatePartialSpecialization;

class CS_API ClassTemplate : public Template
{
public:
    ClassTemplate();
    ~ClassTemplate();
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
    ~ClassTemplateSpecialization();
    ClassTemplate* templatedDecl;
    VECTOR(TemplateArgument, Arguments)
    TemplateSpecializationKind specializationKind;
};

class CS_API ClassTemplatePartialSpecialization : public ClassTemplateSpecialization
{
public:
    ClassTemplatePartialSpecialization();
    ~ClassTemplatePartialSpecialization();
};

class CS_API FunctionTemplate : public Template
{
public:
    FunctionTemplate();
    ~FunctionTemplate();
    VECTOR(FunctionTemplateSpecialization*, Specializations)
    FunctionTemplateSpecialization* FindSpecialization(const std::string& usr);
};

class CS_API FunctionTemplateSpecialization
{
public:
    FunctionTemplateSpecialization();
    ~FunctionTemplateSpecialization();
    FunctionTemplate* _template;
    VECTOR(TemplateArgument, Arguments)
    Function* specializedFunction;
    TemplateSpecializationKind specializationKind;
};

class VarTemplateSpecialization;
class VarTemplatePartialSpecialization;

class CS_API VarTemplate : public Template
{
public:
    VarTemplate();
    ~VarTemplate();
    VECTOR(VarTemplateSpecialization*, Specializations)
    VarTemplateSpecialization* FindSpecialization(const std::string& usr);
    VarTemplatePartialSpecialization* FindPartialSpecialization(const std::string& usr);
};

class CS_API VarTemplateSpecialization : public Variable
{
public:
    VarTemplateSpecialization();
    ~VarTemplateSpecialization();
    VarTemplate* templatedDecl;
    VECTOR(TemplateArgument, Arguments)
    TemplateSpecializationKind specializationKind;
};

class CS_API VarTemplatePartialSpecialization : public VarTemplateSpecialization
{
public:
    VarTemplatePartialSpecialization();
    ~VarTemplatePartialSpecialization();
};

class CS_API Namespace : public DeclarationContext
{
public:
    Namespace();
    ~Namespace();
    bool isInline;
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

class CS_API PreprocessedEntity
{
public:
    PreprocessedEntity();
    MacroLocation macroLocation;
    void* originalPtr;
    DeclarationKind kind;
};

class CS_API MacroDefinition : public PreprocessedEntity
{
public:
    MacroDefinition();
    ~MacroDefinition();
    STRING(Name)
    STRING(Expression)
    int lineNumberStart;
    int lineNumberEnd;
};

class CS_API MacroExpansion : public PreprocessedEntity
{
public:
    MacroExpansion();
    ~MacroExpansion();
    STRING(Name)
    STRING(Text)
    MacroDefinition* definition;
};

class CS_API TranslationUnit : public Namespace
{
public:
    TranslationUnit();
    ~TranslationUnit();
    STRING(FileName)
    bool isSystemHeader;
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
    ~NativeLibrary();
    STRING(FileName)
    ArchType archType;
    VECTOR_STRING(Symbols)
    VECTOR_STRING(Dependencies)
};

class CS_API ASTContext
{
public:
    ASTContext();
    ~ASTContext();
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
    CommentKind kind;
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
    ~FullComment();
    VECTOR(BlockContentComment*, Blocks)
};

class CS_API InlineContentComment : public Comment
{
public:
    InlineContentComment();
    InlineContentComment(CommentKind Kind);
    bool hasTrailingNewline;
};

class CS_API ParagraphComment : public BlockContentComment
{
public:
    ParagraphComment();
    ~ParagraphComment();
    bool isWhitespace;
    VECTOR(InlineContentComment*, Content)
};

class CS_API BlockCommandComment : public BlockContentComment
{
public:
    class CS_API Argument
    {
    public:
        Argument();
        Argument(const Argument&);
        STRING(Text)
    };
    BlockCommandComment();
    BlockCommandComment(CommentKind Kind);
    ~BlockCommandComment();
    unsigned commandId;
    ParagraphComment* paragraphComment;
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
    PassDirection direction;
    unsigned paramIndex;
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
    ~VerbatimBlockComment();
    VECTOR(VerbatimBlockLineComment*, Lines)
};

class CS_API VerbatimLineComment : public BlockCommandComment
{
public:
    VerbatimLineComment();
    STRING(Text)
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
        Argument(const Argument&);
        STRING(Text)
    };
    InlineCommandComment();
    unsigned commandId;
    RenderKind commentRenderKind;
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
        Attribute(const Attribute&);
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
    ~RawComment();
    RawCommentKind kind;
    STRING(Text)
    STRING(BriefText)
    FullComment* fullCommentBlock;
};

#pragma region Commands

#pragma endregion

#pragma endregion

} } }