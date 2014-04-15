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

TemplateArgument::TemplateArgument() : Declaration(0) {}

TemplateSpecializationType::TemplateSpecializationType()
    : Type(TypeKind::TemplateSpecialization), Template(0), Desugared(0) {}
DEF_VECTOR(TemplateSpecializationType, TemplateArgument, Arguments)

// TemplateParameter
DEF_STRING(TemplateParameter, Name)

TemplateParameterType::TemplateParameterType() : Type(TypeKind::TemplateParameter) {}

TemplateParameterSubstitutionType::TemplateParameterSubstitutionType()
    : Type(TypeKind::TemplateParameterSubstitution) {}

InjectedClassNameType::InjectedClassNameType() : Type(TypeKind::InjectedClassName),
    Class(0) {}

DependentNameType::DependentNameType() : Type(TypeKind::DependentName) {}

PackExpansionType::PackExpansionType() : Type(TypeKind::PackExpansion) {}

BuiltinType::BuiltinType() : CppSharp::CppParser::AST::Type(TypeKind::Builtin) {}

VTableComponent::VTableComponent() : Offset(0), Declaration(0) {}

// VTableLayout
VTableLayout::VTableLayout() {}
DEF_VECTOR(VTableLayout, VTableComponent, Components)

VFTableInfo::VFTableInfo() : VBTableIndex(0), VFPtrOffset(0), VFPtrFullOffset(0) {}

ClassLayout::ClassLayout() : ABI(CppAbi::Itanium), HasOwnVFPtr(false),
    VBPtrOffset(0), Alignment(0), Size(0), DataSize(0) {}

DEF_VECTOR(ClassLayout, VFTableInfo, VFTables)

Declaration::Declaration(DeclarationKind kind)
    : Kind(kind)
    , Access(AccessSpecifier::Public)
    , _Namespace(0)
    , Comment(0)
    , IsIncomplete(false)
    , IsDependent(false)
    , CompleteDeclaration(0)
    , DefinitionOrder(0)
    , OriginalPtr(0)
{
}

DEF_STRING(Declaration, Name)
DEF_STRING(Declaration, DebugText)
DEF_VECTOR(Declaration, PreprocessedEntity*, PreprocessedEntities)

DeclarationContext::DeclarationContext() : IsAnonymous(false),
    Declaration(DeclarationKind::DeclarationContext) {}

DEF_VECTOR(DeclarationContext, Namespace*, Namespaces)
DEF_VECTOR(DeclarationContext, Enumeration*, Enums)
DEF_VECTOR(DeclarationContext, Function*, Functions)
DEF_VECTOR(DeclarationContext, Class*, Classes)
DEF_VECTOR(DeclarationContext, Template*, Templates)
DEF_VECTOR(DeclarationContext, TypedefDecl*, Typedefs)
DEF_VECTOR(DeclarationContext, Variable*, Variables)

Declaration* DeclarationContext::FindAnonymous(uint64_t key)
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

Function* DeclarationContext::FindFunction(const std::string& Name, bool Create)
{
    auto foundFunction = std::find_if(Functions.begin(), Functions.end(),
            [&](Function* func) { return func->Name == Name; });

    if (foundFunction != Functions.end())
        return *foundFunction;

    if (!Create)
        return nullptr;
     
    auto function = new Function();
    function->Name = Name;
    function->_Namespace = this;
    Functions.push_back(function);

    return function;
}

FunctionTemplate*
DeclarationContext::FindFunctionTemplate(void* OriginalPtr)
{
    auto foundFunction = std::find_if(Templates.begin(), Templates.end(),
        [&](Template* func) { return func->OriginalPtr == OriginalPtr; });

    if (foundFunction != Templates.end())
        return static_cast<FunctionTemplate*>(*foundFunction);

    return nullptr;
}

