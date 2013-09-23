#include "AST.h"

using namespace System;
using namespace System::Runtime::InteropServices;

CppSharp::Parser::Type::Type(::CppSharp::CppParser::Type* native)
{
    NativePtr = native;
}

CppSharp::Parser::Type::Type(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::Type*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::Type::Type()
{
    NativePtr = new ::CppSharp::CppParser::Type();
}

System::IntPtr CppSharp::Parser::Type::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::Type::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::Type*)object.ToPointer();
}
CppSharp::Parser::TypeQualifiers::TypeQualifiers(::CppSharp::CppParser::TypeQualifiers* native)
{
    NativePtr = native;
}

CppSharp::Parser::TypeQualifiers::TypeQualifiers(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::TypeQualifiers*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::TypeQualifiers::TypeQualifiers()
{
    NativePtr = new ::CppSharp::CppParser::TypeQualifiers();
}

System::IntPtr CppSharp::Parser::TypeQualifiers::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::TypeQualifiers::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::TypeQualifiers*)object.ToPointer();
}

bool CppSharp::Parser::TypeQualifiers::IsConst::get()
{
    return ((::CppSharp::CppParser::TypeQualifiers*)NativePtr)->IsConst;
}

void CppSharp::Parser::TypeQualifiers::IsConst::set(bool value)
{
    ((::CppSharp::CppParser::TypeQualifiers*)NativePtr)->IsConst = value;
}

bool CppSharp::Parser::TypeQualifiers::IsVolatile::get()
{
    return ((::CppSharp::CppParser::TypeQualifiers*)NativePtr)->IsVolatile;
}

void CppSharp::Parser::TypeQualifiers::IsVolatile::set(bool value)
{
    ((::CppSharp::CppParser::TypeQualifiers*)NativePtr)->IsVolatile = value;
}

bool CppSharp::Parser::TypeQualifiers::IsRestrict::get()
{
    return ((::CppSharp::CppParser::TypeQualifiers*)NativePtr)->IsRestrict;
}

void CppSharp::Parser::TypeQualifiers::IsRestrict::set(bool value)
{
    ((::CppSharp::CppParser::TypeQualifiers*)NativePtr)->IsRestrict = value;
}

CppSharp::Parser::QualifiedType::QualifiedType(::CppSharp::CppParser::QualifiedType* native)
{
    NativePtr = native;
}

CppSharp::Parser::QualifiedType::QualifiedType(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::QualifiedType*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::QualifiedType::QualifiedType()
{
    NativePtr = new ::CppSharp::CppParser::QualifiedType();
}

System::IntPtr CppSharp::Parser::QualifiedType::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::QualifiedType::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::QualifiedType*)object.ToPointer();
}

CppSharp::Parser::Type^ CppSharp::Parser::QualifiedType::Type::get()
{
    return gcnew CppSharp::Parser::Type((::CppSharp::CppParser::Type*)((::CppSharp::CppParser::QualifiedType*)NativePtr)->Type);
}

void CppSharp::Parser::QualifiedType::Type::set(CppSharp::Parser::Type^ value)
{
    ((::CppSharp::CppParser::QualifiedType*)NativePtr)->Type = (::CppSharp::CppParser::Type*)value->NativePtr;
}

CppSharp::Parser::TypeQualifiers^ CppSharp::Parser::QualifiedType::Qualifiers::get()
{
    return gcnew CppSharp::Parser::TypeQualifiers((::CppSharp::CppParser::TypeQualifiers*)&((::CppSharp::CppParser::QualifiedType*)NativePtr)->Qualifiers);
}

void CppSharp::Parser::QualifiedType::Qualifiers::set(CppSharp::Parser::TypeQualifiers^ value)
{
    ((::CppSharp::CppParser::QualifiedType*)NativePtr)->Qualifiers = *(::CppSharp::CppParser::TypeQualifiers*)value->NativePtr;
}

CppSharp::Parser::TagType::TagType(::CppSharp::CppParser::TagType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::TagType::TagType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::TagType*)native.ToPointer();
}

CppSharp::Parser::TagType::TagType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::TagType();
}

CppSharp::Parser::Declaration^ CppSharp::Parser::TagType::Declaration::get()
{
    return gcnew CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)((::CppSharp::CppParser::TagType*)NativePtr)->Declaration);
}

void CppSharp::Parser::TagType::Declaration::set(CppSharp::Parser::Declaration^ value)
{
    ((::CppSharp::CppParser::TagType*)NativePtr)->Declaration = (::CppSharp::CppParser::Declaration*)value->NativePtr;
}

CppSharp::Parser::ArrayType::ArrayType(::CppSharp::CppParser::ArrayType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::ArrayType::ArrayType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::ArrayType*)native.ToPointer();
}

CppSharp::Parser::ArrayType::ArrayType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::ArrayType();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::ArrayType::QualifiedType::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::ArrayType*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::ArrayType::QualifiedType::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::ArrayType*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::ArrayType::ArraySize CppSharp::Parser::ArrayType::SizeType::get()
{
    return (CppSharp::Parser::ArrayType::ArraySize)((::CppSharp::CppParser::ArrayType*)NativePtr)->SizeType;
}

void CppSharp::Parser::ArrayType::SizeType::set(CppSharp::Parser::ArrayType::ArraySize value)
{
    ((::CppSharp::CppParser::ArrayType*)NativePtr)->SizeType = (::CppSharp::CppParser::ArrayType::ArraySize)value;
}

int CppSharp::Parser::ArrayType::Size::get()
{
    return ((::CppSharp::CppParser::ArrayType*)NativePtr)->Size;
}

void CppSharp::Parser::ArrayType::Size::set(int value)
{
    ((::CppSharp::CppParser::ArrayType*)NativePtr)->Size = value;
}

CppSharp::Parser::FunctionType::FunctionType(::CppSharp::CppParser::FunctionType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::FunctionType::FunctionType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::FunctionType*)native.ToPointer();
}

CppSharp::Parser::FunctionType::FunctionType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::FunctionType();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::FunctionType::ReturnType::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::FunctionType*)NativePtr)->ReturnType);
}

void CppSharp::Parser::FunctionType::ReturnType::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::FunctionType*)NativePtr)->ReturnType = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

System::Collections::Generic::List<CppSharp::Parser::Parameter^>^ CppSharp::Parser::FunctionType::Parameters::get()
{
    auto _tmpParameters = gcnew System::Collections::Generic::List<CppSharp::Parser::Parameter^>();
    for(auto _element : ((::CppSharp::CppParser::FunctionType*)NativePtr)->Parameters)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Parameter((::CppSharp::CppParser::Parameter*)_element);
        _tmpParameters->Add(_marshalElement);
    }
    return _tmpParameters;
}

void CppSharp::Parser::FunctionType::Parameters::set(System::Collections::Generic::List<CppSharp::Parser::Parameter^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Parameter*>();
    for each(CppSharp::Parser::Parameter^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Parameter*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::FunctionType*)NativePtr)->Parameters = _tmpvalue;
}

CppSharp::Parser::CallingConvention CppSharp::Parser::FunctionType::CallingConvention::get()
{
    return (CppSharp::Parser::CallingConvention)((::CppSharp::CppParser::FunctionType*)NativePtr)->CallingConvention;
}

void CppSharp::Parser::FunctionType::CallingConvention::set(CppSharp::Parser::CallingConvention value)
{
    ((::CppSharp::CppParser::FunctionType*)NativePtr)->CallingConvention = (::CppSharp::CppParser::CallingConvention)value;
}

CppSharp::Parser::PointerType::PointerType(::CppSharp::CppParser::PointerType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::PointerType::PointerType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::PointerType*)native.ToPointer();
}

CppSharp::Parser::PointerType::PointerType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::PointerType();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::PointerType::QualifiedPointee::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::PointerType*)NativePtr)->QualifiedPointee);
}

void CppSharp::Parser::PointerType::QualifiedPointee::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::PointerType*)NativePtr)->QualifiedPointee = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::PointerType::TypeModifier CppSharp::Parser::PointerType::Modifier::get()
{
    return (CppSharp::Parser::PointerType::TypeModifier)((::CppSharp::CppParser::PointerType*)NativePtr)->Modifier;
}

void CppSharp::Parser::PointerType::Modifier::set(CppSharp::Parser::PointerType::TypeModifier value)
{
    ((::CppSharp::CppParser::PointerType*)NativePtr)->Modifier = (::CppSharp::CppParser::PointerType::TypeModifier)value;
}

CppSharp::Parser::MemberPointerType::MemberPointerType(::CppSharp::CppParser::MemberPointerType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::MemberPointerType::MemberPointerType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::MemberPointerType*)native.ToPointer();
}

CppSharp::Parser::MemberPointerType::MemberPointerType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::MemberPointerType();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::MemberPointerType::Pointee::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::MemberPointerType*)NativePtr)->Pointee);
}

void CppSharp::Parser::MemberPointerType::Pointee::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::MemberPointerType*)NativePtr)->Pointee = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::TypedefType::TypedefType(::CppSharp::CppParser::TypedefType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::TypedefType::TypedefType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::TypedefType*)native.ToPointer();
}

CppSharp::Parser::TypedefType::TypedefType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::TypedefType();
}

CppSharp::Parser::TypedefDecl^ CppSharp::Parser::TypedefType::Declaration::get()
{
    return gcnew CppSharp::Parser::TypedefDecl((::CppSharp::CppParser::TypedefDecl*)((::CppSharp::CppParser::TypedefType*)NativePtr)->Declaration);
}

void CppSharp::Parser::TypedefType::Declaration::set(CppSharp::Parser::TypedefDecl^ value)
{
    ((::CppSharp::CppParser::TypedefType*)NativePtr)->Declaration = (::CppSharp::CppParser::TypedefDecl*)value->NativePtr;
}

CppSharp::Parser::DecayedType::DecayedType(::CppSharp::CppParser::DecayedType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::DecayedType::DecayedType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::DecayedType*)native.ToPointer();
}

CppSharp::Parser::DecayedType::DecayedType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::DecayedType();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::DecayedType::Decayed::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::DecayedType*)NativePtr)->Decayed);
}

void CppSharp::Parser::DecayedType::Decayed::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::DecayedType*)NativePtr)->Decayed = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::DecayedType::Original::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::DecayedType*)NativePtr)->Original);
}

void CppSharp::Parser::DecayedType::Original::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::DecayedType*)NativePtr)->Original = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::DecayedType::Pointee::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::DecayedType*)NativePtr)->Pointee);
}

void CppSharp::Parser::DecayedType::Pointee::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::DecayedType*)NativePtr)->Pointee = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::TemplateArgument::TemplateArgument(::CppSharp::CppParser::TemplateArgument* native)
{
    NativePtr = native;
}

