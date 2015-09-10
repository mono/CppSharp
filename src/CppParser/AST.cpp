/************************************************************************
*
* CppSharp
* Licensed under the MIT license.
*
************************************************************************/

#include "AST.h"
#include <algorithm>
#include <string>
#include <vector>
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

Type::Type(TypeKind kind) : Kind(kind) {}
Type::Type(const Type& rhs) : Kind(rhs.Kind), IsDependent(rhs.IsDependent) {}

QualifiedType::QualifiedType() : Type(0) {}

TagType::TagType() : Type(TypeKind::Tag) {}

ArrayType::ArrayType() : Type(TypeKind::Array) {}

FunctionType::FunctionType() : Type(TypeKind::Function) {}
DEF_VECTOR(FunctionType, Parameter*, Parameters)

PointerType::PointerType() : Type(TypeKind::Pointer) {}

MemberPointerType::MemberPointerType() : Type(TypeKind::MemberPointer) {}

TypedefType::TypedefType() : Type(TypeKind::Typedef), Declaration(0) {}

AttributedType::AttributedType() : Type(TypeKind::Attributed) {}

DecayedType::DecayedType() : Type(TypeKind::Decayed) {}

// Template
Template::Template(DeclarationKind kind) : Declaration(kind) {}

TemplateArgument::TemplateArgument() : Declaration(0) {}

TemplateSpecializationType::TemplateSpecializationType()
    : Type(TypeKind::TemplateSpecialization), Template(0), Desugared(0) {}
TemplateSpecializationType::TemplateSpecializationType(
    const TemplateSpecializationType& rhs) : Type(rhs),
    Arguments(rhs.Arguments), Template(rhs.Template), Desugared(rhs.Desugared) {}

DEF_VECTOR(TemplateSpecializationType, TemplateArgument, Arguments)

// TemplateParameter
TemplateParameter::TemplateParameter() 
    : IsTypeParameter(false)
{
}

TemplateParameter::TemplateParameter(const TemplateParameter& rhs)
    : Name(rhs.Name)
    , IsTypeParameter(rhs.IsTypeParameter)
{
}

DEF_STRING(TemplateParameter, Name)

TemplateParameterType::TemplateParameterType() : Type(TypeKind::TemplateParameter) {}

TemplateParameterSubstitutionType::TemplateParameterSubstitutionType()
    : Type(TypeKind::TemplateParameterSubstitution) {}

InjectedClassNameType::InjectedClassNameType()
    : Type(TypeKind::InjectedClassName)
    , TemplateSpecialization(0)
    , Class(0)
{
}

DependentNameType::DependentNameType() : Type(TypeKind::DependentName) {}

PackExpansionType::PackExpansionType() : Type(TypeKind::PackExpansion) {}

BuiltinType::BuiltinType() : CppSharp::CppParser::AST::Type(TypeKind::Builtin) {}

VTableComponent::VTableComponent() : Offset(0), Declaration(0) {}

// VTableLayout
VTableLayout::VTableLayout() {}
VTableLayout::VTableLayout(const VTableLayout& rhs) : Components(rhs.Components) {}
VTableLayout::~VTableLayout() {}

DEF_VECTOR(VTableLayout, VTableComponent, Components)

VFTableInfo::VFTableInfo() : VBTableIndex(0), VFPtrOffset(0), VFPtrFullOffset(0) {}
VFTableInfo::VFTableInfo(const VFTableInfo& rhs) : VBTableIndex(rhs.VBTableIndex),
    VFPtrOffset(rhs.VFPtrOffset), VFPtrFullOffset(rhs.VFPtrFullOffset),
    Layout(rhs.Layout) {}

ClassLayout::ClassLayout() : ABI(CppAbi::Itanium), HasOwnVFPtr(false),
    VBPtrOffset(0), Alignment(0), Size(0), DataSize(0) {}

DEF_VECTOR(ClassLayout, VFTableInfo, VFTables)

Declaration::Declaration(DeclarationKind kind)
    : Kind(kind)
    , Access(AccessSpecifier::Public)
    , _Namespace(0)
    , Location(0)
    , LineNumberStart(0)
    , LineNumberEnd(0)
    , Comment(0)
    , IsIncomplete(false)
    , IsDependent(false)
    , CompleteDeclaration(0)
    , DefinitionOrder(0)
    , OriginalPtr(0)
{
}

