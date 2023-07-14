/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#include "AST.h"
#include <string>
#include <vector>
#include <llvm/ADT/SmallString.h>
#include <llvm/Support/Path.h>

// copy from widenPath ('llvm/lib/Support/Windows/Path.inc')
static std::string normalizePath(const std::string & File) {
    llvm::SmallString<2 * 128> Result;

    for (llvm::sys::path::const_iterator I = llvm::sys::path::begin(File),
        E = llvm::sys::path::end(File);
        I != E; ++I) {
        if (I->size() == 1 && *I == ".")
            continue;
        if (I->size() == 2 && *I == "..")
            llvm::sys::path::remove_filename(Result);
        else
            llvm::sys::path::append(Result, *I);
    }

#ifdef _WIN32
    // Clean up the file path.
    std::replace(Result.begin(), Result.end(), '/', '\\');
#endif

    return Result.c_str();
}

template<typename T>
static std::vector<T> split(const T & str, const T & delimiters) {
    std::vector<T> v;
    if (str.length() == 0) {
        v.push_back(str);
        return v;
    }
    typename T::size_type start = 0;
    auto pos = str.find_first_of(delimiters, start);
    while(pos != T::npos) {
        if(pos != start) // ignore empty tokens
            v.emplace_back(str, start, pos - start);
        start = pos + 1;
        pos = str.find_first_of(delimiters, start);
    }
    if(start < str.length()) // ignore trailing delimiter
       // add what's left of the string
        v.emplace_back(str, start, str.length() - start);
    return v;
}