CppSharp::Parser::TemplateArgument::TemplateArgument(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::TemplateArgument*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::TemplateArgument::TemplateArgument()
{
    NativePtr = new ::CppSharp::CppParser::TemplateArgument();
}

System::IntPtr CppSharp::Parser::TemplateArgument::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::TemplateArgument::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::TemplateArgument*)object.ToPointer();
}

CppSharp::Parser::TemplateArgument::ArgumentKind CppSharp::Parser::TemplateArgument::Kind::get()
{
    return (CppSharp::Parser::TemplateArgument::ArgumentKind)((::CppSharp::CppParser::TemplateArgument*)NativePtr)->Kind;
}

void CppSharp::Parser::TemplateArgument::Kind::set(CppSharp::Parser::TemplateArgument::ArgumentKind value)
{
    ((::CppSharp::CppParser::TemplateArgument*)NativePtr)->Kind = (::CppSharp::CppParser::TemplateArgument::ArgumentKind)value;
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::TemplateArgument::Type::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::TemplateArgument*)NativePtr)->Type);
}

void CppSharp::Parser::TemplateArgument::Type::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::TemplateArgument*)NativePtr)->Type = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::Declaration^ CppSharp::Parser::TemplateArgument::Declaration::get()
{
    return gcnew CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)((::CppSharp::CppParser::TemplateArgument*)NativePtr)->Declaration);
}

void CppSharp::Parser::TemplateArgument::Declaration::set(CppSharp::Parser::Declaration^ value)
{
    ((::CppSharp::CppParser::TemplateArgument*)NativePtr)->Declaration = (::CppSharp::CppParser::Declaration*)value->NativePtr;
}

int CppSharp::Parser::TemplateArgument::Integral::get()
{
    return ((::CppSharp::CppParser::TemplateArgument*)NativePtr)->Integral;
}

void CppSharp::Parser::TemplateArgument::Integral::set(int value)
{
    ((::CppSharp::CppParser::TemplateArgument*)NativePtr)->Integral = value;
}

CppSharp::Parser::TemplateSpecializationType::TemplateSpecializationType(::CppSharp::CppParser::TemplateSpecializationType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::TemplateSpecializationType::TemplateSpecializationType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::TemplateSpecializationType*)native.ToPointer();
}

CppSharp::Parser::TemplateSpecializationType::TemplateSpecializationType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::TemplateSpecializationType();
}

System::Collections::Generic::List<CppSharp::Parser::TemplateArgument^>^ CppSharp::Parser::TemplateSpecializationType::Arguments::get()
{
    auto _tmpArguments = gcnew System::Collections::Generic::List<CppSharp::Parser::TemplateArgument^>();
    for(auto _element : ((::CppSharp::CppParser::TemplateSpecializationType*)NativePtr)->Arguments)
    {
        auto ___element = new ::CppSharp::CppParser::TemplateArgument(_element);
        auto _marshalElement = gcnew CppSharp::Parser::TemplateArgument((::CppSharp::CppParser::TemplateArgument*)___element);
        _tmpArguments->Add(_marshalElement);
    }
    return _tmpArguments;
}

void CppSharp::Parser::TemplateSpecializationType::Arguments::set(System::Collections::Generic::List<CppSharp::Parser::TemplateArgument^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::TemplateArgument>();
    for each(CppSharp::Parser::TemplateArgument^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::TemplateArgument*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::TemplateSpecializationType*)NativePtr)->Arguments = _tmpvalue;
}

CppSharp::Parser::Template^ CppSharp::Parser::TemplateSpecializationType::Template::get()
{
    return gcnew CppSharp::Parser::Template((::CppSharp::CppParser::Template*)((::CppSharp::CppParser::TemplateSpecializationType*)NativePtr)->Template);
}

void CppSharp::Parser::TemplateSpecializationType::Template::set(CppSharp::Parser::Template^ value)
{
    ((::CppSharp::CppParser::TemplateSpecializationType*)NativePtr)->Template = (::CppSharp::CppParser::Template*)value->NativePtr;
}

CppSharp::Parser::Type^ CppSharp::Parser::TemplateSpecializationType::Desugared::get()
{
    return gcnew CppSharp::Parser::Type((::CppSharp::CppParser::Type*)((::CppSharp::CppParser::TemplateSpecializationType*)NativePtr)->Desugared);
}

void CppSharp::Parser::TemplateSpecializationType::Desugared::set(CppSharp::Parser::Type^ value)
{
    ((::CppSharp::CppParser::TemplateSpecializationType*)NativePtr)->Desugared = (::CppSharp::CppParser::Type*)value->NativePtr;
}

CppSharp::Parser::TemplateParameter::TemplateParameter(::CppSharp::CppParser::TemplateParameter* native)
{
    NativePtr = native;
}

CppSharp::Parser::TemplateParameter::TemplateParameter(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::TemplateParameter*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::TemplateParameter::TemplateParameter()
{
    NativePtr = new ::CppSharp::CppParser::TemplateParameter();
}

System::IntPtr CppSharp::Parser::TemplateParameter::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::TemplateParameter::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::TemplateParameter*)object.ToPointer();
}

System::String^ CppSharp::Parser::TemplateParameter::Name::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::TemplateParameter*)NativePtr)->Name);
}

void CppSharp::Parser::TemplateParameter::Name::set(System::String^ value)
{
    ((::CppSharp::CppParser::TemplateParameter*)NativePtr)->Name = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::TemplateParameterType::TemplateParameterType(::CppSharp::CppParser::TemplateParameterType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::TemplateParameterType::TemplateParameterType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::TemplateParameterType*)native.ToPointer();
}

CppSharp::Parser::TemplateParameterType::TemplateParameterType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::TemplateParameterType();
}

CppSharp::Parser::TemplateParameter^ CppSharp::Parser::TemplateParameterType::Parameter::get()
{
    return gcnew CppSharp::Parser::TemplateParameter((::CppSharp::CppParser::TemplateParameter*)&((::CppSharp::CppParser::TemplateParameterType*)NativePtr)->Parameter);
}

void CppSharp::Parser::TemplateParameterType::Parameter::set(CppSharp::Parser::TemplateParameter^ value)
{
    ((::CppSharp::CppParser::TemplateParameterType*)NativePtr)->Parameter = *(::CppSharp::CppParser::TemplateParameter*)value->NativePtr;
}

CppSharp::Parser::TemplateParameterSubstitutionType::TemplateParameterSubstitutionType(::CppSharp::CppParser::TemplateParameterSubstitutionType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::TemplateParameterSubstitutionType::TemplateParameterSubstitutionType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::TemplateParameterSubstitutionType*)native.ToPointer();
}

CppSharp::Parser::TemplateParameterSubstitutionType::TemplateParameterSubstitutionType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::TemplateParameterSubstitutionType();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::TemplateParameterSubstitutionType::Replacement::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::TemplateParameterSubstitutionType*)NativePtr)->Replacement);
}

void CppSharp::Parser::TemplateParameterSubstitutionType::Replacement::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::TemplateParameterSubstitutionType*)NativePtr)->Replacement = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::InjectedClassNameType::InjectedClassNameType(::CppSharp::CppParser::InjectedClassNameType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::InjectedClassNameType::InjectedClassNameType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::InjectedClassNameType*)native.ToPointer();
}

CppSharp::Parser::InjectedClassNameType::InjectedClassNameType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::InjectedClassNameType();
}

CppSharp::Parser::TemplateSpecializationType^ CppSharp::Parser::InjectedClassNameType::TemplateSpecialization::get()
{
    return gcnew CppSharp::Parser::TemplateSpecializationType((::CppSharp::CppParser::TemplateSpecializationType*)&((::CppSharp::CppParser::InjectedClassNameType*)NativePtr)->TemplateSpecialization);
}

void CppSharp::Parser::InjectedClassNameType::TemplateSpecialization::set(CppSharp::Parser::TemplateSpecializationType^ value)
{
    ((::CppSharp::CppParser::InjectedClassNameType*)NativePtr)->TemplateSpecialization = *(::CppSharp::CppParser::TemplateSpecializationType*)value->NativePtr;
}

CppSharp::Parser::Class^ CppSharp::Parser::InjectedClassNameType::Class::get()
{
    return gcnew CppSharp::Parser::Class((::CppSharp::CppParser::Class*)((::CppSharp::CppParser::InjectedClassNameType*)NativePtr)->Class);
}

void CppSharp::Parser::InjectedClassNameType::Class::set(CppSharp::Parser::Class^ value)
{
    ((::CppSharp::CppParser::InjectedClassNameType*)NativePtr)->Class = (::CppSharp::CppParser::Class*)value->NativePtr;
}

CppSharp::Parser::DependentNameType::DependentNameType(::CppSharp::CppParser::DependentNameType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::DependentNameType::DependentNameType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::DependentNameType*)native.ToPointer();
}

CppSharp::Parser::DependentNameType::DependentNameType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::DependentNameType();
}

CppSharp::Parser::BuiltinType::BuiltinType(::CppSharp::CppParser::BuiltinType* native)
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)native)
{
}

CppSharp::Parser::BuiltinType::BuiltinType(System::IntPtr native)
    : CppSharp::Parser::Type(native)
{
    auto __native = (::CppSharp::CppParser::BuiltinType*)native.ToPointer();
}

CppSharp::Parser::BuiltinType::BuiltinType()
    : CppSharp::Parser::Type((::CppSharp::CppParser::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::BuiltinType();
}

CppSharp::Parser::PrimitiveType CppSharp::Parser::BuiltinType::Type::get()
{
    return (CppSharp::Parser::PrimitiveType)((::CppSharp::CppParser::BuiltinType*)NativePtr)->Type;
}

void CppSharp::Parser::BuiltinType::Type::set(CppSharp::Parser::PrimitiveType value)
{
    ((::CppSharp::CppParser::BuiltinType*)NativePtr)->Type = (::CppSharp::CppParser::PrimitiveType)value;
}

CppSharp::Parser::RawComment::RawComment(::CppSharp::CppParser::RawComment* native)
{
    NativePtr = native;
}

CppSharp::Parser::RawComment::RawComment(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::RawComment*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::RawComment::RawComment()
{
    NativePtr = new ::CppSharp::CppParser::RawComment();
}

System::IntPtr CppSharp::Parser::RawComment::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::RawComment::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::RawComment*)object.ToPointer();
}

CppSharp::Parser::RawCommentKind CppSharp::Parser::RawComment::Kind::get()
{
    return (CppSharp::Parser::RawCommentKind)((::CppSharp::CppParser::RawComment*)NativePtr)->Kind;
}