FunctionTemplate*
DeclarationContext::FindFunctionTemplate(const std::string& Name,
                                         const std::vector<TemplateParameter>& Params)
{
    FunctionTemplate* func = 0;
    auto foundFunction = std::find_if(Templates.begin(), Templates.end(),
        [&](Template* func) { return func->Name == Name && func->Parameters == Params; }
    );

    if (foundFunction != Templates.end())
        return static_cast<FunctionTemplate*>(*foundFunction);

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

TypedefDecl::TypedefDecl() : Declaration(DeclarationKind::Typedef) {}

Parameter::Parameter() : Declaration(DeclarationKind::Parameter),
    IsIndirect(false), HasDefaultValue(false) {}

Function::Function() : Declaration(DeclarationKind::Function),
    IsReturnIndirect(false) {}

DEF_STRING(Function, Mangled)
DEF_STRING(Function, Signature)
DEF_VECTOR(Function, Parameter*, Parameters)

Method::Method() : IsDefaultConstructor(false), IsCopyConstructor(false),
    IsMoveConstructor(false) { Kind = DeclarationKind::Method; }

Enumeration::Enumeration() : Declaration(DeclarationKind::Enumeration) {}

DEF_VECTOR(Enumeration, Enumeration::Item, Items)

Enumeration::Item::Item() : Declaration(DeclarationKind::EnumerationItem) {}

DEF_STRING(Enumeration::Item, Expression)

Variable::Variable() : Declaration(DeclarationKind::Variable) {}

DEF_STRING(Variable, Mangled)

BaseClassSpecifier::BaseClassSpecifier() : Type(0) {}

Field::Field() : Declaration(DeclarationKind::Field), Class(0) {}

AccessSpecifierDecl::AccessSpecifierDecl()
    : Declaration(DeclarationKind::AccessSpecifier) {}

Class::Class() : Layout(0) { Kind = DeclarationKind::Class; }

DEF_VECTOR(Class, BaseClassSpecifier*, Bases)
DEF_VECTOR(Class, Field*, Fields)
DEF_VECTOR(Class, Method*, Methods)
DEF_VECTOR(Class, AccessSpecifierDecl*, Specifiers)

Template::Template() : Declaration(DeclarationKind::Template),
    TemplatedDecl(0) {}

DEF_VECTOR(Template, TemplateParameter, Parameters)

ClassTemplate::ClassTemplate() { Kind = DeclarationKind::ClassTemplate; }

DEF_VECTOR(ClassTemplate, ClassTemplateSpecialization*, Specializations)

ClassTemplateSpecialization::ClassTemplateSpecialization() : TemplatedDecl(0)
    { Kind = DeclarationKind::ClassTemplateSpecialization; }

DEF_VECTOR(ClassTemplateSpecialization, TemplateArgument, Arguments)

ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization()
    { Kind = DeclarationKind::ClassTemplatePartialSpecialization; }

FunctionTemplate::FunctionTemplate() { Kind = DeclarationKind::FunctionTemplate; }

Namespace::Namespace() : IsInline(false) { Kind = DeclarationKind::Namespace; }

PreprocessedEntity::PreprocessedEntity()
    : Declaration(DeclarationKind::PreprocessedEntity),
      Location(MacroLocation::Unknown) {}

MacroDefinition::MacroDefinition() { Kind = DeclarationKind::MacroDefinition; }

DEF_STRING(MacroDefinition, Expression)

MacroExpansion::MacroExpansion() { Kind = DeclarationKind::MacroExpansion; }

DEF_STRING(MacroExpansion, Text)

TranslationUnit::TranslationUnit() { Kind = DeclarationKind::TranslationUnit; }

DEF_STRING(TranslationUnit, FileName)
DEF_VECTOR(TranslationUnit, MacroDefinition*, Macros)

// NativeLibrary
DEF_STRING(NativeLibrary, FileName)
DEF_VECTOR_STRING(NativeLibrary, Symbols)

// ASTContext
DEF_VECTOR(ASTContext, TranslationUnit*, TranslationUnits)

ClassTemplateSpecialization* ClassTemplate::FindSpecialization(void* ptr)
{
    return 0;
}

ClassTemplateSpecialization*
ClassTemplate::FindSpecialization(TemplateSpecializationType type)
{
    return 0;
}

ClassTemplatePartialSpecialization* ClassTemplate::FindPartialSpecialization(void* ptr)
{
    return 0;
}

ClassTemplatePartialSpecialization*
ClassTemplate::FindPartialSpecialization(TemplateSpecializationType type)
{
    return 0;
}

ASTContext::ASTContext() {}

TranslationUnit* ASTContext::FindOrCreateModule(std::string File)
{
    // Clean up the file path.
    std::replace(File.begin(), File.end(), '/', '\\');

    auto existingUnit = std::find_if(TranslationUnits.begin(),
        TranslationUnits.end(), [&](TranslationUnit* unit) {
            return unit && unit->FileName == File;
    });

    if (existingUnit != TranslationUnits.end())
        return *existingUnit;

    auto unit = new TranslationUnit();
    unit->FileName = File;
    TranslationUnits.push_back(unit);

    return unit;
}

// Comments
Comment::Comment(CommentKind kind) : Kind(kind) {}

DEF_STRING(RawComment, Text)
DEF_STRING(RawComment, BriefText)

RawComment::RawComment() : FullComment(0) {}

FullComment::FullComment() : Comment(CommentKind::FullComment) {}

} } }