namespace CppSharp { namespace CppParser { namespace AST {

static void deleteExpression(ExpressionObsolete* expression)
{
    if (expression)
    {
        // HACK: see https://github.com/mono/CppSharp/issues/598
        switch (expression->_class)
        {
        case StatementClassObsolete::BinaryOperator:
            delete static_cast<BinaryOperatorObsolete*>(expression);
            break;
        case StatementClassObsolete::CallExprClass:
            delete static_cast<CallExprObsolete*>(expression);
            break;
        case StatementClassObsolete::CXXConstructExprClass:
            delete static_cast<CXXConstructExprObsolete*>(expression);
            break;
        default:
            delete expression;
            break;
        }
    }
}

Type::Type(TypeKind kind) : kind(kind) {}
Type::Type(const Type& rhs) : kind(rhs.kind), isDependent(rhs.isDependent) {}

QualifiedType::QualifiedType() : type(0) {}

TagType::TagType() : Type(TypeKind::Tag) {}

ArrayType::ArrayType() : Type(TypeKind::Array), size(0), elementSize(0) {}

FunctionType::FunctionType()
    : Type(TypeKind::Function)
    , callingConvention(CallingConvention::Default)
    , exceptionSpecType(ExceptionSpecType::None)
{
}

FunctionType::~FunctionType() {}

DEF_VECTOR(FunctionType, Parameter*, Parameters)

PointerType::PointerType() : Type(TypeKind::Pointer) {}

MemberPointerType::MemberPointerType() : Type(TypeKind::MemberPointer) {}

TypedefType::TypedefType() : Type(TypeKind::Typedef), declaration(0) {}

AttributedType::AttributedType() : Type(TypeKind::Attributed) {}

DecayedType::DecayedType() : Type(TypeKind::Decayed) {}

// Template
TemplateParameter::TemplateParameter(DeclarationKind kind)
    : Declaration(kind)
    , depth(0)
    , index(0)
    , isParameterPack(false)
{
}

TemplateParameter::~TemplateParameter()
{
}

TemplateTemplateParameter::TemplateTemplateParameter()
    : Template(DeclarationKind::TemplateTemplateParm)
    , isParameterPack(false)
    , isPackExpansion(false)
    , isExpandedParameterPack(false)
{
}

TemplateTemplateParameter::~TemplateTemplateParameter()
{
}

// TemplateParameter
TypeTemplateParameter::TypeTemplateParameter()
    : TemplateParameter(DeclarationKind::TemplateTypeParm)
{
}

TypeTemplateParameter::TypeTemplateParameter(const TypeTemplateParameter& rhs)
    : TemplateParameter(rhs.kind)
    , defaultArgument(rhs.defaultArgument)
{
}

TypeTemplateParameter::~TypeTemplateParameter() {}

NonTypeTemplateParameter::NonTypeTemplateParameter()
    : TemplateParameter(DeclarationKind::NonTypeTemplateParm)
    , defaultArgument(0)
    , position(0)
    , isPackExpansion(false)
    , isExpandedParameterPack(false)
{
}

NonTypeTemplateParameter::NonTypeTemplateParameter(const NonTypeTemplateParameter& rhs)
    : TemplateParameter(rhs.kind)
    , defaultArgument(rhs.defaultArgument)
    , position(rhs.position)
    , isPackExpansion(rhs.isPackExpansion)
    , isExpandedParameterPack(rhs.isExpandedParameterPack)
    , type(rhs.type)
{
}

NonTypeTemplateParameter::~NonTypeTemplateParameter()
{
    deleteExpression(defaultArgument);
}

TemplateArgument::TemplateArgument() : declaration(0), integral(0) {}

TemplateSpecializationType::TemplateSpecializationType()
    : Type(TypeKind::TemplateSpecialization), _template(0) {}

TemplateSpecializationType::TemplateSpecializationType(
    const TemplateSpecializationType& rhs) : Type(rhs),
    Arguments(rhs.Arguments), _template(rhs._template), desugared(rhs.desugared) {}

TemplateSpecializationType::~TemplateSpecializationType() {}

DEF_VECTOR(TemplateSpecializationType, TemplateArgument, Arguments)

DependentTemplateSpecializationType::DependentTemplateSpecializationType()
    : Type(TypeKind::DependentTemplateSpecialization) {}

DependentTemplateSpecializationType::DependentTemplateSpecializationType(
    const DependentTemplateSpecializationType& rhs) : Type(rhs),
    Arguments(rhs.Arguments), desugared(rhs.desugared) {}

DependentTemplateSpecializationType::~DependentTemplateSpecializationType() {}

DEF_VECTOR(DependentTemplateSpecializationType, TemplateArgument, Arguments)

TemplateParameterType::TemplateParameterType() : Type(TypeKind::TemplateParameter), parameter(0) {}

TemplateParameterType::~TemplateParameterType() {}

TemplateParameterSubstitutionType::TemplateParameterSubstitutionType()
    : Type(TypeKind::TemplateParameterSubstitution), replacedParameter(0) {}

InjectedClassNameType::InjectedClassNameType()
    : Type(TypeKind::InjectedClassName)
    , _class(0)
{
}

DependentNameType::DependentNameType() : Type(TypeKind::DependentName) {}

DependentNameType::~DependentNameType() {}

PackExpansionType::PackExpansionType() : Type(TypeKind::PackExpansion) {}

UnaryTransformType::UnaryTransformType() : Type(TypeKind::UnaryTransform) {}

UnresolvedUsingType::UnresolvedUsingType() : Type(TypeKind::UnresolvedUsing) {}

VectorType::VectorType() : Type(TypeKind::Vector), numElements(0) {}

BuiltinType::BuiltinType() : CppSharp::CppParser::AST::Type(TypeKind::Builtin) {}

VTableComponent::VTableComponent() : offset(0), declaration(0) {}

// VTableLayout
VTableLayout::VTableLayout() {}
VTableLayout::VTableLayout(const VTableLayout& rhs) : Components(rhs.Components) {}
VTableLayout::~VTableLayout() {}

DEF_VECTOR(VTableLayout, VTableComponent, Components)

VFTableInfo::VFTableInfo() : VBTableIndex(0), VFPtrOffset(0), VFPtrFullOffset(0) {}
VFTableInfo::VFTableInfo(const VFTableInfo& rhs) : VBTableIndex(rhs.VBTableIndex),
    VFPtrOffset(rhs.VFPtrOffset), VFPtrFullOffset(rhs.VFPtrFullOffset),
    layout(rhs.layout) {}

LayoutField::LayoutField() : offset(0), fieldPtr(0) {}

LayoutField::LayoutField(const LayoutField & other)
    : offset(other.offset)
    , name(other.name)
    , qualifiedType(other.qualifiedType)
    , fieldPtr(other.fieldPtr)
{
}

LayoutField::~LayoutField() {}

LayoutBase::LayoutBase() : offset(0), _class(0) {}

LayoutBase::LayoutBase(const LayoutBase& other) : offset(other.offset), _class(other._class) {}

LayoutBase::~LayoutBase() {}

ClassLayout::ClassLayout() : ABI(CppAbi::Itanium), argABI(RecordArgABI::Default),
    hasOwnVFPtr(false), VBPtrOffset(0), alignment(0), size(0), dataSize(0) {}

DEF_VECTOR(ClassLayout, VFTableInfo, VFTables)

DEF_VECTOR(ClassLayout, LayoutField, Fields)

DEF_VECTOR(ClassLayout, LayoutBase, Bases)

Declaration::Declaration(DeclarationKind kind)
    : kind(kind)
    , access(AccessSpecifier::Public)
    , _namespace(0)
    , location(0)
    , lineNumberStart(0)
    , lineNumberEnd(0)
    , comment(0)
    , isIncomplete(false)
    , isDependent(false)
    , isImplicit(false)
    , isInvalid(false)
    , isDeprecated(false)
    , completeDeclaration(0)
    , definitionOrder(0)
    , originalPtr(0)
    , alignAs(0)
    , maxFieldAlignment(0)    
{
}

Declaration::Declaration(const Declaration& rhs)
    : kind(rhs.kind)
    , access(rhs.access)
    , _namespace(rhs._namespace)
    , location(rhs.location.ID)
    , lineNumberStart(rhs.lineNumberStart)
    , lineNumberEnd(rhs.lineNumberEnd)
    , name(rhs.name)
    , comment(rhs.comment)
    , debugText(rhs.debugText)
    , isIncomplete(rhs.isIncomplete)
    , isDependent(rhs.isDependent)
    , isImplicit(rhs.isImplicit)
    , isInvalid(rhs.isInvalid)
    , isDeprecated(rhs.isDeprecated)
    , completeDeclaration(rhs.completeDeclaration)
    , definitionOrder(rhs.definitionOrder)
    , PreprocessedEntities(rhs.PreprocessedEntities)
    , originalPtr(rhs.originalPtr)
{
}

Declaration::~Declaration()
{
}

DEF_VECTOR(Declaration, PreprocessedEntity*, PreprocessedEntities)
DEF_VECTOR(Declaration, Declaration*, Redeclarations)

DeclarationContext::DeclarationContext(DeclarationKind kind)
    : Declaration(kind)
    , isAnonymous(false)
{}

DEF_VECTOR(DeclarationContext, Namespace*, Namespaces)
DEF_VECTOR(DeclarationContext, Enumeration*, Enums)
DEF_VECTOR(DeclarationContext, Function*, Functions)
DEF_VECTOR(DeclarationContext, Class*, Classes)
DEF_VECTOR(DeclarationContext, Template*, Templates)
DEF_VECTOR(DeclarationContext, TypedefDecl*, Typedefs)
DEF_VECTOR(DeclarationContext, TypeAlias*, TypeAliases)
DEF_VECTOR(DeclarationContext, Variable*, Variables)
DEF_VECTOR(DeclarationContext, Friend*, Friends)

Declaration* DeclarationContext::FindAnonymous(const std::string& key)
{
    auto it = anonymous.find(key);
    return (it != anonymous.end()) ? it->second : 0;
}

Namespace* DeclarationContext::FindNamespace(const std::string& Name)
{
    auto namespaces = split<std::string>(Name, "::");
    return FindNamespace(namespaces);
}

Namespace*
DeclarationContext::FindNamespace(const std::vector<std::string>& Namespaces)
{
    auto currentNamespace = this;
    for (auto I = Namespaces.begin(), E = Namespaces.end(); I != E; ++I)
    {
        auto& _namespace = *I;

        auto childNamespace = std::find_if(currentNamespace->Namespaces.begin(),
            currentNamespace->Namespaces.end(),
            [&](CppSharp::CppParser::AST::Namespace* ns) {
                return ns->name == _namespace;
        });

        if (childNamespace == currentNamespace->Namespaces.end())
            return nullptr;

        currentNamespace = *childNamespace;
    }

    return (CppSharp::CppParser::AST::Namespace*) currentNamespace;
}

Namespace* DeclarationContext::FindCreateNamespace(const std::string& Name)
{
    auto _namespace = FindNamespace(Name);

    if (!_namespace)
    {
        _namespace = new Namespace();
        _namespace->name = Name;
        _namespace->_namespace = this;

        Namespaces.push_back(_namespace);
    }

    return _namespace;
}

Class* DeclarationContext::FindClass(const void* OriginalPtr,
    const std::string& Name, bool IsComplete)
{
    if (Name.empty()) return nullptr;

    auto entries = split<std::string>(Name, "::");

    if (entries.size() == 1)
    {
        auto _class = std::find_if(Classes.begin(), Classes.end(),
            [OriginalPtr, Name, IsComplete](Class* klass) {
                return (OriginalPtr && klass->originalPtr == OriginalPtr) ||
                    (klass->name == Name && klass->isIncomplete == !IsComplete); });

        return _class != Classes.end() ? *_class : nullptr;
    }

    auto className = entries[entries.size() - 1];

    std::vector<std::string> namespaces;
    std::copy_n(entries.begin(), entries.size() - 1, std::back_inserter(namespaces));

    auto _namespace = FindNamespace(namespaces);
    if (!_namespace)
        return nullptr;

    return _namespace->FindClass(OriginalPtr, className, IsComplete);
}

Class* DeclarationContext::CreateClass(const std::string& Name, bool IsComplete)
{
    auto _class = new Class();
    _class->name = Name;
    _class->_namespace = this;
    _class->isIncomplete = !IsComplete;

    return _class;
}

Class* DeclarationContext::FindClass(const void* OriginalPtr,
    const std::string& Name, bool IsComplete, bool Create)
{
    auto _class = FindClass(OriginalPtr, Name, IsComplete);

    if (!_class)
    {
        if (Create)
        {
            _class = CreateClass(Name, IsComplete);
            Classes.push_back(_class);
        }
        
        return _class;
    }

    return _class;
}

Enumeration* DeclarationContext::FindEnum(const void* OriginalPtr)
{
    auto foundEnum = std::find_if(Enums.begin(), Enums.end(),
        [&](Enumeration* enumeration) { return enumeration->originalPtr == OriginalPtr; });

    if (foundEnum != Enums.end())
        return *foundEnum;

    return nullptr;
}

Enumeration* DeclarationContext::FindEnum(const std::string& Name, bool Create)
{
    auto entries = split<std::string>(Name, "::");

    if (entries.size() == 1)
    {
        auto foundEnum = std::find_if(Enums.begin(), Enums.end(),
            [&](Enumeration* _enum) { return _enum->name == Name; });

        if (foundEnum != Enums.end())
            return *foundEnum;

        if (!Create)
            return nullptr;

        auto _enum = new Enumeration();
        _enum->name = Name;
        _enum->_namespace = this;
        Enums.push_back(_enum);
        return _enum;
    }

    auto enumName = entries[entries.size() - 1];

    std::vector<std::string> namespaces;
    std::copy_n(entries.begin(), entries.size() - 1, std::back_inserter(namespaces));

    auto _namespace = FindNamespace(namespaces);
    if (!_namespace)
        return nullptr;

    return _namespace->FindEnum(enumName, Create);
}

Enumeration* DeclarationContext::FindEnumWithItem(const std::string& Name)
{
    auto foundEnumIt = std::find_if(Enums.begin(), Enums.end(), 
        [&](Enumeration* _enum) { return _enum->FindItemByName(Name) != nullptr; });
    if (foundEnumIt != Enums.end())
        return *foundEnumIt;
    for (auto it = Namespaces.begin(); it != Namespaces.end(); ++it)
    {
        auto foundEnum = (*it)->FindEnumWithItem(Name);
        if (foundEnum != nullptr)
            return foundEnum;
    }
    for (auto it = Classes.begin(); it != Classes.end(); ++it)
    {
        auto foundEnum = (*it)->FindEnumWithItem(Name);
        if (foundEnum != nullptr)
            return foundEnum;
    }
    return nullptr;
}

Function* DeclarationContext::FindFunction(const std::string& USR)
{
    auto foundFunction = std::find_if(Functions.begin(), Functions.end(),
        [&](Function* func) { return func->USR == USR; });

    if (foundFunction != Functions.end())
        return *foundFunction;

    auto foundTemplate = std::find_if(Templates.begin(), Templates.end(),
        [&](Template* t) { return t->TemplatedDecl && t->TemplatedDecl->USR == USR; });

    if (foundTemplate != Templates.end())
        return static_cast<Function*>((*foundTemplate)->TemplatedDecl);

    return nullptr;
}

TypedefDecl* DeclarationContext::FindTypedef(const std::string& Name, bool Create)
{
    auto foundTypedef = std::find_if(Typedefs.begin(), Typedefs.end(),
            [&](TypedefDecl* tdef) { return tdef->name == Name; });

    if (foundTypedef != Typedefs.end())
        return *foundTypedef;

    if (!Create)
        return nullptr;
     
    auto tdef = new TypedefDecl();
    tdef->name = Name;
    tdef->_namespace = this;

    return tdef;
}

TypeAlias* DeclarationContext::FindTypeAlias(const std::string& Name, bool Create)
{
    auto foundTypeAlias = std::find_if(TypeAliases.begin(), TypeAliases.end(),
        [&](TypeAlias* talias) { return talias->name == Name; });

    if (foundTypeAlias != TypeAliases.end())
        return *foundTypeAlias;

    if (!Create)
        return nullptr;

    auto talias = new TypeAlias();
    talias->name = Name;
    talias->_namespace = this;

    return talias;
}

Variable* DeclarationContext::FindVariable(const std::string& USR)
{
    auto found = std::find_if(Variables.begin(), Variables.end(),
        [&](Variable* var) { return var->USR == USR; });

    if (found != Variables.end())
        return *found;

    return nullptr;
}

Friend* DeclarationContext::FindFriend(const std::string& USR)
{
    auto found = std::find_if(Friends.begin(), Friends.end(),
        [&](Friend* var) { return var->USR == USR; });

    if (found != Friends.end())
        return *found;

    return nullptr;
}

TypedefNameDecl::TypedefNameDecl(DeclarationKind Kind) : Declaration(Kind) {}

TypedefNameDecl::~TypedefNameDecl() {}

TypedefDecl::TypedefDecl() : TypedefNameDecl(DeclarationKind::Typedef) {}

TypedefDecl::~TypedefDecl() {}

TypeAlias::TypeAlias() : TypedefNameDecl(DeclarationKind::TypeAlias), describedAliasTemplate(0) {}

TypeAlias::~TypeAlias() {}

Friend::Friend() : CppSharp::CppParser::AST::Declaration(DeclarationKind::Friend), declaration(0) {}

Friend::~Friend() {}

StatementObsolete::StatementObsolete(const std::string& str, StatementClassObsolete stmtClass, Declaration* decl) : string(str), _class(stmtClass), decl(decl) {}

ExpressionObsolete::ExpressionObsolete(const std::string& str, StatementClassObsolete stmtClass, Declaration* decl)
    : StatementObsolete(str, stmtClass, decl) {}

BinaryOperatorObsolete::BinaryOperatorObsolete(const std::string& str, ExpressionObsolete* lhs, ExpressionObsolete* rhs, const std::string& opcodeStr)
    : ExpressionObsolete(str, StatementClassObsolete::BinaryOperator), LHS(lhs), RHS(rhs), opcodeStr(opcodeStr) {}

BinaryOperatorObsolete::~BinaryOperatorObsolete()
{
    deleteExpression(LHS);
    deleteExpression(RHS);
}


CallExprObsolete::CallExprObsolete(const std::string& str, Declaration* decl)
    : ExpressionObsolete(str, StatementClassObsolete::CallExprClass, decl) {}

CallExprObsolete::~CallExprObsolete()
{
    for (auto& arg : Arguments)
        deleteExpression(arg);
}

DEF_VECTOR(CallExprObsolete, ExpressionObsolete*, Arguments)

CXXConstructExprObsolete::CXXConstructExprObsolete(const std::string& str, Declaration* decl)
    : ExpressionObsolete(str, StatementClassObsolete::CXXConstructExprClass, decl) {}

CXXConstructExprObsolete::~CXXConstructExprObsolete()
{
    for (auto& arg : Arguments)
        deleteExpression(arg);
}

DEF_VECTOR(CXXConstructExprObsolete, ExpressionObsolete*, Arguments)

Parameter::Parameter()
    : Declaration(DeclarationKind::Parameter)
    , isIndirect(false)
    , hasDefaultValue(false)
    , defaultArgument(0)
    , defaultValue(0)
{
}

Parameter::~Parameter()
{
    deleteExpression(defaultArgument);
}

Function::Function() 
    : DeclarationContext(DeclarationKind::Function)
    , isReturnIndirect(false)
    , isConstExpr(false)
    , isVariadic(false)
    , isInline(false)
    , isPure(false)
    , isDeleted(false)
    , isDefaulted(false)
    , friendKind(FriendKind::None)
    , operatorKind(CXXOperatorKind::None)
    , callingConvention(CallingConvention::Default)
    , specializationInfo(0)
    , instantiatedFrom(0)
    , bodyStmt(0)
{
}

Function::~Function() {}
DEF_VECTOR(Function, Parameter*, Parameters)

Method::Method() 
    : Function()
    , isVirtual(false)
    , isStatic(false)
    , isConst(false)
    , isExplicit(false)
    , isDefaultConstructor(false)
    , isCopyConstructor(false)
    , isMoveConstructor(false)
    , refQualifier(RefQualifierKind::None)
{ 
    kind = DeclarationKind::Method; 
}

Method::~Method() {}

DEF_VECTOR(Method, Method*, OverriddenMethods)

// Enumeration

Enumeration::Enumeration() : DeclarationContext(DeclarationKind::Enumeration),
    modifiers((EnumModifiers)0), type(0), builtinType(0) {}

Enumeration::~Enumeration() {}

DEF_VECTOR(Enumeration, Enumeration::Item*, Items)

Enumeration::Item::Item() : Declaration(DeclarationKind::EnumerationItem) {}

Enumeration::Item::Item(const Item& rhs) : Declaration(rhs),
    expression(rhs.expression), value(rhs.value) {}

Enumeration::Item::~Item() {}

Enumeration::Item* Enumeration::FindItemByName(const std::string& Name)
{
    auto foundEnumItem = std::find_if(Items.begin(), Items.end(),
        [&](Item* _item) { return _item->name == Name; });
    if (foundEnumItem != Items.end())
        return *foundEnumItem;
    return nullptr;
}

Variable::Variable() : Declaration(DeclarationKind::Variable),
    isConstExpr(false), initializer(0) {}

Variable::~Variable() {}

BaseClassSpecifier::BaseClassSpecifier() : type(0), offset(0) {}

Field::Field() : Declaration(DeclarationKind::Field), _class(0),
    isBitField(false), bitWidth(0) {}

Field::~Field() {}

AccessSpecifierDecl::AccessSpecifierDecl()
    : Declaration(DeclarationKind::AccessSpecifier) {}

AccessSpecifierDecl::~AccessSpecifierDecl() {}

Class::Class()
    : DeclarationContext(DeclarationKind::Class)
    , isPOD(false)
    , isAbstract(false)
    , isUnion(false)
    , isDynamic(false)
    , isPolymorphic(false)
    , hasNonTrivialDefaultConstructor(false)
    , hasNonTrivialCopyConstructor(false)
    , hasNonTrivialDestructor(false)
    , isExternCContext(false)
    , isInjected(false)
    , layout(0)
    , tagKind(TagKind::Struct)
{
}

Class::~Class()
{
    if (layout)
        delete layout;
}

DEF_VECTOR(Class, BaseClassSpecifier*, Bases)
DEF_VECTOR(Class, Field*, Fields)
DEF_VECTOR(Class, Method*, Methods)
DEF_VECTOR(Class, AccessSpecifierDecl*, Specifiers)

Template::Template() : Declaration(DeclarationKind::Template),
    TemplatedDecl(0) {}

Template::Template(DeclarationKind kind) : Declaration(kind), TemplatedDecl(0) {}

DEF_VECTOR(Template, Declaration*, Parameters)

TypeAliasTemplate::TypeAliasTemplate() : Template(DeclarationKind::TypeAliasTemplate) {}

TypeAliasTemplate::~TypeAliasTemplate() {}

ClassTemplate::ClassTemplate() : Template(DeclarationKind::ClassTemplate) {}

ClassTemplate::~ClassTemplate() {}

DEF_VECTOR(ClassTemplate, ClassTemplateSpecialization*, Specializations)

ClassTemplateSpecialization::ClassTemplateSpecialization() 
    : Class()
    , templatedDecl(0)
{ 
    kind = DeclarationKind::ClassTemplateSpecialization; 
}

ClassTemplateSpecialization::~ClassTemplateSpecialization() {}

DEF_VECTOR(ClassTemplateSpecialization, TemplateArgument, Arguments)

ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization()
    : ClassTemplateSpecialization()
{ 
    kind = DeclarationKind::ClassTemplatePartialSpecialization; 
}

ClassTemplatePartialSpecialization::~ClassTemplatePartialSpecialization() {}

FunctionTemplate::FunctionTemplate() : Template(DeclarationKind::FunctionTemplate) {}

FunctionTemplate::~FunctionTemplate() {}

DEF_VECTOR(FunctionTemplate, FunctionTemplateSpecialization*, Specializations)

FunctionTemplateSpecialization* FunctionTemplate::FindSpecialization(const std::string& usr)
{
    auto foundSpec = std::find_if(Specializations.begin(), Specializations.end(),
        [&](FunctionTemplateSpecialization* cts) { return cts->specializedFunction->USR == usr; });

    if (foundSpec != Specializations.end())
        return static_cast<FunctionTemplateSpecialization*>(*foundSpec);

    return nullptr;
}

FunctionTemplateSpecialization::FunctionTemplateSpecialization()
    : _template(0)
    , specializedFunction(0)
{
}

FunctionTemplateSpecialization::~FunctionTemplateSpecialization()
{
}

DEF_VECTOR(FunctionTemplateSpecialization, TemplateArgument, Arguments)

VarTemplate::VarTemplate() : Template(DeclarationKind::VarTemplate) {}

VarTemplate::~VarTemplate() {}

DEF_VECTOR(VarTemplate, VarTemplateSpecialization*, Specializations)

VarTemplateSpecialization* VarTemplate::FindSpecialization(const std::string& usr)
{
    auto foundSpec = std::find_if(Specializations.begin(), Specializations.end(),
        [&](VarTemplateSpecialization* cts) { return cts->USR == usr; });

    if (foundSpec != Specializations.end())
        return static_cast<VarTemplateSpecialization*>(*foundSpec);

    return nullptr;
}

VarTemplatePartialSpecialization* VarTemplate::FindPartialSpecialization(const std::string& usr)
{
    auto foundSpec = FindSpecialization(usr);
    if (foundSpec != nullptr)
        return static_cast<VarTemplatePartialSpecialization*>(foundSpec);
    return nullptr;
}

VarTemplateSpecialization::VarTemplateSpecialization()
    : Variable()
    , templatedDecl(0)
{
    kind = DeclarationKind::VarTemplateSpecialization;
}

VarTemplateSpecialization::~VarTemplateSpecialization() {}

DEF_VECTOR(VarTemplateSpecialization, TemplateArgument, Arguments)

VarTemplatePartialSpecialization::VarTemplatePartialSpecialization()
    : VarTemplateSpecialization()
{
    kind = DeclarationKind::VarTemplatePartialSpecialization;
}

VarTemplatePartialSpecialization::~VarTemplatePartialSpecialization()
{
}

UnresolvedUsingTypename::UnresolvedUsingTypename() : Declaration(DeclarationKind::UnresolvedUsingTypename) {}

UnresolvedUsingTypename::~UnresolvedUsingTypename() {}

Namespace::Namespace() 
    : DeclarationContext(DeclarationKind::Namespace)
    , isInline(false) 
{
}

Namespace::~Namespace() {}

PreprocessedEntity::PreprocessedEntity()
    : macroLocation(AST::MacroLocation::Unknown),
      originalPtr(0), kind(DeclarationKind::PreprocessedEntity) {}

MacroDefinition::MacroDefinition()
    : lineNumberStart(0), lineNumberEnd(0) { kind = DeclarationKind::MacroDefinition; }

MacroDefinition::~MacroDefinition() {}

MacroExpansion::MacroExpansion() : definition(0) { kind = DeclarationKind::MacroExpansion; }

MacroExpansion::~MacroExpansion() {}

TranslationUnit::TranslationUnit() { kind = DeclarationKind::TranslationUnit; }

TranslationUnit::~TranslationUnit() {}
DEF_VECTOR(TranslationUnit, MacroDefinition*, Macros)

NativeLibrary::NativeLibrary()
    : archType(AST::ArchType::UnknownArch) {}

NativeLibrary::~NativeLibrary() {}

// NativeLibrary
DEF_VECTOR_STRING(NativeLibrary, Symbols)
DEF_VECTOR_STRING(NativeLibrary, Dependencies)

// ASTContext
DEF_VECTOR(ASTContext, TranslationUnit*, TranslationUnits)

ClassTemplateSpecialization* ClassTemplate::FindSpecialization(const std::string& usr)
{
    auto foundSpec = std::find_if(Specializations.begin(), Specializations.end(),
        [&](ClassTemplateSpecialization* cts) { return cts->USR == usr; });

    if (foundSpec != Specializations.end())
        return static_cast<ClassTemplateSpecialization*>(*foundSpec);

    return nullptr;
}

ClassTemplatePartialSpecialization* ClassTemplate::FindPartialSpecialization(const std::string& usr)
{
    auto foundSpec = FindSpecialization(usr);
    if (foundSpec != nullptr)
        return static_cast<ClassTemplatePartialSpecialization*>(foundSpec);
    return nullptr;
}

ASTContext::ASTContext() {}

ASTContext::~ASTContext() {}

TranslationUnit* ASTContext::FindOrCreateModule(std::string File)
{
    auto normalizedFile = normalizePath(File);

    auto existingUnit = std::find_if(TranslationUnits.begin(),
        TranslationUnits.end(), [&](TranslationUnit* unit) {
            return unit && unit->fileName == normalizedFile;
    });

    if (existingUnit != TranslationUnits.end())
        return *existingUnit;

    auto unit = new TranslationUnit();
    unit->fileName = normalizedFile;
    TranslationUnits.push_back(unit);

    return unit;
}

// Comments
Comment::Comment(CommentKind kind) : kind(kind) {}

RawComment::RawComment() : fullCommentBlock(0) {}

RawComment::~RawComment()
{
    if (fullCommentBlock)
        delete fullCommentBlock;
}

FullComment::FullComment() : Comment(CommentKind::FullComment) {}

FullComment::~FullComment()
{
    for (auto& block : Blocks)
    {
        // HACK: see https://github.com/mono/CppSharp/issues/599
        switch (block->kind)
        {
        case CommentKind::BlockCommandComment:
            delete static_cast<BlockCommandComment*>(block);
            break;
        case CommentKind::ParamCommandComment:
            delete static_cast<ParamCommandComment*>(block);
            break;
        case CommentKind::TParamCommandComment:
            delete static_cast<TParamCommandComment*>(block);
            break;
        case CommentKind::VerbatimBlockComment:
            delete static_cast<VerbatimBlockComment*>(block);
            break;
        case CommentKind::VerbatimLineComment:
            delete static_cast<VerbatimLineComment*>(block);
            break;
        case CommentKind::ParagraphComment:
            delete static_cast<ParagraphComment*>(block);
            break;
        default:
            delete block;
            break;
        }
    }
}

DEF_VECTOR(FullComment, BlockContentComment*, Blocks)

BlockContentComment::BlockContentComment() : Comment(CommentKind::BlockContentComment) {}

BlockContentComment::BlockContentComment(CommentKind Kind) : Comment(Kind) {}

BlockCommandComment::Argument::Argument() {}

BlockCommandComment::Argument::Argument(const Argument& rhs) : text(rhs.text) {}

BlockCommandComment::Argument::~Argument() {}

BlockCommandComment::BlockCommandComment() : BlockContentComment(CommentKind::BlockCommandComment), commandId(0), paragraphComment(0) {}

BlockCommandComment::BlockCommandComment(CommentKind Kind) : BlockContentComment(Kind), commandId(0), paragraphComment(0) {}

BlockCommandComment::~BlockCommandComment()
{
    delete paragraphComment;
}

DEF_VECTOR(BlockCommandComment, BlockCommandComment::Argument, Arguments)

ParamCommandComment::ParamCommandComment() : BlockCommandComment(CommentKind::ParamCommandComment), direction(PassDirection::In), paramIndex(0) {}

TParamCommandComment::TParamCommandComment() : BlockCommandComment(CommentKind::TParamCommandComment) {}

DEF_VECTOR(TParamCommandComment, unsigned, Position)

VerbatimBlockComment::VerbatimBlockComment() : BlockCommandComment(CommentKind::VerbatimBlockComment) {}

VerbatimBlockComment::~VerbatimBlockComment()
{
    for (auto& line : Lines)
        delete line;
}

DEF_VECTOR(VerbatimBlockComment, VerbatimBlockLineComment*, Lines)

VerbatimLineComment::VerbatimLineComment() : BlockCommandComment(CommentKind::VerbatimLineComment) {}

ParagraphComment::ParagraphComment() : BlockContentComment(CommentKind::ParagraphComment), isWhitespace(false) {}

ParagraphComment::~ParagraphComment()
{
    for (auto& content : Content)
    {
        // HACK: see https://github.com/mono/CppSharp/issues/599
        switch (content->kind)
        {
        case CommentKind::InlineCommandComment:
            delete static_cast<InlineCommandComment*>(content);
            break;
        case CommentKind::HTMLTagComment:
            delete static_cast<HTMLTagComment*>(content);
            break;
        case CommentKind::HTMLStartTagComment:
            delete static_cast<HTMLStartTagComment*>(content);
            break;
        case CommentKind::HTMLEndTagComment:
            delete static_cast<HTMLEndTagComment*>(content);
            break;
        case CommentKind::TextComment:
            delete static_cast<TextComment*>(content);
            break;
        default:
            delete content;
            break;
        }
    }
}

DEF_VECTOR(ParagraphComment, InlineContentComment*, Content)

HTMLTagComment::HTMLTagComment() : InlineContentComment(CommentKind::HTMLTagComment) {}

HTMLTagComment::HTMLTagComment(CommentKind Kind) : InlineContentComment(Kind) {}

HTMLStartTagComment::Attribute::Attribute() {}

HTMLStartTagComment::Attribute::Attribute(const Attribute& rhs) : name(rhs.name), value(rhs.value) {}

HTMLStartTagComment::HTMLStartTagComment() : HTMLTagComment(CommentKind::HTMLStartTagComment) {}

DEF_VECTOR(HTMLStartTagComment, HTMLStartTagComment::Attribute, Attributes)

HTMLEndTagComment::HTMLEndTagComment() : HTMLTagComment(CommentKind::HTMLEndTagComment) {}

InlineContentComment::InlineContentComment() : Comment(CommentKind::InlineContentComment), hasTrailingNewline(false) {}

InlineContentComment::InlineContentComment(CommentKind Kind) : Comment(Kind), hasTrailingNewline(false) {}

TextComment::TextComment() : InlineContentComment(CommentKind::TextComment) {}

InlineCommandComment::Argument::Argument() {}

InlineCommandComment::Argument::Argument(const Argument& rhs) : text(rhs.text) {}

InlineCommandComment::Argument::~Argument() {}

InlineCommandComment::InlineCommandComment()
    : InlineContentComment(CommentKind::InlineCommandComment), commandId(0), commentRenderKind(RenderNormal) {}

DEF_VECTOR(InlineCommandComment, InlineCommandComment::Argument, Arguments)

VerbatimBlockLineComment::VerbatimBlockLineComment() : Comment(CommentKind::VerbatimBlockLineComment) {}

} } }