void CppSharp::Parser::RawComment::Kind::set(CppSharp::Parser::RawCommentKind value)
{
    ((::CppSharp::CppParser::RawComment*)NativePtr)->Kind = (::CppSharp::CppParser::RawCommentKind)value;
}

System::String^ CppSharp::Parser::RawComment::Text::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::RawComment*)NativePtr)->Text);
}

void CppSharp::Parser::RawComment::Text::set(System::String^ value)
{
    ((::CppSharp::CppParser::RawComment*)NativePtr)->Text = clix::marshalString<clix::E_UTF8>(value);
}

System::String^ CppSharp::Parser::RawComment::BriefText::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::RawComment*)NativePtr)->BriefText);
}

void CppSharp::Parser::RawComment::BriefText::set(System::String^ value)
{
    ((::CppSharp::CppParser::RawComment*)NativePtr)->BriefText = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::VTableComponent::VTableComponent(::CppSharp::CppParser::VTableComponent* native)
{
    NativePtr = native;
}

CppSharp::Parser::VTableComponent::VTableComponent(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::VTableComponent*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::VTableComponent::VTableComponent()
{
    NativePtr = new ::CppSharp::CppParser::VTableComponent();
}

System::IntPtr CppSharp::Parser::VTableComponent::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::VTableComponent::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::VTableComponent*)object.ToPointer();
}

CppSharp::Parser::VTableComponentKind CppSharp::Parser::VTableComponent::Kind::get()
{
    return (CppSharp::Parser::VTableComponentKind)((::CppSharp::CppParser::VTableComponent*)NativePtr)->Kind;
}

void CppSharp::Parser::VTableComponent::Kind::set(CppSharp::Parser::VTableComponentKind value)
{
    ((::CppSharp::CppParser::VTableComponent*)NativePtr)->Kind = (::CppSharp::CppParser::VTableComponentKind)value;
}

unsigned int CppSharp::Parser::VTableComponent::Offset::get()
{
    return ((::CppSharp::CppParser::VTableComponent*)NativePtr)->Offset;
}

void CppSharp::Parser::VTableComponent::Offset::set(unsigned int value)
{
    ((::CppSharp::CppParser::VTableComponent*)NativePtr)->Offset = value;
}

CppSharp::Parser::Declaration^ CppSharp::Parser::VTableComponent::Declaration::get()
{
    return gcnew CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)((::CppSharp::CppParser::VTableComponent*)NativePtr)->Declaration);
}

void CppSharp::Parser::VTableComponent::Declaration::set(CppSharp::Parser::Declaration^ value)
{
    ((::CppSharp::CppParser::VTableComponent*)NativePtr)->Declaration = (::CppSharp::CppParser::Declaration*)value->NativePtr;
}

CppSharp::Parser::VTableLayout::VTableLayout(::CppSharp::CppParser::VTableLayout* native)
{
    NativePtr = native;
}

CppSharp::Parser::VTableLayout::VTableLayout(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::VTableLayout*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::VTableLayout::VTableLayout()
{
    NativePtr = new ::CppSharp::CppParser::VTableLayout();
}

System::IntPtr CppSharp::Parser::VTableLayout::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::VTableLayout::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::VTableLayout*)object.ToPointer();
}

System::Collections::Generic::List<CppSharp::Parser::VTableComponent^>^ CppSharp::Parser::VTableLayout::Components::get()
{
    auto _tmpComponents = gcnew System::Collections::Generic::List<CppSharp::Parser::VTableComponent^>();
    for(auto _element : ((::CppSharp::CppParser::VTableLayout*)NativePtr)->Components)
    {
        auto ___element = new ::CppSharp::CppParser::VTableComponent(_element);
        auto _marshalElement = gcnew CppSharp::Parser::VTableComponent((::CppSharp::CppParser::VTableComponent*)___element);
        _tmpComponents->Add(_marshalElement);
    }
    return _tmpComponents;
}

void CppSharp::Parser::VTableLayout::Components::set(System::Collections::Generic::List<CppSharp::Parser::VTableComponent^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::VTableComponent>();
    for each(CppSharp::Parser::VTableComponent^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::VTableComponent*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::VTableLayout*)NativePtr)->Components = _tmpvalue;
}

CppSharp::Parser::VFTableInfo::VFTableInfo(::CppSharp::CppParser::VFTableInfo* native)
{
    NativePtr = native;
}

CppSharp::Parser::VFTableInfo::VFTableInfo(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::VFTableInfo*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::VFTableInfo::VFTableInfo()
{
    NativePtr = new ::CppSharp::CppParser::VFTableInfo();
}

System::IntPtr CppSharp::Parser::VFTableInfo::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::VFTableInfo::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::VFTableInfo*)object.ToPointer();
}

unsigned long long CppSharp::Parser::VFTableInfo::VBTableIndex::get()
{
    return ((::CppSharp::CppParser::VFTableInfo*)NativePtr)->VBTableIndex;
}

void CppSharp::Parser::VFTableInfo::VBTableIndex::set(unsigned long long value)
{
    ((::CppSharp::CppParser::VFTableInfo*)NativePtr)->VBTableIndex = (::uint64_t)value;
}

unsigned int CppSharp::Parser::VFTableInfo::VFPtrOffset::get()
{
    return ((::CppSharp::CppParser::VFTableInfo*)NativePtr)->VFPtrOffset;
}

void CppSharp::Parser::VFTableInfo::VFPtrOffset::set(unsigned int value)
{
    ((::CppSharp::CppParser::VFTableInfo*)NativePtr)->VFPtrOffset = (::uint32_t)value;
}

unsigned int CppSharp::Parser::VFTableInfo::VFPtrFullOffset::get()
{
    return ((::CppSharp::CppParser::VFTableInfo*)NativePtr)->VFPtrFullOffset;
}

void CppSharp::Parser::VFTableInfo::VFPtrFullOffset::set(unsigned int value)
{
    ((::CppSharp::CppParser::VFTableInfo*)NativePtr)->VFPtrFullOffset = (::uint32_t)value;
}

CppSharp::Parser::VTableLayout^ CppSharp::Parser::VFTableInfo::Layout::get()
{
    return gcnew CppSharp::Parser::VTableLayout((::CppSharp::CppParser::VTableLayout*)&((::CppSharp::CppParser::VFTableInfo*)NativePtr)->Layout);
}

void CppSharp::Parser::VFTableInfo::Layout::set(CppSharp::Parser::VTableLayout^ value)
{
    ((::CppSharp::CppParser::VFTableInfo*)NativePtr)->Layout = *(::CppSharp::CppParser::VTableLayout*)value->NativePtr;
}

CppSharp::Parser::ClassLayout::ClassLayout(::CppSharp::CppParser::ClassLayout* native)
{
    NativePtr = native;
}

CppSharp::Parser::ClassLayout::ClassLayout(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::ClassLayout*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::ClassLayout::ClassLayout()
{
    NativePtr = new ::CppSharp::CppParser::ClassLayout();
}

System::IntPtr CppSharp::Parser::ClassLayout::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ClassLayout::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ClassLayout*)object.ToPointer();
}

CppSharp::Parser::CppAbi CppSharp::Parser::ClassLayout::ABI::get()
{
    return (CppSharp::Parser::CppAbi)((::CppSharp::CppParser::ClassLayout*)NativePtr)->ABI;
}

void CppSharp::Parser::ClassLayout::ABI::set(CppSharp::Parser::CppAbi value)
{
    ((::CppSharp::CppParser::ClassLayout*)NativePtr)->ABI = (::CppSharp::CppParser::CppAbi)value;
}

System::Collections::Generic::List<CppSharp::Parser::VFTableInfo^>^ CppSharp::Parser::ClassLayout::VFTables::get()
{
    auto _tmpVFTables = gcnew System::Collections::Generic::List<CppSharp::Parser::VFTableInfo^>();
    for(auto _element : ((::CppSharp::CppParser::ClassLayout*)NativePtr)->VFTables)
    {
        auto ___element = new ::CppSharp::CppParser::VFTableInfo(_element);
        auto _marshalElement = gcnew CppSharp::Parser::VFTableInfo((::CppSharp::CppParser::VFTableInfo*)___element);
        _tmpVFTables->Add(_marshalElement);
    }
    return _tmpVFTables;
}

void CppSharp::Parser::ClassLayout::VFTables::set(System::Collections::Generic::List<CppSharp::Parser::VFTableInfo^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::VFTableInfo>();
    for each(CppSharp::Parser::VFTableInfo^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::VFTableInfo*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::ClassLayout*)NativePtr)->VFTables = _tmpvalue;
}

CppSharp::Parser::VTableLayout^ CppSharp::Parser::ClassLayout::Layout::get()
{
    return gcnew CppSharp::Parser::VTableLayout((::CppSharp::CppParser::VTableLayout*)&((::CppSharp::CppParser::ClassLayout*)NativePtr)->Layout);
}

void CppSharp::Parser::ClassLayout::Layout::set(CppSharp::Parser::VTableLayout^ value)
{
    ((::CppSharp::CppParser::ClassLayout*)NativePtr)->Layout = *(::CppSharp::CppParser::VTableLayout*)value->NativePtr;
}

bool CppSharp::Parser::ClassLayout::HasOwnVFPtr::get()
{
    return ((::CppSharp::CppParser::ClassLayout*)NativePtr)->HasOwnVFPtr;
}

void CppSharp::Parser::ClassLayout::HasOwnVFPtr::set(bool value)
{
    ((::CppSharp::CppParser::ClassLayout*)NativePtr)->HasOwnVFPtr = value;
}

int CppSharp::Parser::ClassLayout::VBPtrOffset::get()
{
    return ((::CppSharp::CppParser::ClassLayout*)NativePtr)->VBPtrOffset;
}

void CppSharp::Parser::ClassLayout::VBPtrOffset::set(int value)
{
    ((::CppSharp::CppParser::ClassLayout*)NativePtr)->VBPtrOffset = value;
}

int CppSharp::Parser::ClassLayout::Alignment::get()
{
    return ((::CppSharp::CppParser::ClassLayout*)NativePtr)->Alignment;
}

void CppSharp::Parser::ClassLayout::Alignment::set(int value)
{
    ((::CppSharp::CppParser::ClassLayout*)NativePtr)->Alignment = value;
}

int CppSharp::Parser::ClassLayout::Size::get()
{
    return ((::CppSharp::CppParser::ClassLayout*)NativePtr)->Size;
}

void CppSharp::Parser::ClassLayout::Size::set(int value)
{
    ((::CppSharp::CppParser::ClassLayout*)NativePtr)->Size = value;
}