Declaration::Declaration(const Declaration& rhs)
    : Kind(rhs.Kind)
    , Access(rhs.Access)
    , _Namespace(rhs._Namespace)
    , Location(rhs.Location.ID)
    , LineNumberStart(rhs.LineNumberStart)
    , LineNumberEnd(rhs.LineNumberEnd)
    , Name(rhs.Name)
    , Comment(rhs.Comment)
    , DebugText(rhs.DebugText)
    , IsIncomplete(rhs.IsIncomplete)
    , IsDependent(rhs.IsDependent)
    , CompleteDeclaration(rhs.CompleteDeclaration)
    , DefinitionOrder(rhs.DefinitionOrder)
    , PreprocessedEntities(rhs.PreprocessedEntities)
    , OriginalPtr(rhs.OriginalPtr)
{
}

Declaration::~Declaration()
{
}

DEF_STRING(Declaration, Name)
DEF_STRING(Declaration, DebugText)
DEF_VECTOR(Declaration, PreprocessedEntity*, PreprocessedEntities)

DeclarationContext::DeclarationContext(DeclarationKind kind)
    : Declaration(kind)
    , IsAnonymous(false)
{}

DEF_VECTOR(DeclarationContext, Namespace*, Namespaces)
DEF_VECTOR(DeclarationContext, Enumeration*, Enums)
DEF_VECTOR(DeclarationContext, Function*, Functions)
DEF_VECTOR(DeclarationContext, Class*, Classes)
DEF_VECTOR(DeclarationContext, Template*, Templates)
DEF_VECTOR(DeclarationContext, TypedefDecl*, Typedefs)
DEF_VECTOR(DeclarationContext, Variable*, Variables)
DEF_VECTOR(DeclarationContext, Friend*, Friends)

Declaration* DeclarationContext::FindAnonymous(const std::string& key)
{
    auto it = Anonymous.find(key);
    return (it != Anonymous.end()) ? it->second : 0;
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
                return ns->Name == _namespace;
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
        _namespace->Name = Name;
        _namespace->_Namespace = this;

        Namespaces.push_back(_namespace);
    }

    return _namespace;
}

Class* DeclarationContext::FindClass(const std::string& Name)
{
    if (Name.empty()) return nullptr;

    auto entries = split<std::string>(Name, "::");

    if (entries.size() == 1)
    {
        auto _class = std::find_if(Classes.begin(), Classes.end(),
            [&](Class* klass) { return klass->Name == Name; });

        return _class != Classes.end() ? *_class : nullptr;
    }

    auto className = entries[entries.size() - 1];

    std::vector<std::string> namespaces;
    std::copy_n(entries.begin(), entries.size() - 1, std::back_inserter(namespaces));

    auto _namespace = FindNamespace(namespaces);
    if (!_namespace)
        return nullptr;

    return _namespace->FindClass(className);
}

Class* DeclarationContext::CreateClass(std::string Name, bool IsComplete)
{
    auto _class = new Class();
    _class->Name = Name;
    _class->_Namespace = this;
    _class->IsIncomplete = !IsComplete;

    return _class;
}

Class* DeclarationContext::FindClass(const std::string& Name, bool IsComplete,
        bool Create)
{
    auto _class = FindClass(Name);

    if (!_class)
    {
        if (Create)
        {
            _class = CreateClass(Name, IsComplete);
            Classes.push_back(_class);
        }
        
        return _class;
    }

    if (_class->IsIncomplete == !IsComplete)
        return _class;

    if (!Create)
        return nullptr;

    auto newClass = CreateClass(Name, IsComplete);

    // Replace the incomplete declaration with the complete one.
    if (_class->IsIncomplete)
    {
        _class->CompleteDeclaration = newClass;

        std::replace_if(Classes.begin(), Classes.end(),
            [&](Class* klass) { return klass == _class; }, newClass);
    }

    return newClass;
}

Enumeration* DeclarationContext::FindEnum(void* OriginalPtr)
{
    auto foundEnum = std::find_if(Enums.begin(), Enums.end(),
        [&](Enumeration* enumeration) { return enumeration->OriginalPtr == OriginalPtr; });

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
            [&](Enumeration* _enum) { return _enum->Name == Name; });

        if (foundEnum != Enums.end())
            return *foundEnum;

        if (!Create)
            return nullptr;

        auto _enum = new Enumeration();
        _enum->Name = Name;
        _enum->_Namespace = this;
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
        [&](Template* t) { return t->TemplatedDecl->USR == USR; });

    if (foundTemplate != Templates.end())
        return static_cast<Function*>((*foundTemplate)->TemplatedDecl);

    return nullptr;
}