int CppSharp::Parser::ClassLayout::DataSize::get()
{
    return ((::CppSharp::CppParser::ClassLayout*)NativePtr)->DataSize;
}

void CppSharp::Parser::ClassLayout::DataSize::set(int value)
{
    ((::CppSharp::CppParser::ClassLayout*)NativePtr)->DataSize = value;
}

CppSharp::Parser::Declaration::Declaration(::CppSharp::CppParser::Declaration* native)
{
    NativePtr = native;
}

CppSharp::Parser::Declaration::Declaration(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::Declaration*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::Declaration::Declaration()
{
    NativePtr = new ::CppSharp::CppParser::Declaration();
}

System::IntPtr CppSharp::Parser::Declaration::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::Declaration::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::Declaration*)object.ToPointer();
}

CppSharp::Parser::AccessSpecifier CppSharp::Parser::Declaration::Access::get()
{
    return (CppSharp::Parser::AccessSpecifier)((::CppSharp::CppParser::Declaration*)NativePtr)->Access;
}

void CppSharp::Parser::Declaration::Access::set(CppSharp::Parser::AccessSpecifier value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->Access = (::CppSharp::CppParser::AccessSpecifier)value;
}

CppSharp::Parser::DeclarationContext^ CppSharp::Parser::Declaration::_Namespace::get()
{
    return gcnew CppSharp::Parser::DeclarationContext((::CppSharp::CppParser::DeclarationContext*)((::CppSharp::CppParser::Declaration*)NativePtr)->_Namespace);
}

void CppSharp::Parser::Declaration::_Namespace::set(CppSharp::Parser::DeclarationContext^ value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->_Namespace = (::CppSharp::CppParser::DeclarationContext*)value->NativePtr;
}

System::String^ CppSharp::Parser::Declaration::Name::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::Declaration*)NativePtr)->Name);
}

void CppSharp::Parser::Declaration::Name::set(System::String^ value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->Name = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::RawComment^ CppSharp::Parser::Declaration::Comment::get()
{
    return gcnew CppSharp::Parser::RawComment((::CppSharp::CppParser::RawComment*)((::CppSharp::CppParser::Declaration*)NativePtr)->Comment);
}

void CppSharp::Parser::Declaration::Comment::set(CppSharp::Parser::RawComment^ value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->Comment = (::CppSharp::CppParser::RawComment*)value->NativePtr;
}

System::String^ CppSharp::Parser::Declaration::DebugText::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::Declaration*)NativePtr)->DebugText);
}

void CppSharp::Parser::Declaration::DebugText::set(System::String^ value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->DebugText = clix::marshalString<clix::E_UTF8>(value);
}

bool CppSharp::Parser::Declaration::IsIncomplete::get()
{
    return ((::CppSharp::CppParser::Declaration*)NativePtr)->IsIncomplete;
}

void CppSharp::Parser::Declaration::IsIncomplete::set(bool value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->IsIncomplete = value;
}

bool CppSharp::Parser::Declaration::IsDependent::get()
{
    return ((::CppSharp::CppParser::Declaration*)NativePtr)->IsDependent;
}

void CppSharp::Parser::Declaration::IsDependent::set(bool value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->IsDependent = value;
}

CppSharp::Parser::Declaration^ CppSharp::Parser::Declaration::CompleteDeclaration::get()
{
    return gcnew CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)((::CppSharp::CppParser::Declaration*)NativePtr)->CompleteDeclaration);
}

void CppSharp::Parser::Declaration::CompleteDeclaration::set(CppSharp::Parser::Declaration^ value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->CompleteDeclaration = (::CppSharp::CppParser::Declaration*)value->NativePtr;
}

unsigned int CppSharp::Parser::Declaration::DefinitionOrder::get()
{
    return ((::CppSharp::CppParser::Declaration*)NativePtr)->DefinitionOrder;
}

void CppSharp::Parser::Declaration::DefinitionOrder::set(unsigned int value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->DefinitionOrder = value;
}

System::Collections::Generic::List<CppSharp::Parser::PreprocessedEntity^>^ CppSharp::Parser::Declaration::PreprocessedEntities::get()
{
    auto _tmpPreprocessedEntities = gcnew System::Collections::Generic::List<CppSharp::Parser::PreprocessedEntity^>();
    for(auto _element : ((::CppSharp::CppParser::Declaration*)NativePtr)->PreprocessedEntities)
    {
        auto _marshalElement = gcnew CppSharp::Parser::PreprocessedEntity((::CppSharp::CppParser::PreprocessedEntity*)_element);
        _tmpPreprocessedEntities->Add(_marshalElement);
    }
    return _tmpPreprocessedEntities;
}

void CppSharp::Parser::Declaration::PreprocessedEntities::set(System::Collections::Generic::List<CppSharp::Parser::PreprocessedEntity^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::PreprocessedEntity*>();
    for each(CppSharp::Parser::PreprocessedEntity^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::PreprocessedEntity*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Declaration*)NativePtr)->PreprocessedEntities = _tmpvalue;
}

System::IntPtr CppSharp::Parser::Declaration::OriginalPtr::get()
{
    return IntPtr(((::CppSharp::CppParser::Declaration*)NativePtr)->OriginalPtr);
}

void CppSharp::Parser::Declaration::OriginalPtr::set(System::IntPtr value)
{
    ((::CppSharp::CppParser::Declaration*)NativePtr)->OriginalPtr = (void*)value.ToPointer();
}

CppSharp::Parser::DeclarationContext::DeclarationContext(::CppSharp::CppParser::DeclarationContext* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::DeclarationContext::DeclarationContext(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::DeclarationContext*)native.ToPointer();
}

CppSharp::Parser::Declaration^ CppSharp::Parser::DeclarationContext::FindAnonymous(unsigned long long key)
{
    auto arg0 = (::uint64_t)key;
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindAnonymous(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)__ret);
}

CppSharp::Parser::Namespace^ CppSharp::Parser::DeclarationContext::FindNamespace(System::String^ Name)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindNamespace(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Namespace((::CppSharp::CppParser::Namespace*)__ret);
}

CppSharp::Parser::Namespace^ CppSharp::Parser::DeclarationContext::FindNamespace(System::Collections::Generic::List<System::String^>^ _0)
{
    auto _tmp_0 = std::vector<::std::string>();
    for each(System::String^ _element in _0)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmp_0.push_back(_marshalElement);
    }
    auto arg0 = _tmp_0;
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindNamespace(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Namespace((::CppSharp::CppParser::Namespace*)__ret);
}

CppSharp::Parser::Namespace^ CppSharp::Parser::DeclarationContext::FindCreateNamespace(System::String^ Name)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindCreateNamespace(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Namespace((::CppSharp::CppParser::Namespace*)__ret);
}

CppSharp::Parser::Class^ CppSharp::Parser::DeclarationContext::CreateClass(System::String^ Name, bool IsComplete)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->CreateClass(arg0, IsComplete);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Class((::CppSharp::CppParser::Class*)__ret);
}

CppSharp::Parser::Class^ CppSharp::Parser::DeclarationContext::FindClass(System::String^ Name)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindClass(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Class((::CppSharp::CppParser::Class*)__ret);
}

CppSharp::Parser::Class^ CppSharp::Parser::DeclarationContext::FindClass(System::String^ Name, bool IsComplete, bool Create)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindClass(arg0, IsComplete, Create);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Class((::CppSharp::CppParser::Class*)__ret);
}

CppSharp::Parser::Enumeration^ CppSharp::Parser::DeclarationContext::FindEnum(System::String^ Name, bool Create)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindEnum(arg0, Create);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Enumeration((::CppSharp::CppParser::Enumeration*)__ret);
}

CppSharp::Parser::Function^ CppSharp::Parser::DeclarationContext::FindFunction(System::String^ Name, bool Create)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindFunction(arg0, Create);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::Function((::CppSharp::CppParser::Function*)__ret);
}

CppSharp::Parser::TypedefDecl^ CppSharp::Parser::DeclarationContext::FindTypedef(System::String^ Name, bool Create)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->FindTypedef(arg0, Create);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::TypedefDecl((::CppSharp::CppParser::TypedefDecl*)__ret);
}

CppSharp::Parser::DeclarationContext::DeclarationContext()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::DeclarationContext();
}

System::Collections::Generic::List<CppSharp::Parser::Namespace^>^ CppSharp::Parser::DeclarationContext::Namespaces::get()
{
    auto _tmpNamespaces = gcnew System::Collections::Generic::List<CppSharp::Parser::Namespace^>();
    for(auto _element : ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Namespaces)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Namespace((::CppSharp::CppParser::Namespace*)_element);
        _tmpNamespaces->Add(_marshalElement);
    }
    return _tmpNamespaces;
}

void CppSharp::Parser::DeclarationContext::Namespaces::set(System::Collections::Generic::List<CppSharp::Parser::Namespace^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Namespace*>();
    for each(CppSharp::Parser::Namespace^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Namespace*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Namespaces = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::Enumeration^>^ CppSharp::Parser::DeclarationContext::Enums::get()
{
    auto _tmpEnums = gcnew System::Collections::Generic::List<CppSharp::Parser::Enumeration^>();
    for(auto _element : ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Enums)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Enumeration((::CppSharp::CppParser::Enumeration*)_element);
        _tmpEnums->Add(_marshalElement);
    }
    return _tmpEnums;
}

void CppSharp::Parser::DeclarationContext::Enums::set(System::Collections::Generic::List<CppSharp::Parser::Enumeration^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Enumeration*>();
    for each(CppSharp::Parser::Enumeration^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Enumeration*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Enums = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::Function^>^ CppSharp::Parser::DeclarationContext::Functions::get()
{
    auto _tmpFunctions = gcnew System::Collections::Generic::List<CppSharp::Parser::Function^>();
    for(auto _element : ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Functions)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Function((::CppSharp::CppParser::Function*)_element);
        _tmpFunctions->Add(_marshalElement);
    }
    return _tmpFunctions;
}

void CppSharp::Parser::DeclarationContext::Functions::set(System::Collections::Generic::List<CppSharp::Parser::Function^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Function*>();
    for each(CppSharp::Parser::Function^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Function*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Functions = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::Class^>^ CppSharp::Parser::DeclarationContext::Classes::get()
{
    auto _tmpClasses = gcnew System::Collections::Generic::List<CppSharp::Parser::Class^>();
    for(auto _element : ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Classes)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Class((::CppSharp::CppParser::Class*)_element);
        _tmpClasses->Add(_marshalElement);
    }
    return _tmpClasses;
}

void CppSharp::Parser::DeclarationContext::Classes::set(System::Collections::Generic::List<CppSharp::Parser::Class^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Class*>();
    for each(CppSharp::Parser::Class^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Class*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Classes = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::Template^>^ CppSharp::Parser::DeclarationContext::Templates::get()
{
    auto _tmpTemplates = gcnew System::Collections::Generic::List<CppSharp::Parser::Template^>();
    for(auto _element : ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Templates)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Template((::CppSharp::CppParser::Template*)_element);
        _tmpTemplates->Add(_marshalElement);
    }
    return _tmpTemplates;
}

void CppSharp::Parser::DeclarationContext::Templates::set(System::Collections::Generic::List<CppSharp::Parser::Template^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Template*>();
    for each(CppSharp::Parser::Template^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Template*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Templates = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::TypedefDecl^>^ CppSharp::Parser::DeclarationContext::Typedefs::get()
{
    auto _tmpTypedefs = gcnew System::Collections::Generic::List<CppSharp::Parser::TypedefDecl^>();
    for(auto _element : ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Typedefs)
    {
        auto _marshalElement = gcnew CppSharp::Parser::TypedefDecl((::CppSharp::CppParser::TypedefDecl*)_element);
        _tmpTypedefs->Add(_marshalElement);
    }
    return _tmpTypedefs;
}

void CppSharp::Parser::DeclarationContext::Typedefs::set(System::Collections::Generic::List<CppSharp::Parser::TypedefDecl^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::TypedefDecl*>();
    for each(CppSharp::Parser::TypedefDecl^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::TypedefDecl*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Typedefs = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::Variable^>^ CppSharp::Parser::DeclarationContext::Variables::get()
{
    auto _tmpVariables = gcnew System::Collections::Generic::List<CppSharp::Parser::Variable^>();
    for(auto _element : ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Variables)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Variable((::CppSharp::CppParser::Variable*)_element);
        _tmpVariables->Add(_marshalElement);
    }
    return _tmpVariables;
}

void CppSharp::Parser::DeclarationContext::Variables::set(System::Collections::Generic::List<CppSharp::Parser::Variable^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Variable*>();
    for each(CppSharp::Parser::Variable^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Variable*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::DeclarationContext*)NativePtr)->Variables = _tmpvalue;
}

CppSharp::Parser::TypedefDecl::TypedefDecl(::CppSharp::CppParser::TypedefDecl* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::TypedefDecl::TypedefDecl(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::TypedefDecl*)native.ToPointer();
}

CppSharp::Parser::TypedefDecl::TypedefDecl()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::TypedefDecl();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::TypedefDecl::QualifiedType::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::TypedefDecl*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::TypedefDecl::QualifiedType::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::TypedefDecl*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::Parameter::Parameter(::CppSharp::CppParser::Parameter* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::Parameter::Parameter(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::Parameter*)native.ToPointer();
}

CppSharp::Parser::Parameter::Parameter()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Parameter();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::Parameter::QualifiedType::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::Parameter*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::Parameter::QualifiedType::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::Parameter*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

bool CppSharp::Parser::Parameter::IsIndirect::get()
{
    return ((::CppSharp::CppParser::Parameter*)NativePtr)->IsIndirect;
}

void CppSharp::Parser::Parameter::IsIndirect::set(bool value)
{
    ((::CppSharp::CppParser::Parameter*)NativePtr)->IsIndirect = value;
}

bool CppSharp::Parser::Parameter::HasDefaultValue::get()
{
    return ((::CppSharp::CppParser::Parameter*)NativePtr)->HasDefaultValue;
}

void CppSharp::Parser::Parameter::HasDefaultValue::set(bool value)
{
    ((::CppSharp::CppParser::Parameter*)NativePtr)->HasDefaultValue = value;
}

CppSharp::Parser::Function::Function(::CppSharp::CppParser::Function* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::Function::Function(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::Function*)native.ToPointer();
}

CppSharp::Parser::Function::Function()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Function();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::Function::ReturnType::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::Function*)NativePtr)->ReturnType);
}

void CppSharp::Parser::Function::ReturnType::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::Function*)NativePtr)->ReturnType = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

bool CppSharp::Parser::Function::IsReturnIndirect::get()
{
    return ((::CppSharp::CppParser::Function*)NativePtr)->IsReturnIndirect;
}

void CppSharp::Parser::Function::IsReturnIndirect::set(bool value)
{
    ((::CppSharp::CppParser::Function*)NativePtr)->IsReturnIndirect = value;
}

bool CppSharp::Parser::Function::IsVariadic::get()
{
    return ((::CppSharp::CppParser::Function*)NativePtr)->IsVariadic;
}

void CppSharp::Parser::Function::IsVariadic::set(bool value)
{
    ((::CppSharp::CppParser::Function*)NativePtr)->IsVariadic = value;
}

bool CppSharp::Parser::Function::IsInline::get()
{
    return ((::CppSharp::CppParser::Function*)NativePtr)->IsInline;
}

void CppSharp::Parser::Function::IsInline::set(bool value)
{
    ((::CppSharp::CppParser::Function*)NativePtr)->IsInline = value;
}

bool CppSharp::Parser::Function::IsPure::get()
{
    return ((::CppSharp::CppParser::Function*)NativePtr)->IsPure;
}

void CppSharp::Parser::Function::IsPure::set(bool value)
{
    ((::CppSharp::CppParser::Function*)NativePtr)->IsPure = value;
}

CppSharp::Parser::CXXOperatorKind CppSharp::Parser::Function::OperatorKind::get()
{
    return (CppSharp::Parser::CXXOperatorKind)((::CppSharp::CppParser::Function*)NativePtr)->OperatorKind;
}

void CppSharp::Parser::Function::OperatorKind::set(CppSharp::Parser::CXXOperatorKind value)
{
    ((::CppSharp::CppParser::Function*)NativePtr)->OperatorKind = (::CppSharp::CppParser::CXXOperatorKind)value;
}

System::String^ CppSharp::Parser::Function::Mangled::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::Function*)NativePtr)->Mangled);
}

void CppSharp::Parser::Function::Mangled::set(System::String^ value)
{
    ((::CppSharp::CppParser::Function*)NativePtr)->Mangled = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::CallingConvention CppSharp::Parser::Function::CallingConvention::get()
{
    return (CppSharp::Parser::CallingConvention)((::CppSharp::CppParser::Function*)NativePtr)->CallingConvention;
}

void CppSharp::Parser::Function::CallingConvention::set(CppSharp::Parser::CallingConvention value)
{
    ((::CppSharp::CppParser::Function*)NativePtr)->CallingConvention = (::CppSharp::CppParser::CallingConvention)value;
}

System::Collections::Generic::List<CppSharp::Parser::Parameter^>^ CppSharp::Parser::Function::Parameters::get()
{
    auto _tmpParameters = gcnew System::Collections::Generic::List<CppSharp::Parser::Parameter^>();
    for(auto _element : ((::CppSharp::CppParser::Function*)NativePtr)->Parameters)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Parameter((::CppSharp::CppParser::Parameter*)_element);
        _tmpParameters->Add(_marshalElement);
    }
    return _tmpParameters;
}

void CppSharp::Parser::Function::Parameters::set(System::Collections::Generic::List<CppSharp::Parser::Parameter^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Parameter*>();
    for each(CppSharp::Parser::Parameter^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Parameter*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Function*)NativePtr)->Parameters = _tmpvalue;
}

CppSharp::Parser::Method::Method(::CppSharp::CppParser::Method* native)
    : CppSharp::Parser::Function((::CppSharp::CppParser::Function*)native)
{
}

CppSharp::Parser::Method::Method(System::IntPtr native)
    : CppSharp::Parser::Function(native)
{
    auto __native = (::CppSharp::CppParser::Method*)native.ToPointer();
}

CppSharp::Parser::Method::Method()
    : CppSharp::Parser::Function((::CppSharp::CppParser::Function*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Method();
}

CppSharp::Parser::AccessSpecifierDecl^ CppSharp::Parser::Method::AccessDecl::get()
{
    return gcnew CppSharp::Parser::AccessSpecifierDecl((::CppSharp::CppParser::AccessSpecifierDecl*)((::CppSharp::CppParser::Method*)NativePtr)->AccessDecl);
}

void CppSharp::Parser::Method::AccessDecl::set(CppSharp::Parser::AccessSpecifierDecl^ value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->AccessDecl = (::CppSharp::CppParser::AccessSpecifierDecl*)value->NativePtr;
}

bool CppSharp::Parser::Method::IsVirtual::get()
{
    return ((::CppSharp::CppParser::Method*)NativePtr)->IsVirtual;
}

void CppSharp::Parser::Method::IsVirtual::set(bool value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->IsVirtual = value;
}

bool CppSharp::Parser::Method::IsStatic::get()
{
    return ((::CppSharp::CppParser::Method*)NativePtr)->IsStatic;
}

void CppSharp::Parser::Method::IsStatic::set(bool value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->IsStatic = value;
}

bool CppSharp::Parser::Method::IsConst::get()
{
    return ((::CppSharp::CppParser::Method*)NativePtr)->IsConst;
}

void CppSharp::Parser::Method::IsConst::set(bool value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->IsConst = value;
}

bool CppSharp::Parser::Method::IsImplicit::get()
{
    return ((::CppSharp::CppParser::Method*)NativePtr)->IsImplicit;
}

void CppSharp::Parser::Method::IsImplicit::set(bool value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->IsImplicit = value;
}

bool CppSharp::Parser::Method::IsOverride::get()
{
    return ((::CppSharp::CppParser::Method*)NativePtr)->IsOverride;
}

void CppSharp::Parser::Method::IsOverride::set(bool value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->IsOverride = value;
}

CppSharp::Parser::CXXMethodKind CppSharp::Parser::Method::Kind::get()
{
    return (CppSharp::Parser::CXXMethodKind)((::CppSharp::CppParser::Method*)NativePtr)->Kind;
}

void CppSharp::Parser::Method::Kind::set(CppSharp::Parser::CXXMethodKind value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->Kind = (::CppSharp::CppParser::CXXMethodKind)value;
}

bool CppSharp::Parser::Method::IsDefaultConstructor::get()
{
    return ((::CppSharp::CppParser::Method*)NativePtr)->IsDefaultConstructor;
}

void CppSharp::Parser::Method::IsDefaultConstructor::set(bool value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->IsDefaultConstructor = value;
}

bool CppSharp::Parser::Method::IsCopyConstructor::get()
{
    return ((::CppSharp::CppParser::Method*)NativePtr)->IsCopyConstructor;
}

void CppSharp::Parser::Method::IsCopyConstructor::set(bool value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->IsCopyConstructor = value;
}

bool CppSharp::Parser::Method::IsMoveConstructor::get()
{
    return ((::CppSharp::CppParser::Method*)NativePtr)->IsMoveConstructor;
}

void CppSharp::Parser::Method::IsMoveConstructor::set(bool value)
{
    ((::CppSharp::CppParser::Method*)NativePtr)->IsMoveConstructor = value;
}

CppSharp::Parser::Enumeration::Item::Item(::CppSharp::CppParser::Enumeration::Item* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::Enumeration::Item::Item(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::Enumeration::Item*)native.ToPointer();
}

CppSharp::Parser::Enumeration::Item::Item()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Enumeration::Item();
}