ClassTemplate*
DeclarationContext::FindClassTemplate(const std::string& USR)
{
    auto foundTemplate = std::find_if(Templates.begin(), Templates.end(),
        [&](Template* t) { return t->USR == USR; });

    if (foundTemplate != Templates.end())
        return static_cast<ClassTemplate*>(*foundTemplate);

    return nullptr;
}

FunctionTemplate*
DeclarationContext::FindFunctionTemplate(const std::string& USR)
{
    auto foundTemplate = std::find_if(Templates.begin(), Templates.end(),
        [&](Template* t) { return t->USR == USR; }
    );

    if (foundTemplate != Templates.end())
        return static_cast<FunctionTemplate*>(*foundTemplate);

    return nullptr;
}

TypedefDecl* DeclarationContext::FindTypedef(const std::string& Name, bool Create)
{
    auto foundTypedef = std::find_if(Typedefs.begin(), Typedefs.end(),
            [&](TypedefDecl* tdef) { return tdef->Name == Name; });

    if (foundTypedef != Typedefs.end())
        return *foundTypedef;

    if (!Create)
        return nullptr;
     
    auto tdef = new TypedefDecl();
    tdef->Name = Name;
    tdef->_Namespace = this;
    Typedefs.push_back(tdef);

    return tdef;
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

TypedefDecl::TypedefDecl() : Declaration(DeclarationKind::Typedef) {}

Friend::Friend() : CppSharp::CppParser::AST::Declaration(DeclarationKind::Friend), Declaration(0) {}

DEF_STRING(Statement, String)

Statement::Statement(const std::string& str, StatementClass stmtClass, Declaration* decl) : String(str), Class(stmtClass), Decl(decl) {}

Expression::Expression(const std::string& str, StatementClass stmtClass, Declaration* decl)
    : Statement(str, stmtClass, decl) {}

BinaryOperator::BinaryOperator(const std::string& str, Expression* lhs, Expression* rhs, const std::string& opcodeStr)
    : Expression(str, StatementClass::BinaryOperator), LHS(lhs), RHS(rhs), OpcodeStr(opcodeStr) {}

DEF_STRING(BinaryOperator, OpcodeStr)

CXXConstructExpr::CXXConstructExpr(const std::string& str, Declaration* decl)
    : Expression(str, StatementClass::CXXConstructExprClass, decl) {}

DEF_VECTOR(CXXConstructExpr, Expression*, Arguments)

Parameter::Parameter() : Declaration(DeclarationKind::Parameter),
    IsIndirect(false), HasDefaultValue(false), DefaultArgument(0) {}

Parameter::~Parameter()
{
    if (DefaultArgument)
        delete DefaultArgument;
}

Function::Function() 
    : Declaration(DeclarationKind::Function)
    , IsReturnIndirect(false)
    , SpecializationInfo(0)
{
}

DEF_STRING(Function, Mangled)
DEF_STRING(Function, Signature)
DEF_VECTOR(Function, Parameter*, Parameters)

Method::Method() 
    : Function()
    , AccessDecl(0)
    , IsVirtual(false)
    , IsStatic(false)
    , IsConst(false)
    , IsImplicit(false)
    , IsExplicit(false)
    , IsOverride(false)
    , IsDefaultConstructor(false)
    , IsCopyConstructor(false)
    , IsMoveConstructor(false)
{ 
    Kind = DeclarationKind::Method; 
}

// Enumeration

Enumeration::Enumeration() : DeclarationContext(DeclarationKind::Enumeration),
    Modifiers((EnumModifiers)0), Type(0), BuiltinType(0) {}

DEF_VECTOR(Enumeration, Enumeration::Item, Items)

Enumeration::Item::Item() : Declaration(DeclarationKind::EnumerationItem) {}

Enumeration::Item::Item(const Item& rhs) : Declaration(rhs),
    Expression(rhs.Expression), Value(rhs.Value) {}

DEF_STRING(Enumeration::Item, Expression)

Enumeration::Item* Enumeration::FindItemByName(const std::string& Name)
{
    auto foundEnumItem = std::find_if(Items.begin(), Items.end(),
        [&](Item _item) { return _item.Name == Name; });
    if (foundEnumItem != Items.end())
        return &*foundEnumItem;
    return nullptr;
}

Variable::Variable() : Declaration(DeclarationKind::Variable) {}

DEF_STRING(Variable, Mangled)

BaseClassSpecifier::BaseClassSpecifier() : Type(0), Offset(0) {}

Field::Field() : Declaration(DeclarationKind::Field), Class(0),
    IsBitField(false), BitWidth(0) {}

AccessSpecifierDecl::AccessSpecifierDecl()
    : Declaration(DeclarationKind::AccessSpecifier) {}

Class::Class()
    : DeclarationContext(DeclarationKind::Class)
    , IsPOD(false)
    , IsAbstract(false)
    , IsUnion(false)
    , IsDynamic(false)
    , IsPolymorphic(false)
    , HasNonTrivialDefaultConstructor(false)
    , HasNonTrivialCopyConstructor(false)
    , HasNonTrivialDestructor(false)
    , IsExternCContext(false)
    , Layout(0)
{
}

Class::~Class()
{
    if (Layout)
        delete Layout;
}

DEF_VECTOR(Class, BaseClassSpecifier*, Bases)
DEF_VECTOR(Class, Field*, Fields)
DEF_VECTOR(Class, Method*, Methods)
DEF_VECTOR(Class, AccessSpecifierDecl*, Specifiers)

Template::Template() : Declaration(DeclarationKind::Template),
    TemplatedDecl(0) {}

DEF_VECTOR(Template, TemplateParameter, Parameters)

ClassTemplate::ClassTemplate() : Template(DeclarationKind::ClassTemplate) {}

DEF_VECTOR(ClassTemplate, ClassTemplateSpecialization*, Specializations)

ClassTemplateSpecialization::ClassTemplateSpecialization() 
    : Class()
    , TemplatedDecl(0)
{ 
    Kind = DeclarationKind::ClassTemplateSpecialization; 
}

DEF_VECTOR(ClassTemplateSpecialization, TemplateArgument, Arguments)

ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization()
    : ClassTemplateSpecialization()
{ 
    Kind = DeclarationKind::ClassTemplatePartialSpecialization; 
}

FunctionTemplate::FunctionTemplate() : Template(DeclarationKind::FunctionTemplate) {}

DEF_VECTOR(FunctionTemplate, FunctionTemplateSpecialization*, Specializations)

FunctionTemplateSpecialization* FunctionTemplate::FindSpecialization(const std::string& usr)
{
    auto foundSpec = std::find_if(Specializations.begin(), Specializations.end(),
        [&](FunctionTemplateSpecialization* cts) { return cts->SpecializedFunction->USR == usr; });

    if (foundSpec != Specializations.end())
        return static_cast<FunctionTemplateSpecialization*>(*foundSpec);

    return nullptr;
}

FunctionTemplateSpecialization::FunctionTemplateSpecialization()
    : Template(0)
    , SpecializedFunction(0)
{
}

DEF_VECTOR(FunctionTemplateSpecialization, TemplateArgument, Arguments)

Namespace::Namespace() 
    : DeclarationContext(DeclarationKind::Namespace)
    , IsInline(false) 
{
}

PreprocessedEntity::PreprocessedEntity()
    : Declaration(DeclarationKind::PreprocessedEntity),
      MacroLocation(AST::MacroLocation::Unknown) {}

MacroDefinition::MacroDefinition() { Kind = DeclarationKind::MacroDefinition; }

DEF_STRING(MacroDefinition, Expression)

MacroExpansion::MacroExpansion() { Kind = DeclarationKind::MacroExpansion; }

DEF_STRING(MacroExpansion, Text)

TranslationUnit::TranslationUnit() { Kind = DeclarationKind::TranslationUnit; }

DEF_STRING(TranslationUnit, FileName)
DEF_VECTOR(TranslationUnit, MacroDefinition*, Macros)

NativeLibrary::NativeLibrary()
    : ArchType(AST::ArchType::UnknownArch)
{
}

// NativeLibrary
DEF_STRING(NativeLibrary, FileName)
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

TranslationUnit* ASTContext::FindOrCreateModule(std::string File)
{
    auto normalizedFile = normalizePath(File);

    auto existingUnit = std::find_if(TranslationUnits.begin(),
        TranslationUnits.end(), [&](TranslationUnit* unit) {
            return unit && unit->FileName == normalizedFile;
    });

    if (existingUnit != TranslationUnits.end())
        return *existingUnit;

    auto unit = new TranslationUnit();
    unit->FileName = normalizedFile;
    TranslationUnits.push_back(unit);

    return unit;
}

// Comments
Comment::Comment(CommentKind kind) : Kind(kind) {}

DEF_STRING(RawComment, Text)
DEF_STRING(RawComment, BriefText)

RawComment::RawComment() : FullCommentBlock(0) {}

FullComment::FullComment() : Comment(CommentKind::FullComment) {}

DEF_VECTOR(FullComment, BlockContentComment*, Blocks)

BlockContentComment::BlockContentComment() : Comment(CommentKind::BlockContentComment) {}

BlockContentComment::BlockContentComment(CommentKind Kind) : Comment(Kind) {}

BlockCommandComment::Argument::Argument() {}

DEF_STRING(BlockCommandComment::Argument, Text)

BlockCommandComment::BlockCommandComment() : BlockContentComment(CommentKind::BlockCommandComment), CommandId(0) {}

BlockCommandComment::BlockCommandComment(CommentKind Kind) : BlockContentComment(Kind) {}

DEF_VECTOR(BlockCommandComment, BlockCommandComment::Argument, Arguments)

ParamCommandComment::ParamCommandComment() : BlockCommandComment(CommentKind::ParamCommandComment), Direction(PassDirection::In), ParamIndex(0) {}

TParamCommandComment::TParamCommandComment() : BlockCommandComment(CommentKind::TParamCommandComment) {}

DEF_VECTOR(TParamCommandComment, unsigned, Position)

VerbatimBlockComment::VerbatimBlockComment() : BlockCommandComment(CommentKind::VerbatimBlockComment) {}

DEF_VECTOR(VerbatimBlockComment, VerbatimBlockLineComment*, Lines)

VerbatimLineComment::VerbatimLineComment() : BlockCommandComment(CommentKind::VerbatimLineComment) {}

DEF_STRING(VerbatimLineComment, Text)

ParagraphComment::ParagraphComment() : BlockContentComment(CommentKind::ParagraphComment), IsWhitespace(false) {}

DEF_VECTOR(ParagraphComment, InlineContentComment*, Content)

HTMLTagComment::HTMLTagComment() : InlineContentComment(CommentKind::HTMLTagComment) {}

HTMLTagComment::HTMLTagComment(CommentKind Kind) : InlineContentComment(Kind) {}

HTMLStartTagComment::Attribute::Attribute() {}

DEF_STRING(HTMLStartTagComment::Attribute, Name)

DEF_STRING(HTMLStartTagComment::Attribute, Value)

HTMLStartTagComment::HTMLStartTagComment() : HTMLTagComment(CommentKind::HTMLStartTagComment) {}

DEF_VECTOR(HTMLStartTagComment, HTMLStartTagComment::Attribute, Attributes)

DEF_STRING(HTMLStartTagComment, TagName)

HTMLEndTagComment::HTMLEndTagComment() : HTMLTagComment(CommentKind::HTMLEndTagComment) {}

DEF_STRING(HTMLEndTagComment, TagName)

InlineContentComment::InlineContentComment() : Comment(CommentKind::InlineContentComment) {}

InlineContentComment::InlineContentComment(CommentKind Kind) : Comment(Kind) {}

TextComment::TextComment() : InlineContentComment(CommentKind::TextComment) {}

DEF_STRING(TextComment, Text)

InlineCommandComment::Argument::Argument() {}

DEF_STRING(InlineCommandComment::Argument, Text)

InlineCommandComment::InlineCommandComment() : InlineContentComment(CommentKind::InlineCommandComment), CommentRenderKind(RenderNormal) {}

DEF_VECTOR(InlineCommandComment, InlineCommandComment::Argument, Arguments)

VerbatimBlockLineComment::VerbatimBlockLineComment() : Comment(CommentKind::VerbatimBlockLineComment) {}

DEF_STRING(VerbatimBlockLineComment, Text)

} } }