System::String^ CppSharp::Parser::Enumeration::Item::Name::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::Enumeration::Item*)NativePtr)->Name);
}

void CppSharp::Parser::Enumeration::Item::Name::set(System::String^ value)
{
    ((::CppSharp::CppParser::Enumeration::Item*)NativePtr)->Name = clix::marshalString<clix::E_UTF8>(value);
}

System::String^ CppSharp::Parser::Enumeration::Item::Expression::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::Enumeration::Item*)NativePtr)->Expression);
}

void CppSharp::Parser::Enumeration::Item::Expression::set(System::String^ value)
{
    ((::CppSharp::CppParser::Enumeration::Item*)NativePtr)->Expression = clix::marshalString<clix::E_UTF8>(value);
}

System::String^ CppSharp::Parser::Enumeration::Item::Comment::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::Enumeration::Item*)NativePtr)->Comment);
}

void CppSharp::Parser::Enumeration::Item::Comment::set(System::String^ value)
{
    ((::CppSharp::CppParser::Enumeration::Item*)NativePtr)->Comment = clix::marshalString<clix::E_UTF8>(value);
}

unsigned long long CppSharp::Parser::Enumeration::Item::Value::get()
{
    return ((::CppSharp::CppParser::Enumeration::Item*)NativePtr)->Value;
}

void CppSharp::Parser::Enumeration::Item::Value::set(unsigned long long value)
{
    ((::CppSharp::CppParser::Enumeration::Item*)NativePtr)->Value = (::uint64_t)value;
}

CppSharp::Parser::Enumeration::Enumeration(::CppSharp::CppParser::Enumeration* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::Enumeration::Enumeration(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::Enumeration*)native.ToPointer();
}

CppSharp::Parser::Enumeration::Enumeration()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Enumeration();
}

CppSharp::Parser::Enumeration::EnumModifiers CppSharp::Parser::Enumeration::Modifiers::get()
{
    return (CppSharp::Parser::Enumeration::EnumModifiers)((::CppSharp::CppParser::Enumeration*)NativePtr)->Modifiers;
}

void CppSharp::Parser::Enumeration::Modifiers::set(CppSharp::Parser::Enumeration::EnumModifiers value)
{
    ((::CppSharp::CppParser::Enumeration*)NativePtr)->Modifiers = (::CppSharp::CppParser::Enumeration::EnumModifiers)value;
}

CppSharp::Parser::Type^ CppSharp::Parser::Enumeration::Type::get()
{
    return gcnew CppSharp::Parser::Type((::CppSharp::CppParser::Type*)((::CppSharp::CppParser::Enumeration*)NativePtr)->Type);
}

void CppSharp::Parser::Enumeration::Type::set(CppSharp::Parser::Type^ value)
{
    ((::CppSharp::CppParser::Enumeration*)NativePtr)->Type = (::CppSharp::CppParser::Type*)value->NativePtr;
}

CppSharp::Parser::BuiltinType^ CppSharp::Parser::Enumeration::BuiltinType::get()
{
    return gcnew CppSharp::Parser::BuiltinType((::CppSharp::CppParser::BuiltinType*)((::CppSharp::CppParser::Enumeration*)NativePtr)->BuiltinType);
}

void CppSharp::Parser::Enumeration::BuiltinType::set(CppSharp::Parser::BuiltinType^ value)
{
    ((::CppSharp::CppParser::Enumeration*)NativePtr)->BuiltinType = (::CppSharp::CppParser::BuiltinType*)value->NativePtr;
}

System::Collections::Generic::List<CppSharp::Parser::Enumeration::Item^>^ CppSharp::Parser::Enumeration::Items::get()
{
    auto _tmpItems = gcnew System::Collections::Generic::List<CppSharp::Parser::Enumeration::Item^>();
    for(auto _element : ((::CppSharp::CppParser::Enumeration*)NativePtr)->Items)
    {
        auto ___element = new ::CppSharp::CppParser::Enumeration::Item(_element);
        auto _marshalElement = gcnew CppSharp::Parser::Enumeration::Item((::CppSharp::CppParser::Enumeration::Item*)___element);
        _tmpItems->Add(_marshalElement);
    }
    return _tmpItems;
}

void CppSharp::Parser::Enumeration::Items::set(System::Collections::Generic::List<CppSharp::Parser::Enumeration::Item^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Enumeration::Item>();
    for each(CppSharp::Parser::Enumeration::Item^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::Enumeration::Item*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Enumeration*)NativePtr)->Items = _tmpvalue;
}

CppSharp::Parser::Variable::Variable(::CppSharp::CppParser::Variable* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::Variable::Variable(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::Variable*)native.ToPointer();
}

CppSharp::Parser::Variable::Variable()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Variable();
}

System::String^ CppSharp::Parser::Variable::Mangled::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::Variable*)NativePtr)->Mangled);
}

void CppSharp::Parser::Variable::Mangled::set(System::String^ value)
{
    ((::CppSharp::CppParser::Variable*)NativePtr)->Mangled = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::Variable::QualifiedType::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::Variable*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::Variable::QualifiedType::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::Variable*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::BaseClassSpecifier::BaseClassSpecifier(::CppSharp::CppParser::BaseClassSpecifier* native)
{
    NativePtr = native;
}

CppSharp::Parser::BaseClassSpecifier::BaseClassSpecifier(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::BaseClassSpecifier*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::BaseClassSpecifier::BaseClassSpecifier()
{
    NativePtr = new ::CppSharp::CppParser::BaseClassSpecifier();
}

System::IntPtr CppSharp::Parser::BaseClassSpecifier::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::BaseClassSpecifier::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::BaseClassSpecifier*)object.ToPointer();
}

CppSharp::Parser::AccessSpecifier CppSharp::Parser::BaseClassSpecifier::Access::get()
{
    return (CppSharp::Parser::AccessSpecifier)((::CppSharp::CppParser::BaseClassSpecifier*)NativePtr)->Access;
}

void CppSharp::Parser::BaseClassSpecifier::Access::set(CppSharp::Parser::AccessSpecifier value)
{
    ((::CppSharp::CppParser::BaseClassSpecifier*)NativePtr)->Access = (::CppSharp::CppParser::AccessSpecifier)value;
}

bool CppSharp::Parser::BaseClassSpecifier::IsVirtual::get()
{
    return ((::CppSharp::CppParser::BaseClassSpecifier*)NativePtr)->IsVirtual;
}

void CppSharp::Parser::BaseClassSpecifier::IsVirtual::set(bool value)
{
    ((::CppSharp::CppParser::BaseClassSpecifier*)NativePtr)->IsVirtual = value;
}

CppSharp::Parser::Type^ CppSharp::Parser::BaseClassSpecifier::Type::get()
{
    return gcnew CppSharp::Parser::Type((::CppSharp::CppParser::Type*)((::CppSharp::CppParser::BaseClassSpecifier*)NativePtr)->Type);
}

void CppSharp::Parser::BaseClassSpecifier::Type::set(CppSharp::Parser::Type^ value)
{
    ((::CppSharp::CppParser::BaseClassSpecifier*)NativePtr)->Type = (::CppSharp::CppParser::Type*)value->NativePtr;
}

CppSharp::Parser::Field::Field(::CppSharp::CppParser::Field* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::Field::Field(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::Field*)native.ToPointer();
}

CppSharp::Parser::Field::Field()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Field();
}

CppSharp::Parser::QualifiedType^ CppSharp::Parser::Field::QualifiedType::get()
{
    return gcnew CppSharp::Parser::QualifiedType((::CppSharp::CppParser::QualifiedType*)&((::CppSharp::CppParser::Field*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::Field::QualifiedType::set(CppSharp::Parser::QualifiedType^ value)
{
    ((::CppSharp::CppParser::Field*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AccessSpecifier CppSharp::Parser::Field::Access::get()
{
    return (CppSharp::Parser::AccessSpecifier)((::CppSharp::CppParser::Field*)NativePtr)->Access;
}

void CppSharp::Parser::Field::Access::set(CppSharp::Parser::AccessSpecifier value)
{
    ((::CppSharp::CppParser::Field*)NativePtr)->Access = (::CppSharp::CppParser::AccessSpecifier)value;
}

unsigned int CppSharp::Parser::Field::Offset::get()
{
    return ((::CppSharp::CppParser::Field*)NativePtr)->Offset;
}

void CppSharp::Parser::Field::Offset::set(unsigned int value)
{
    ((::CppSharp::CppParser::Field*)NativePtr)->Offset = value;
}

CppSharp::Parser::Class^ CppSharp::Parser::Field::Class::get()
{
    return gcnew CppSharp::Parser::Class((::CppSharp::CppParser::Class*)((::CppSharp::CppParser::Field*)NativePtr)->Class);
}

void CppSharp::Parser::Field::Class::set(CppSharp::Parser::Class^ value)
{
    ((::CppSharp::CppParser::Field*)NativePtr)->Class = (::CppSharp::CppParser::Class*)value->NativePtr;
}

CppSharp::Parser::AccessSpecifierDecl::AccessSpecifierDecl(::CppSharp::CppParser::AccessSpecifierDecl* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::AccessSpecifierDecl::AccessSpecifierDecl(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AccessSpecifierDecl*)native.ToPointer();
}

CppSharp::Parser::AccessSpecifierDecl::AccessSpecifierDecl()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AccessSpecifierDecl();
}

CppSharp::Parser::Class::Class(::CppSharp::CppParser::Class* native)
    : CppSharp::Parser::DeclarationContext((::CppSharp::CppParser::DeclarationContext*)native)
{
}

CppSharp::Parser::Class::Class(System::IntPtr native)
    : CppSharp::Parser::DeclarationContext(native)
{
    auto __native = (::CppSharp::CppParser::Class*)native.ToPointer();
}

CppSharp::Parser::Class::Class()
    : CppSharp::Parser::DeclarationContext((::CppSharp::CppParser::DeclarationContext*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Class();
}

System::Collections::Generic::List<CppSharp::Parser::BaseClassSpecifier^>^ CppSharp::Parser::Class::Bases::get()
{
    auto _tmpBases = gcnew System::Collections::Generic::List<CppSharp::Parser::BaseClassSpecifier^>();
    for(auto _element : ((::CppSharp::CppParser::Class*)NativePtr)->Bases)
    {
        auto _marshalElement = gcnew CppSharp::Parser::BaseClassSpecifier((::CppSharp::CppParser::BaseClassSpecifier*)_element);
        _tmpBases->Add(_marshalElement);
    }
    return _tmpBases;
}

void CppSharp::Parser::Class::Bases::set(System::Collections::Generic::List<CppSharp::Parser::BaseClassSpecifier^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::BaseClassSpecifier*>();
    for each(CppSharp::Parser::BaseClassSpecifier^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::BaseClassSpecifier*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Class*)NativePtr)->Bases = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::Field^>^ CppSharp::Parser::Class::Fields::get()
{
    auto _tmpFields = gcnew System::Collections::Generic::List<CppSharp::Parser::Field^>();
    for(auto _element : ((::CppSharp::CppParser::Class*)NativePtr)->Fields)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Field((::CppSharp::CppParser::Field*)_element);
        _tmpFields->Add(_marshalElement);
    }
    return _tmpFields;
}

void CppSharp::Parser::Class::Fields::set(System::Collections::Generic::List<CppSharp::Parser::Field^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Field*>();
    for each(CppSharp::Parser::Field^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Field*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Class*)NativePtr)->Fields = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::Method^>^ CppSharp::Parser::Class::Methods::get()
{
    auto _tmpMethods = gcnew System::Collections::Generic::List<CppSharp::Parser::Method^>();
    for(auto _element : ((::CppSharp::CppParser::Class*)NativePtr)->Methods)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Method((::CppSharp::CppParser::Method*)_element);
        _tmpMethods->Add(_marshalElement);
    }
    return _tmpMethods;
}

void CppSharp::Parser::Class::Methods::set(System::Collections::Generic::List<CppSharp::Parser::Method^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Method*>();
    for each(CppSharp::Parser::Method^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Method*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Class*)NativePtr)->Methods = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AccessSpecifierDecl^>^ CppSharp::Parser::Class::Specifiers::get()
{
    auto _tmpSpecifiers = gcnew System::Collections::Generic::List<CppSharp::Parser::AccessSpecifierDecl^>();
    for(auto _element : ((::CppSharp::CppParser::Class*)NativePtr)->Specifiers)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AccessSpecifierDecl((::CppSharp::CppParser::AccessSpecifierDecl*)_element);
        _tmpSpecifiers->Add(_marshalElement);
    }
    return _tmpSpecifiers;
}

void CppSharp::Parser::Class::Specifiers::set(System::Collections::Generic::List<CppSharp::Parser::AccessSpecifierDecl^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AccessSpecifierDecl*>();
    for each(CppSharp::Parser::AccessSpecifierDecl^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AccessSpecifierDecl*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Class*)NativePtr)->Specifiers = _tmpvalue;
}

bool CppSharp::Parser::Class::IsPOD::get()
{
    return ((::CppSharp::CppParser::Class*)NativePtr)->IsPOD;
}

void CppSharp::Parser::Class::IsPOD::set(bool value)
{
    ((::CppSharp::CppParser::Class*)NativePtr)->IsPOD = value;
}

bool CppSharp::Parser::Class::IsAbstract::get()
{
    return ((::CppSharp::CppParser::Class*)NativePtr)->IsAbstract;
}

void CppSharp::Parser::Class::IsAbstract::set(bool value)
{
    ((::CppSharp::CppParser::Class*)NativePtr)->IsAbstract = value;
}

bool CppSharp::Parser::Class::IsUnion::get()
{
    return ((::CppSharp::CppParser::Class*)NativePtr)->IsUnion;
}

void CppSharp::Parser::Class::IsUnion::set(bool value)
{
    ((::CppSharp::CppParser::Class*)NativePtr)->IsUnion = value;
}

bool CppSharp::Parser::Class::IsDynamic::get()
{
    return ((::CppSharp::CppParser::Class*)NativePtr)->IsDynamic;
}

void CppSharp::Parser::Class::IsDynamic::set(bool value)
{
    ((::CppSharp::CppParser::Class*)NativePtr)->IsDynamic = value;
}

bool CppSharp::Parser::Class::IsPolymorphic::get()
{
    return ((::CppSharp::CppParser::Class*)NativePtr)->IsPolymorphic;
}

void CppSharp::Parser::Class::IsPolymorphic::set(bool value)
{
    ((::CppSharp::CppParser::Class*)NativePtr)->IsPolymorphic = value;
}

bool CppSharp::Parser::Class::HasNonTrivialDefaultConstructor::get()
{
    return ((::CppSharp::CppParser::Class*)NativePtr)->HasNonTrivialDefaultConstructor;
}

void CppSharp::Parser::Class::HasNonTrivialDefaultConstructor::set(bool value)
{
    ((::CppSharp::CppParser::Class*)NativePtr)->HasNonTrivialDefaultConstructor = value;
}

bool CppSharp::Parser::Class::HasNonTrivialCopyConstructor::get()
{
    return ((::CppSharp::CppParser::Class*)NativePtr)->HasNonTrivialCopyConstructor;
}

void CppSharp::Parser::Class::HasNonTrivialCopyConstructor::set(bool value)
{
    ((::CppSharp::CppParser::Class*)NativePtr)->HasNonTrivialCopyConstructor = value;
}

CppSharp::Parser::ClassLayout^ CppSharp::Parser::Class::Layout::get()
{
    return gcnew CppSharp::Parser::ClassLayout((::CppSharp::CppParser::ClassLayout*)&((::CppSharp::CppParser::Class*)NativePtr)->Layout);
}

void CppSharp::Parser::Class::Layout::set(CppSharp::Parser::ClassLayout^ value)
{
    ((::CppSharp::CppParser::Class*)NativePtr)->Layout = *(::CppSharp::CppParser::ClassLayout*)value->NativePtr;
}

CppSharp::Parser::Template::Template(::CppSharp::CppParser::Template* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::Template::Template(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::Template*)native.ToPointer();
}

CppSharp::Parser::Template::Template()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Template();
}

CppSharp::Parser::Declaration^ CppSharp::Parser::Template::TemplatedDecl::get()
{
    return gcnew CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)((::CppSharp::CppParser::Template*)NativePtr)->TemplatedDecl);
}

void CppSharp::Parser::Template::TemplatedDecl::set(CppSharp::Parser::Declaration^ value)
{
    ((::CppSharp::CppParser::Template*)NativePtr)->TemplatedDecl = (::CppSharp::CppParser::Declaration*)value->NativePtr;
}

System::Collections::Generic::List<CppSharp::Parser::TemplateParameter^>^ CppSharp::Parser::Template::Parameters::get()
{
    auto _tmpParameters = gcnew System::Collections::Generic::List<CppSharp::Parser::TemplateParameter^>();
    for(auto _element : ((::CppSharp::CppParser::Template*)NativePtr)->Parameters)
    {
        auto ___element = new ::CppSharp::CppParser::TemplateParameter(_element);
        auto _marshalElement = gcnew CppSharp::Parser::TemplateParameter((::CppSharp::CppParser::TemplateParameter*)___element);
        _tmpParameters->Add(_marshalElement);
    }
    return _tmpParameters;
}

void CppSharp::Parser::Template::Parameters::set(System::Collections::Generic::List<CppSharp::Parser::TemplateParameter^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::TemplateParameter>();
    for each(CppSharp::Parser::TemplateParameter^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::TemplateParameter*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Template*)NativePtr)->Parameters = _tmpvalue;
}

CppSharp::Parser::ClassTemplate::ClassTemplate(::CppSharp::CppParser::ClassTemplate* native)
    : CppSharp::Parser::Template((::CppSharp::CppParser::Template*)native)
{
}

CppSharp::Parser::ClassTemplate::ClassTemplate(System::IntPtr native)
    : CppSharp::Parser::Template(native)
{
    auto __native = (::CppSharp::CppParser::ClassTemplate*)native.ToPointer();
}

CppSharp::Parser::ClassTemplate::ClassTemplate()
    : CppSharp::Parser::Template((::CppSharp::CppParser::Template*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::ClassTemplate();
}

CppSharp::Parser::ClassTemplateSpecialization::ClassTemplateSpecialization(::CppSharp::CppParser::ClassTemplateSpecialization* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::ClassTemplateSpecialization::ClassTemplateSpecialization(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::ClassTemplateSpecialization*)native.ToPointer();
}

CppSharp::Parser::ClassTemplateSpecialization::ClassTemplateSpecialization()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::ClassTemplateSpecialization();
}

CppSharp::Parser::ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization(::CppSharp::CppParser::ClassTemplatePartialSpecialization* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::ClassTemplatePartialSpecialization*)native.ToPointer();
}

CppSharp::Parser::ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::ClassTemplatePartialSpecialization();
}

CppSharp::Parser::FunctionTemplate::FunctionTemplate(::CppSharp::CppParser::FunctionTemplate* native)
    : CppSharp::Parser::Template((::CppSharp::CppParser::Template*)native)
{
}

CppSharp::Parser::FunctionTemplate::FunctionTemplate(System::IntPtr native)
    : CppSharp::Parser::Template(native)
{
    auto __native = (::CppSharp::CppParser::FunctionTemplate*)native.ToPointer();
}

CppSharp::Parser::FunctionTemplate::FunctionTemplate()
    : CppSharp::Parser::Template((::CppSharp::CppParser::Template*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::FunctionTemplate();
}

CppSharp::Parser::Namespace::Namespace(::CppSharp::CppParser::Namespace* native)
    : CppSharp::Parser::DeclarationContext((::CppSharp::CppParser::DeclarationContext*)native)
{
}

CppSharp::Parser::Namespace::Namespace(System::IntPtr native)
    : CppSharp::Parser::DeclarationContext(native)
{
    auto __native = (::CppSharp::CppParser::Namespace*)native.ToPointer();
}

CppSharp::Parser::Namespace::Namespace()
    : CppSharp::Parser::DeclarationContext((::CppSharp::CppParser::DeclarationContext*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::Namespace();
}

CppSharp::Parser::PreprocessedEntity::PreprocessedEntity(::CppSharp::CppParser::PreprocessedEntity* native)
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)native)
{
}

CppSharp::Parser::PreprocessedEntity::PreprocessedEntity(System::IntPtr native)
    : CppSharp::Parser::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::PreprocessedEntity*)native.ToPointer();
}

CppSharp::Parser::PreprocessedEntity::PreprocessedEntity()
    : CppSharp::Parser::Declaration((::CppSharp::CppParser::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::PreprocessedEntity();
}

CppSharp::Parser::MacroLocation CppSharp::Parser::PreprocessedEntity::Location::get()
{
    return (CppSharp::Parser::MacroLocation)((::CppSharp::CppParser::PreprocessedEntity*)NativePtr)->Location;
}

void CppSharp::Parser::PreprocessedEntity::Location::set(CppSharp::Parser::MacroLocation value)
{
    ((::CppSharp::CppParser::PreprocessedEntity*)NativePtr)->Location = (::CppSharp::CppParser::MacroLocation)value;
}

CppSharp::Parser::MacroDefinition::MacroDefinition(::CppSharp::CppParser::MacroDefinition* native)
    : CppSharp::Parser::PreprocessedEntity((::CppSharp::CppParser::PreprocessedEntity*)native)
{
}

CppSharp::Parser::MacroDefinition::MacroDefinition(System::IntPtr native)
    : CppSharp::Parser::PreprocessedEntity(native)
{
    auto __native = (::CppSharp::CppParser::MacroDefinition*)native.ToPointer();
}

CppSharp::Parser::MacroDefinition::MacroDefinition()
    : CppSharp::Parser::PreprocessedEntity((::CppSharp::CppParser::PreprocessedEntity*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::MacroDefinition();
}

System::String^ CppSharp::Parser::MacroDefinition::Expression::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::MacroDefinition*)NativePtr)->Expression);
}

void CppSharp::Parser::MacroDefinition::Expression::set(System::String^ value)
{
    ((::CppSharp::CppParser::MacroDefinition*)NativePtr)->Expression = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::MacroExpansion::MacroExpansion(::CppSharp::CppParser::MacroExpansion* native)
    : CppSharp::Parser::PreprocessedEntity((::CppSharp::CppParser::PreprocessedEntity*)native)
{
}

CppSharp::Parser::MacroExpansion::MacroExpansion(System::IntPtr native)
    : CppSharp::Parser::PreprocessedEntity(native)
{
    auto __native = (::CppSharp::CppParser::MacroExpansion*)native.ToPointer();
}

CppSharp::Parser::MacroExpansion::MacroExpansion()
    : CppSharp::Parser::PreprocessedEntity((::CppSharp::CppParser::PreprocessedEntity*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::MacroExpansion();
}

System::String^ CppSharp::Parser::MacroExpansion::Text::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::MacroExpansion*)NativePtr)->Text);
}

void CppSharp::Parser::MacroExpansion::Text::set(System::String^ value)
{
    ((::CppSharp::CppParser::MacroExpansion*)NativePtr)->Text = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::MacroDefinition^ CppSharp::Parser::MacroExpansion::Definition::get()
{
    return gcnew CppSharp::Parser::MacroDefinition((::CppSharp::CppParser::MacroDefinition*)((::CppSharp::CppParser::MacroExpansion*)NativePtr)->Definition);
}

void CppSharp::Parser::MacroExpansion::Definition::set(CppSharp::Parser::MacroDefinition^ value)
{
    ((::CppSharp::CppParser::MacroExpansion*)NativePtr)->Definition = (::CppSharp::CppParser::MacroDefinition*)value->NativePtr;
}

CppSharp::Parser::TranslationUnit::TranslationUnit(::CppSharp::CppParser::TranslationUnit* native)
    : CppSharp::Parser::Namespace((::CppSharp::CppParser::Namespace*)native)
{
}

CppSharp::Parser::TranslationUnit::TranslationUnit(System::IntPtr native)
    : CppSharp::Parser::Namespace(native)
{
    auto __native = (::CppSharp::CppParser::TranslationUnit*)native.ToPointer();
}

CppSharp::Parser::TranslationUnit::TranslationUnit()
    : CppSharp::Parser::Namespace((::CppSharp::CppParser::Namespace*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::TranslationUnit();
}

System::String^ CppSharp::Parser::TranslationUnit::FileName::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::TranslationUnit*)NativePtr)->FileName);
}

void CppSharp::Parser::TranslationUnit::FileName::set(System::String^ value)
{
    ((::CppSharp::CppParser::TranslationUnit*)NativePtr)->FileName = clix::marshalString<clix::E_UTF8>(value);
}

bool CppSharp::Parser::TranslationUnit::IsSystemHeader::get()
{
    return ((::CppSharp::CppParser::TranslationUnit*)NativePtr)->IsSystemHeader;
}

void CppSharp::Parser::TranslationUnit::IsSystemHeader::set(bool value)
{
    ((::CppSharp::CppParser::TranslationUnit*)NativePtr)->IsSystemHeader = value;
}

System::Collections::Generic::List<CppSharp::Parser::Namespace^>^ CppSharp::Parser::TranslationUnit::Namespaces::get()
{
    auto _tmpNamespaces = gcnew System::Collections::Generic::List<CppSharp::Parser::Namespace^>();
    for(auto _element : ((::CppSharp::CppParser::TranslationUnit*)NativePtr)->Namespaces)
    {
        auto _marshalElement = gcnew CppSharp::Parser::Namespace((::CppSharp::CppParser::Namespace*)_element);
        _tmpNamespaces->Add(_marshalElement);
    }
    return _tmpNamespaces;
}

void CppSharp::Parser::TranslationUnit::Namespaces::set(System::Collections::Generic::List<CppSharp::Parser::Namespace^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::Namespace*>();
    for each(CppSharp::Parser::Namespace^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::Namespace*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::TranslationUnit*)NativePtr)->Namespaces = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::MacroDefinition^>^ CppSharp::Parser::TranslationUnit::Macros::get()
{
    auto _tmpMacros = gcnew System::Collections::Generic::List<CppSharp::Parser::MacroDefinition^>();
    for(auto _element : ((::CppSharp::CppParser::TranslationUnit*)NativePtr)->Macros)
    {
        auto _marshalElement = gcnew CppSharp::Parser::MacroDefinition((::CppSharp::CppParser::MacroDefinition*)_element);
        _tmpMacros->Add(_marshalElement);
    }
    return _tmpMacros;
}

void CppSharp::Parser::TranslationUnit::Macros::set(System::Collections::Generic::List<CppSharp::Parser::MacroDefinition^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::MacroDefinition*>();
    for each(CppSharp::Parser::MacroDefinition^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::MacroDefinition*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::TranslationUnit*)NativePtr)->Macros = _tmpvalue;
}

CppSharp::Parser::NativeLibrary::NativeLibrary(::CppSharp::CppParser::NativeLibrary* native)
{
    NativePtr = native;
}

CppSharp::Parser::NativeLibrary::NativeLibrary(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::NativeLibrary*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::NativeLibrary::NativeLibrary()
{
    NativePtr = new ::CppSharp::CppParser::NativeLibrary();
}

System::IntPtr CppSharp::Parser::NativeLibrary::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::NativeLibrary::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::NativeLibrary*)object.ToPointer();
}

System::String^ CppSharp::Parser::NativeLibrary::FileName::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::NativeLibrary*)NativePtr)->FileName);
}

void CppSharp::Parser::NativeLibrary::FileName::set(System::String^ value)
{
    ((::CppSharp::CppParser::NativeLibrary*)NativePtr)->FileName = clix::marshalString<clix::E_UTF8>(value);
}

System::Collections::Generic::List<System::String^>^ CppSharp::Parser::NativeLibrary::Symbols::get()
{
    auto _tmpSymbols = gcnew System::Collections::Generic::List<System::String^>();
    for(auto _element : ((::CppSharp::CppParser::NativeLibrary*)NativePtr)->Symbols)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpSymbols->Add(_marshalElement);
    }
    return _tmpSymbols;
}

void CppSharp::Parser::NativeLibrary::Symbols::set(System::Collections::Generic::List<System::String^>^ value)
{
    auto _tmpvalue = std::vector<::std::string>();
    for each(System::String^ _element in value)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::NativeLibrary*)NativePtr)->Symbols = _tmpvalue;
}

CppSharp::Parser::Library::Library(::CppSharp::CppParser::Library* native)
{
    NativePtr = native;
}

CppSharp::Parser::Library::Library(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::Library*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::TranslationUnit^ CppSharp::Parser::Library::FindOrCreateModule(System::String^ File)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(File);
    auto __ret = ((::CppSharp::CppParser::Library*)NativePtr)->FindOrCreateModule(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::TranslationUnit((::CppSharp::CppParser::TranslationUnit*)__ret);
}

CppSharp::Parser::NativeLibrary^ CppSharp::Parser::Library::FindOrCreateLibrary(System::String^ File)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(File);
    auto __ret = ((::CppSharp::CppParser::Library*)NativePtr)->FindOrCreateLibrary(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::NativeLibrary((::CppSharp::CppParser::NativeLibrary*)__ret);
}

CppSharp::Parser::Library::Library()
{
    NativePtr = new ::CppSharp::CppParser::Library();
}

System::IntPtr CppSharp::Parser::Library::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::Library::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::Library*)object.ToPointer();
}

System::Collections::Generic::List<CppSharp::Parser::TranslationUnit^>^ CppSharp::Parser::Library::TranslationUnits::get()
{
    auto _tmpTranslationUnits = gcnew System::Collections::Generic::List<CppSharp::Parser::TranslationUnit^>();
    for(auto _element : ((::CppSharp::CppParser::Library*)NativePtr)->TranslationUnits)
    {
        auto _marshalElement = gcnew CppSharp::Parser::TranslationUnit((::CppSharp::CppParser::TranslationUnit*)_element);
        _tmpTranslationUnits->Add(_marshalElement);
    }
    return _tmpTranslationUnits;
}

void CppSharp::Parser::Library::TranslationUnits::set(System::Collections::Generic::List<CppSharp::Parser::TranslationUnit^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::TranslationUnit*>();
    for each(CppSharp::Parser::TranslationUnit^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::TranslationUnit*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Library*)NativePtr)->TranslationUnits = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::NativeLibrary^>^ CppSharp::Parser::Library::Libraries::get()
{
    auto _tmpLibraries = gcnew System::Collections::Generic::List<CppSharp::Parser::NativeLibrary^>();
    for(auto _element : ((::CppSharp::CppParser::Library*)NativePtr)->Libraries)
    {
        auto _marshalElement = gcnew CppSharp::Parser::NativeLibrary((::CppSharp::CppParser::NativeLibrary*)_element);
        _tmpLibraries->Add(_marshalElement);
    }
    return _tmpLibraries;
}

void CppSharp::Parser::Library::Libraries::set(System::Collections::Generic::List<CppSharp::Parser::NativeLibrary^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::NativeLibrary*>();
    for each(CppSharp::Parser::NativeLibrary^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::NativeLibrary*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::Library*)NativePtr)->Libraries = _tmpvalue;
}

