#include "AST.h"

using namespace System;
using namespace System::Runtime::InteropServices;

CppSharp::Parser::AST::Type::Type(::CppSharp::CppParser::AST::Type* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::Type::Type(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::Type*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::Type::Type()
{
    NativePtr = new ::CppSharp::CppParser::AST::Type();
}

System::IntPtr CppSharp::Parser::AST::Type::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::Type::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::Type*)object.ToPointer();
}

bool CppSharp::Parser::AST::Type::IsDependent::get()
{
    return ((::CppSharp::CppParser::AST::Type*)NativePtr)->IsDependent;
}

void CppSharp::Parser::AST::Type::IsDependent::set(bool value)
{
    ((::CppSharp::CppParser::AST::Type*)NativePtr)->IsDependent = value;
}

CppSharp::Parser::AST::TypeQualifiers::TypeQualifiers(::CppSharp::CppParser::AST::TypeQualifiers* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::TypeQualifiers::TypeQualifiers(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::TypeQualifiers*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::TypeQualifiers::TypeQualifiers()
{
    NativePtr = new ::CppSharp::CppParser::AST::TypeQualifiers();
}

System::IntPtr CppSharp::Parser::AST::TypeQualifiers::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::TypeQualifiers::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::TypeQualifiers*)object.ToPointer();
}

bool CppSharp::Parser::AST::TypeQualifiers::IsConst::get()
{
    return ((::CppSharp::CppParser::AST::TypeQualifiers*)NativePtr)->IsConst;
}

void CppSharp::Parser::AST::TypeQualifiers::IsConst::set(bool value)
{
    ((::CppSharp::CppParser::AST::TypeQualifiers*)NativePtr)->IsConst = value;
}

bool CppSharp::Parser::AST::TypeQualifiers::IsVolatile::get()
{
    return ((::CppSharp::CppParser::AST::TypeQualifiers*)NativePtr)->IsVolatile;
}

void CppSharp::Parser::AST::TypeQualifiers::IsVolatile::set(bool value)
{
    ((::CppSharp::CppParser::AST::TypeQualifiers*)NativePtr)->IsVolatile = value;
}

bool CppSharp::Parser::AST::TypeQualifiers::IsRestrict::get()
{
    return ((::CppSharp::CppParser::AST::TypeQualifiers*)NativePtr)->IsRestrict;
}

void CppSharp::Parser::AST::TypeQualifiers::IsRestrict::set(bool value)
{
    ((::CppSharp::CppParser::AST::TypeQualifiers*)NativePtr)->IsRestrict = value;
}

CppSharp::Parser::AST::QualifiedType::QualifiedType(::CppSharp::CppParser::AST::QualifiedType* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::QualifiedType::QualifiedType(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::QualifiedType*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::QualifiedType::QualifiedType()
{
    NativePtr = new ::CppSharp::CppParser::AST::QualifiedType();
}

System::IntPtr CppSharp::Parser::AST::QualifiedType::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::QualifiedType::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::QualifiedType*)object.ToPointer();
}

CppSharp::Parser::AST::Type^ CppSharp::Parser::AST::QualifiedType::Type::get()
{
    return gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Type);
}

void CppSharp::Parser::AST::QualifiedType::Type::set(CppSharp::Parser::AST::Type^ value)
{
    ((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Type = (::CppSharp::CppParser::AST::Type*)value->NativePtr;
}

CppSharp::Parser::AST::TypeQualifiers^ CppSharp::Parser::AST::QualifiedType::Qualifiers::get()
{
    return gcnew CppSharp::Parser::AST::TypeQualifiers((::CppSharp::CppParser::AST::TypeQualifiers*)&((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Qualifiers);
}

void CppSharp::Parser::AST::QualifiedType::Qualifiers::set(CppSharp::Parser::AST::TypeQualifiers^ value)
{
    ((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Qualifiers = *(::CppSharp::CppParser::AST::TypeQualifiers*)value->NativePtr;
}

CppSharp::Parser::AST::TagType::TagType(::CppSharp::CppParser::AST::TagType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TagType::TagType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::TagType*)native.ToPointer();
}

CppSharp::Parser::AST::TagType::TagType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TagType();
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::TagType::Declaration::get()
{
    return gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::TagType*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::TagType::Declaration::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::TagType*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

CppSharp::Parser::AST::ArrayType::ArrayType(::CppSharp::CppParser::AST::ArrayType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::ArrayType::ArrayType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::ArrayType*)native.ToPointer();
}

CppSharp::Parser::AST::ArrayType::ArrayType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::ArrayType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::ArrayType::QualifiedType::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::AST::ArrayType::QualifiedType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::ArrayType::ArraySize CppSharp::Parser::AST::ArrayType::SizeType::get()
{
    return (CppSharp::Parser::AST::ArrayType::ArraySize)((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->SizeType;
}

void CppSharp::Parser::AST::ArrayType::SizeType::set(CppSharp::Parser::AST::ArrayType::ArraySize value)
{
    ((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->SizeType = (::CppSharp::CppParser::AST::ArrayType::ArraySize)value;
}

int CppSharp::Parser::AST::ArrayType::Size::get()
{
    return ((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->Size;
}

void CppSharp::Parser::AST::ArrayType::Size::set(int value)
{
    ((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->Size = value;
}

CppSharp::Parser::AST::FunctionType::FunctionType(::CppSharp::CppParser::AST::FunctionType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::FunctionType::FunctionType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::FunctionType*)native.ToPointer();
}

CppSharp::Parser::AST::Parameter^ CppSharp::Parser::AST::FunctionType::getParameters(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->getParameters(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Parameter((::CppSharp::CppParser::AST::Parameter*)__ret);
}

unsigned int CppSharp::Parser::AST::FunctionType::getParametersCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->getParametersCount();
    return __ret;
}

CppSharp::Parser::AST::FunctionType::FunctionType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::FunctionType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::FunctionType::ReturnType::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->ReturnType);
}

void CppSharp::Parser::AST::FunctionType::ReturnType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->ReturnType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::CallingConvention CppSharp::Parser::AST::FunctionType::CallingConvention::get()
{
    return (CppSharp::Parser::AST::CallingConvention)((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->CallingConvention;
}

void CppSharp::Parser::AST::FunctionType::CallingConvention::set(CppSharp::Parser::AST::CallingConvention value)
{
    ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->CallingConvention = (::CppSharp::CppParser::AST::CallingConvention)value;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^ CppSharp::Parser::AST::FunctionType::Parameters::get()
{
    auto _tmpParameters = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>();
    for(auto _element : ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->Parameters)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Parameter((::CppSharp::CppParser::AST::Parameter*)_element);
        _tmpParameters->Add(_marshalElement);
    }
    return _tmpParameters;
}

void CppSharp::Parser::AST::FunctionType::Parameters::set(System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Parameter*>();
    for each(CppSharp::Parser::AST::Parameter^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Parameter*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->Parameters = _tmpvalue;
}

CppSharp::Parser::AST::PointerType::PointerType(::CppSharp::CppParser::AST::PointerType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::PointerType::PointerType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::PointerType*)native.ToPointer();
}

CppSharp::Parser::AST::PointerType::PointerType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::PointerType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::PointerType::QualifiedPointee::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::PointerType*)NativePtr)->QualifiedPointee);
}

void CppSharp::Parser::AST::PointerType::QualifiedPointee::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::PointerType*)NativePtr)->QualifiedPointee = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::PointerType::TypeModifier CppSharp::Parser::AST::PointerType::Modifier::get()
{
    return (CppSharp::Parser::AST::PointerType::TypeModifier)((::CppSharp::CppParser::AST::PointerType*)NativePtr)->Modifier;
}

void CppSharp::Parser::AST::PointerType::Modifier::set(CppSharp::Parser::AST::PointerType::TypeModifier value)
{
    ((::CppSharp::CppParser::AST::PointerType*)NativePtr)->Modifier = (::CppSharp::CppParser::AST::PointerType::TypeModifier)value;
}

CppSharp::Parser::AST::MemberPointerType::MemberPointerType(::CppSharp::CppParser::AST::MemberPointerType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::MemberPointerType::MemberPointerType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::MemberPointerType*)native.ToPointer();
}

CppSharp::Parser::AST::MemberPointerType::MemberPointerType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::MemberPointerType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::MemberPointerType::Pointee::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::MemberPointerType*)NativePtr)->Pointee);
}

void CppSharp::Parser::AST::MemberPointerType::Pointee::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::MemberPointerType*)NativePtr)->Pointee = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::TypedefType::TypedefType(::CppSharp::CppParser::AST::TypedefType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TypedefType::TypedefType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::TypedefType*)native.ToPointer();
}

CppSharp::Parser::AST::TypedefType::TypedefType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TypedefType();
}

CppSharp::Parser::AST::TypedefDecl^ CppSharp::Parser::AST::TypedefType::Declaration::get()
{
    return gcnew CppSharp::Parser::AST::TypedefDecl((::CppSharp::CppParser::AST::TypedefDecl*)((::CppSharp::CppParser::AST::TypedefType*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::TypedefType::Declaration::set(CppSharp::Parser::AST::TypedefDecl^ value)
{
    ((::CppSharp::CppParser::AST::TypedefType*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::TypedefDecl*)value->NativePtr;
}

CppSharp::Parser::AST::AttributedType::AttributedType(::CppSharp::CppParser::AST::AttributedType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::AttributedType::AttributedType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::AttributedType*)native.ToPointer();
}

CppSharp::Parser::AST::AttributedType::AttributedType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::AttributedType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::AttributedType::Modified::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Modified);
}

void CppSharp::Parser::AST::AttributedType::Modified::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Modified = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::AttributedType::Equivalent::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Equivalent);
}

void CppSharp::Parser::AST::AttributedType::Equivalent::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Equivalent = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::DecayedType::DecayedType(::CppSharp::CppParser::AST::DecayedType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::DecayedType::DecayedType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::DecayedType*)native.ToPointer();
}

CppSharp::Parser::AST::DecayedType::DecayedType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::DecayedType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::DecayedType::Decayed::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Decayed);
}

void CppSharp::Parser::AST::DecayedType::Decayed::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Decayed = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::DecayedType::Original::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Original);
}

void CppSharp::Parser::AST::DecayedType::Original::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Original = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::DecayedType::Pointee::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Pointee);
}

void CppSharp::Parser::AST::DecayedType::Pointee::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Pointee = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::TemplateArgument::TemplateArgument(::CppSharp::CppParser::AST::TemplateArgument* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::TemplateArgument::TemplateArgument(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::TemplateArgument*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::TemplateArgument::TemplateArgument()
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateArgument();
}

System::IntPtr CppSharp::Parser::AST::TemplateArgument::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::TemplateArgument::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::TemplateArgument*)object.ToPointer();
}

CppSharp::Parser::AST::TemplateArgument::ArgumentKind CppSharp::Parser::AST::TemplateArgument::Kind::get()
{
    return (CppSharp::Parser::AST::TemplateArgument::ArgumentKind)((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Kind;
}

void CppSharp::Parser::AST::TemplateArgument::Kind::set(CppSharp::Parser::AST::TemplateArgument::ArgumentKind value)
{
    ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Kind = (::CppSharp::CppParser::AST::TemplateArgument::ArgumentKind)value;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::TemplateArgument::Type::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Type);
}

void CppSharp::Parser::AST::TemplateArgument::Type::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Type = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::TemplateArgument::Declaration::get()
{
    return gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::TemplateArgument::Declaration::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

int CppSharp::Parser::AST::TemplateArgument::Integral::get()
{
    return ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Integral;
}

void CppSharp::Parser::AST::TemplateArgument::Integral::set(int value)
{
    ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Integral = value;
}

CppSharp::Parser::AST::TemplateSpecializationType::TemplateSpecializationType(::CppSharp::CppParser::AST::TemplateSpecializationType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TemplateSpecializationType::TemplateSpecializationType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::TemplateSpecializationType*)native.ToPointer();
}

CppSharp::Parser::AST::TemplateArgument^ CppSharp::Parser::AST::TemplateSpecializationType::getArguments(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->getArguments(i);
    auto ____ret = new ::CppSharp::CppParser::AST::TemplateArgument(__ret);
    return gcnew CppSharp::Parser::AST::TemplateArgument((::CppSharp::CppParser::AST::TemplateArgument*)____ret);
}

unsigned int CppSharp::Parser::AST::TemplateSpecializationType::getArgumentsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->getArgumentsCount();
    return __ret;
}

CppSharp::Parser::AST::TemplateSpecializationType::TemplateSpecializationType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateSpecializationType();
}

System::Collections::Generic::List<CppSharp::Parser::AST::TemplateArgument^>^ CppSharp::Parser::AST::TemplateSpecializationType::Arguments::get()
{
    auto _tmpArguments = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::TemplateArgument^>();
    for(auto _element : ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Arguments)
    {
        auto ___element = new ::CppSharp::CppParser::AST::TemplateArgument(_element);
        auto _marshalElement = gcnew CppSharp::Parser::AST::TemplateArgument((::CppSharp::CppParser::AST::TemplateArgument*)___element);
        _tmpArguments->Add(_marshalElement);
    }
    return _tmpArguments;
}

void CppSharp::Parser::AST::TemplateSpecializationType::Arguments::set(System::Collections::Generic::List<CppSharp::Parser::AST::TemplateArgument^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::TemplateArgument>();
    for each(CppSharp::Parser::AST::TemplateArgument^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::AST::TemplateArgument*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Arguments = _tmpvalue;
}

CppSharp::Parser::AST::Template^ CppSharp::Parser::AST::TemplateSpecializationType::Template::get()
{
    return gcnew CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Template);
}

void CppSharp::Parser::AST::TemplateSpecializationType::Template::set(CppSharp::Parser::AST::Template^ value)
{
    ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Template = (::CppSharp::CppParser::AST::Template*)value->NativePtr;
}

CppSharp::Parser::AST::Type^ CppSharp::Parser::AST::TemplateSpecializationType::Desugared::get()
{
    return gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Desugared);
}

void CppSharp::Parser::AST::TemplateSpecializationType::Desugared::set(CppSharp::Parser::AST::Type^ value)
{
    ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Desugared = (::CppSharp::CppParser::AST::Type*)value->NativePtr;
}

CppSharp::Parser::AST::TemplateParameter::TemplateParameter(::CppSharp::CppParser::AST::TemplateParameter* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::TemplateParameter::TemplateParameter(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::TemplateParameter*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::TemplateParameter::TemplateParameter()
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateParameter();
}

System::IntPtr CppSharp::Parser::AST::TemplateParameter::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::TemplateParameter::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::TemplateParameter*)object.ToPointer();
}

System::String^ CppSharp::Parser::AST::TemplateParameter::Name::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::TemplateParameter*)NativePtr)->Name);
}

void CppSharp::Parser::AST::TemplateParameter::Name::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::TemplateParameter*)NativePtr)->Name = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::AST::TemplateParameterType::TemplateParameterType(::CppSharp::CppParser::AST::TemplateParameterType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TemplateParameterType::TemplateParameterType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::TemplateParameterType*)native.ToPointer();
}

CppSharp::Parser::AST::TemplateParameterType::TemplateParameterType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateParameterType();
}

CppSharp::Parser::AST::TemplateParameter^ CppSharp::Parser::AST::TemplateParameterType::Parameter::get()
{
    return gcnew CppSharp::Parser::AST::TemplateParameter((::CppSharp::CppParser::AST::TemplateParameter*)&((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Parameter);
}

void CppSharp::Parser::AST::TemplateParameterType::Parameter::set(CppSharp::Parser::AST::TemplateParameter^ value)
{
    ((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Parameter = *(::CppSharp::CppParser::AST::TemplateParameter*)value->NativePtr;
}

CppSharp::Parser::AST::TemplateParameterSubstitutionType::TemplateParameterSubstitutionType(::CppSharp::CppParser::AST::TemplateParameterSubstitutionType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TemplateParameterSubstitutionType::TemplateParameterSubstitutionType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::TemplateParameterSubstitutionType*)native.ToPointer();
}

CppSharp::Parser::AST::TemplateParameterSubstitutionType::TemplateParameterSubstitutionType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateParameterSubstitutionType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::TemplateParameterSubstitutionType::Replacement::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::TemplateParameterSubstitutionType*)NativePtr)->Replacement);
}

void CppSharp::Parser::AST::TemplateParameterSubstitutionType::Replacement::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::TemplateParameterSubstitutionType*)NativePtr)->Replacement = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::InjectedClassNameType::InjectedClassNameType(::CppSharp::CppParser::AST::InjectedClassNameType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::InjectedClassNameType::InjectedClassNameType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::InjectedClassNameType*)native.ToPointer();
}

CppSharp::Parser::AST::InjectedClassNameType::InjectedClassNameType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::InjectedClassNameType();
}

CppSharp::Parser::AST::TemplateSpecializationType^ CppSharp::Parser::AST::InjectedClassNameType::TemplateSpecialization::get()
{
    return gcnew CppSharp::Parser::AST::TemplateSpecializationType((::CppSharp::CppParser::AST::TemplateSpecializationType*)&((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->TemplateSpecialization);
}

void CppSharp::Parser::AST::InjectedClassNameType::TemplateSpecialization::set(CppSharp::Parser::AST::TemplateSpecializationType^ value)
{
    ((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->TemplateSpecialization = *(::CppSharp::CppParser::AST::TemplateSpecializationType*)value->NativePtr;
}

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::InjectedClassNameType::Class::get()
{
    return gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->Class);
}

void CppSharp::Parser::AST::InjectedClassNameType::Class::set(CppSharp::Parser::AST::Class^ value)
{
    ((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->Class = (::CppSharp::CppParser::AST::Class*)value->NativePtr;
}

CppSharp::Parser::AST::DependentNameType::DependentNameType(::CppSharp::CppParser::AST::DependentNameType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::DependentNameType::DependentNameType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::DependentNameType*)native.ToPointer();
}

CppSharp::Parser::AST::DependentNameType::DependentNameType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::DependentNameType();
}

CppSharp::Parser::AST::BuiltinType::BuiltinType(::CppSharp::CppParser::AST::BuiltinType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::BuiltinType::BuiltinType(System::IntPtr native)
    : CppSharp::Parser::AST::Type(native)
{
    auto __native = (::CppSharp::CppParser::AST::BuiltinType*)native.ToPointer();
}

CppSharp::Parser::AST::BuiltinType::BuiltinType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::BuiltinType();
}

CppSharp::Parser::AST::PrimitiveType CppSharp::Parser::AST::BuiltinType::Type::get()
{
    return (CppSharp::Parser::AST::PrimitiveType)((::CppSharp::CppParser::AST::BuiltinType*)NativePtr)->Type;
}

void CppSharp::Parser::AST::BuiltinType::Type::set(CppSharp::Parser::AST::PrimitiveType value)
{
    ((::CppSharp::CppParser::AST::BuiltinType*)NativePtr)->Type = (::CppSharp::CppParser::AST::PrimitiveType)value;
}

CppSharp::Parser::AST::RawComment::RawComment(::CppSharp::CppParser::AST::RawComment* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::RawComment::RawComment(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::RawComment*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::RawComment::RawComment()
{
    NativePtr = new ::CppSharp::CppParser::AST::RawComment();
}

System::IntPtr CppSharp::Parser::AST::RawComment::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::RawComment::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::RawComment*)object.ToPointer();
}

CppSharp::Parser::AST::RawCommentKind CppSharp::Parser::AST::RawComment::Kind::get()
{
    return (CppSharp::Parser::AST::RawCommentKind)((::CppSharp::CppParser::AST::RawComment*)NativePtr)->Kind;
}

void CppSharp::Parser::AST::RawComment::Kind::set(CppSharp::Parser::AST::RawCommentKind value)
{
    ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->Kind = (::CppSharp::CppParser::AST::RawCommentKind)value;
}

System::String^ CppSharp::Parser::AST::RawComment::Text::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::RawComment*)NativePtr)->Text);
}

void CppSharp::Parser::AST::RawComment::Text::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->Text = clix::marshalString<clix::E_UTF8>(value);
}

System::String^ CppSharp::Parser::AST::RawComment::BriefText::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::RawComment*)NativePtr)->BriefText);
}

void CppSharp::Parser::AST::RawComment::BriefText::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->BriefText = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::AST::VTableComponent::VTableComponent(::CppSharp::CppParser::AST::VTableComponent* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::VTableComponent::VTableComponent(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::VTableComponent*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::VTableComponent::VTableComponent()
{
    NativePtr = new ::CppSharp::CppParser::AST::VTableComponent();
}

System::IntPtr CppSharp::Parser::AST::VTableComponent::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::VTableComponent::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::VTableComponent*)object.ToPointer();
}

CppSharp::Parser::AST::VTableComponentKind CppSharp::Parser::AST::VTableComponent::Kind::get()
{
    return (CppSharp::Parser::AST::VTableComponentKind)((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Kind;
}

void CppSharp::Parser::AST::VTableComponent::Kind::set(CppSharp::Parser::AST::VTableComponentKind value)
{
    ((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Kind = (::CppSharp::CppParser::AST::VTableComponentKind)value;
}

unsigned int CppSharp::Parser::AST::VTableComponent::Offset::get()
{
    return ((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Offset;
}

void CppSharp::Parser::AST::VTableComponent::Offset::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Offset = value;
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::VTableComponent::Declaration::get()
{
    return gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::VTableComponent::Declaration::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

CppSharp::Parser::AST::VTableLayout::VTableLayout(::CppSharp::CppParser::AST::VTableLayout* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::VTableLayout::VTableLayout(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::VTableLayout*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::VTableComponent^ CppSharp::Parser::AST::VTableLayout::getComponents(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::VTableLayout*)NativePtr)->getComponents(i);
    auto ____ret = new ::CppSharp::CppParser::AST::VTableComponent(__ret);
    return gcnew CppSharp::Parser::AST::VTableComponent((::CppSharp::CppParser::AST::VTableComponent*)____ret);
}

unsigned int CppSharp::Parser::AST::VTableLayout::getComponentsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::VTableLayout*)NativePtr)->getComponentsCount();
    return __ret;
}

CppSharp::Parser::AST::VTableLayout::VTableLayout()
{
    NativePtr = new ::CppSharp::CppParser::AST::VTableLayout();
}

System::IntPtr CppSharp::Parser::AST::VTableLayout::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::VTableLayout::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::VTableLayout*)object.ToPointer();
}

System::Collections::Generic::List<CppSharp::Parser::AST::VTableComponent^>^ CppSharp::Parser::AST::VTableLayout::Components::get()
{
    auto _tmpComponents = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::VTableComponent^>();
    for(auto _element : ((::CppSharp::CppParser::AST::VTableLayout*)NativePtr)->Components)
    {
        auto ___element = new ::CppSharp::CppParser::AST::VTableComponent(_element);
        auto _marshalElement = gcnew CppSharp::Parser::AST::VTableComponent((::CppSharp::CppParser::AST::VTableComponent*)___element);
        _tmpComponents->Add(_marshalElement);
    }
    return _tmpComponents;
}

void CppSharp::Parser::AST::VTableLayout::Components::set(System::Collections::Generic::List<CppSharp::Parser::AST::VTableComponent^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::VTableComponent>();
    for each(CppSharp::Parser::AST::VTableComponent^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::AST::VTableComponent*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::VTableLayout*)NativePtr)->Components = _tmpvalue;
}

CppSharp::Parser::AST::VFTableInfo::VFTableInfo(::CppSharp::CppParser::AST::VFTableInfo* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::VFTableInfo::VFTableInfo(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::VFTableInfo*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::VFTableInfo::VFTableInfo()
{
    NativePtr = new ::CppSharp::CppParser::AST::VFTableInfo();
}

System::IntPtr CppSharp::Parser::AST::VFTableInfo::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::VFTableInfo::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::VFTableInfo*)object.ToPointer();
}

unsigned long long CppSharp::Parser::AST::VFTableInfo::VBTableIndex::get()
{
    return ((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->VBTableIndex;
}

void CppSharp::Parser::AST::VFTableInfo::VBTableIndex::set(unsigned long long value)
{
    ((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->VBTableIndex = (::uint64_t)value;
}

unsigned int CppSharp::Parser::AST::VFTableInfo::VFPtrOffset::get()
{
    return ((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->VFPtrOffset;
}

void CppSharp::Parser::AST::VFTableInfo::VFPtrOffset::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->VFPtrOffset = (::uint32_t)value;
}

unsigned int CppSharp::Parser::AST::VFTableInfo::VFPtrFullOffset::get()
{
    return ((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->VFPtrFullOffset;
}

void CppSharp::Parser::AST::VFTableInfo::VFPtrFullOffset::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->VFPtrFullOffset = (::uint32_t)value;
}

CppSharp::Parser::AST::VTableLayout^ CppSharp::Parser::AST::VFTableInfo::Layout::get()
{
    return gcnew CppSharp::Parser::AST::VTableLayout((::CppSharp::CppParser::AST::VTableLayout*)&((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->Layout);
}

void CppSharp::Parser::AST::VFTableInfo::Layout::set(CppSharp::Parser::AST::VTableLayout^ value)
{
    ((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->Layout = *(::CppSharp::CppParser::AST::VTableLayout*)value->NativePtr;
}

CppSharp::Parser::AST::ClassLayout::ClassLayout(::CppSharp::CppParser::AST::ClassLayout* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::ClassLayout::ClassLayout(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::ClassLayout*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::VFTableInfo^ CppSharp::Parser::AST::ClassLayout::getVFTables(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->getVFTables(i);
    auto ____ret = new ::CppSharp::CppParser::AST::VFTableInfo(__ret);
    return gcnew CppSharp::Parser::AST::VFTableInfo((::CppSharp::CppParser::AST::VFTableInfo*)____ret);
}

unsigned int CppSharp::Parser::AST::ClassLayout::getVFTablesCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->getVFTablesCount();
    return __ret;
}

CppSharp::Parser::AST::ClassLayout::ClassLayout()
{
    NativePtr = new ::CppSharp::CppParser::AST::ClassLayout();
}

System::IntPtr CppSharp::Parser::AST::ClassLayout::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::ClassLayout::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::ClassLayout*)object.ToPointer();
}

CppSharp::Parser::AST::CppAbi CppSharp::Parser::AST::ClassLayout::ABI::get()
{
    return (CppSharp::Parser::AST::CppAbi)((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->ABI;
}

void CppSharp::Parser::AST::ClassLayout::ABI::set(CppSharp::Parser::AST::CppAbi value)
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->ABI = (::CppSharp::CppParser::AST::CppAbi)value;
}

System::Collections::Generic::List<CppSharp::Parser::AST::VFTableInfo^>^ CppSharp::Parser::AST::ClassLayout::VFTables::get()
{
    auto _tmpVFTables = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::VFTableInfo^>();
    for(auto _element : ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->VFTables)
    {
        auto ___element = new ::CppSharp::CppParser::AST::VFTableInfo(_element);
        auto _marshalElement = gcnew CppSharp::Parser::AST::VFTableInfo((::CppSharp::CppParser::AST::VFTableInfo*)___element);
        _tmpVFTables->Add(_marshalElement);
    }
    return _tmpVFTables;
}

void CppSharp::Parser::AST::ClassLayout::VFTables::set(System::Collections::Generic::List<CppSharp::Parser::AST::VFTableInfo^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::VFTableInfo>();
    for each(CppSharp::Parser::AST::VFTableInfo^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::AST::VFTableInfo*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->VFTables = _tmpvalue;
}

CppSharp::Parser::AST::VTableLayout^ CppSharp::Parser::AST::ClassLayout::Layout::get()
{
    return gcnew CppSharp::Parser::AST::VTableLayout((::CppSharp::CppParser::AST::VTableLayout*)&((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->Layout);
}

void CppSharp::Parser::AST::ClassLayout::Layout::set(CppSharp::Parser::AST::VTableLayout^ value)
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->Layout = *(::CppSharp::CppParser::AST::VTableLayout*)value->NativePtr;
}

bool CppSharp::Parser::AST::ClassLayout::HasOwnVFPtr::get()
{
    return ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->HasOwnVFPtr;
}

void CppSharp::Parser::AST::ClassLayout::HasOwnVFPtr::set(bool value)
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->HasOwnVFPtr = value;
}

int CppSharp::Parser::AST::ClassLayout::VBPtrOffset::get()
{
    return ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->VBPtrOffset;
}

void CppSharp::Parser::AST::ClassLayout::VBPtrOffset::set(int value)
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->VBPtrOffset = value;
}

int CppSharp::Parser::AST::ClassLayout::Alignment::get()
{
    return ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->Alignment;
}

void CppSharp::Parser::AST::ClassLayout::Alignment::set(int value)
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->Alignment = value;
}

int CppSharp::Parser::AST::ClassLayout::Size::get()
{
    return ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->Size;
}

void CppSharp::Parser::AST::ClassLayout::Size::set(int value)
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->Size = value;
}

int CppSharp::Parser::AST::ClassLayout::DataSize::get()
{
    return ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->DataSize;
}

void CppSharp::Parser::AST::ClassLayout::DataSize::set(int value)
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->DataSize = value;
}

CppSharp::Parser::AST::Declaration::Declaration(::CppSharp::CppParser::AST::Declaration* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::Declaration::Declaration(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::Declaration*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::Declaration::Declaration()
{
    NativePtr = new ::CppSharp::CppParser::AST::Declaration();
}

CppSharp::Parser::AST::PreprocessedEntity^ CppSharp::Parser::AST::Declaration::getPreprocessedEntities(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->getPreprocessedEntities(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)__ret);
}

unsigned int CppSharp::Parser::AST::Declaration::getPreprocessedEntitiesCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->getPreprocessedEntitiesCount();
    return __ret;
}

System::IntPtr CppSharp::Parser::AST::Declaration::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::Declaration::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::Declaration*)object.ToPointer();
}

CppSharp::Parser::AST::AccessSpecifier CppSharp::Parser::AST::Declaration::Access::get()
{
    return (CppSharp::Parser::AST::AccessSpecifier)((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Access;
}

void CppSharp::Parser::AST::Declaration::Access::set(CppSharp::Parser::AST::AccessSpecifier value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Access = (::CppSharp::CppParser::AST::AccessSpecifier)value;
}

CppSharp::Parser::AST::DeclarationContext^ CppSharp::Parser::AST::Declaration::_Namespace::get()
{
    return gcnew CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)((::CppSharp::CppParser::AST::Declaration*)NativePtr)->_Namespace);
}

void CppSharp::Parser::AST::Declaration::_Namespace::set(CppSharp::Parser::AST::DeclarationContext^ value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->_Namespace = (::CppSharp::CppParser::AST::DeclarationContext*)value->NativePtr;
}

System::String^ CppSharp::Parser::AST::Declaration::Name::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Name);
}

void CppSharp::Parser::AST::Declaration::Name::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Name = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::AST::RawComment^ CppSharp::Parser::AST::Declaration::Comment::get()
{
    return gcnew CppSharp::Parser::AST::RawComment((::CppSharp::CppParser::AST::RawComment*)((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Comment);
}

void CppSharp::Parser::AST::Declaration::Comment::set(CppSharp::Parser::AST::RawComment^ value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Comment = (::CppSharp::CppParser::AST::RawComment*)value->NativePtr;
}

System::String^ CppSharp::Parser::AST::Declaration::DebugText::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::Declaration*)NativePtr)->DebugText);
}

void CppSharp::Parser::AST::Declaration::DebugText::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->DebugText = clix::marshalString<clix::E_UTF8>(value);
}

bool CppSharp::Parser::AST::Declaration::IsIncomplete::get()
{
    return ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->IsIncomplete;
}

void CppSharp::Parser::AST::Declaration::IsIncomplete::set(bool value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->IsIncomplete = value;
}

bool CppSharp::Parser::AST::Declaration::IsDependent::get()
{
    return ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->IsDependent;
}

void CppSharp::Parser::AST::Declaration::IsDependent::set(bool value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->IsDependent = value;
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::Declaration::CompleteDeclaration::get()
{
    return gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::Declaration*)NativePtr)->CompleteDeclaration);
}

void CppSharp::Parser::AST::Declaration::CompleteDeclaration::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->CompleteDeclaration = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

unsigned int CppSharp::Parser::AST::Declaration::DefinitionOrder::get()
{
    return ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->DefinitionOrder;
}

void CppSharp::Parser::AST::Declaration::DefinitionOrder::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->DefinitionOrder = value;
}

System::Collections::Generic::List<CppSharp::Parser::AST::PreprocessedEntity^>^ CppSharp::Parser::AST::Declaration::PreprocessedEntities::get()
{
    auto _tmpPreprocessedEntities = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::PreprocessedEntity^>();
    for(auto _element : ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->PreprocessedEntities)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)_element);
        _tmpPreprocessedEntities->Add(_marshalElement);
    }
    return _tmpPreprocessedEntities;
}

void CppSharp::Parser::AST::Declaration::PreprocessedEntities::set(System::Collections::Generic::List<CppSharp::Parser::AST::PreprocessedEntity^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::PreprocessedEntity*>();
    for each(CppSharp::Parser::AST::PreprocessedEntity^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::PreprocessedEntity*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->PreprocessedEntities = _tmpvalue;
}

System::IntPtr CppSharp::Parser::AST::Declaration::OriginalPtr::get()
{
    return IntPtr(((::CppSharp::CppParser::AST::Declaration*)NativePtr)->OriginalPtr);
}

void CppSharp::Parser::AST::Declaration::OriginalPtr::set(System::IntPtr value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->OriginalPtr = (void*)value.ToPointer();
}

CppSharp::Parser::AST::DeclarationContext::DeclarationContext(::CppSharp::CppParser::AST::DeclarationContext* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::DeclarationContext::DeclarationContext(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::DeclarationContext*)native.ToPointer();
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::DeclarationContext::FindAnonymous(unsigned long long key)
{
    auto arg0 = (::uint64_t)key;
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindAnonymous(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)__ret);
}

CppSharp::Parser::AST::Namespace^ CppSharp::Parser::AST::DeclarationContext::FindNamespace(System::String^ Name)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindNamespace(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)__ret);
}

CppSharp::Parser::AST::Namespace^ CppSharp::Parser::AST::DeclarationContext::FindNamespace(System::Collections::Generic::List<System::String^>^ _0)
{
    auto _tmp_0 = std::vector<::std::string>();
    for each(System::String^ _element in _0)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmp_0.push_back(_marshalElement);
    }
    auto arg0 = _tmp_0;
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindNamespace(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)__ret);
}

CppSharp::Parser::AST::Namespace^ CppSharp::Parser::AST::DeclarationContext::FindCreateNamespace(System::String^ Name)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindCreateNamespace(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)__ret);
}

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::DeclarationContext::CreateClass(System::String^ Name, bool IsComplete)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->CreateClass(arg0, IsComplete);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)__ret);
}

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::DeclarationContext::FindClass(System::String^ Name)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindClass(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)__ret);
}

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::DeclarationContext::FindClass(System::String^ Name, bool IsComplete, bool Create)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindClass(arg0, IsComplete, Create);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)__ret);
}

CppSharp::Parser::AST::Enumeration^ CppSharp::Parser::AST::DeclarationContext::FindEnum(System::String^ Name, bool Create)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindEnum(arg0, Create);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Enumeration((::CppSharp::CppParser::AST::Enumeration*)__ret);
}

CppSharp::Parser::AST::Function^ CppSharp::Parser::AST::DeclarationContext::FindFunction(System::String^ Name, bool Create)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindFunction(arg0, Create);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)__ret);
}

CppSharp::Parser::AST::TypedefDecl^ CppSharp::Parser::AST::DeclarationContext::FindTypedef(System::String^ Name, bool Create)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(Name);
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->FindTypedef(arg0, Create);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::TypedefDecl((::CppSharp::CppParser::AST::TypedefDecl*)__ret);
}

CppSharp::Parser::AST::Namespace^ CppSharp::Parser::AST::DeclarationContext::getNamespaces(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getNamespaces(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)__ret);
}

unsigned int CppSharp::Parser::AST::DeclarationContext::getNamespacesCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getNamespacesCount();
    return __ret;
}

CppSharp::Parser::AST::Enumeration^ CppSharp::Parser::AST::DeclarationContext::getEnums(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getEnums(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Enumeration((::CppSharp::CppParser::AST::Enumeration*)__ret);
}

unsigned int CppSharp::Parser::AST::DeclarationContext::getEnumsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getEnumsCount();
    return __ret;
}

CppSharp::Parser::AST::Function^ CppSharp::Parser::AST::DeclarationContext::getFunctions(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getFunctions(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)__ret);
}

unsigned int CppSharp::Parser::AST::DeclarationContext::getFunctionsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getFunctionsCount();
    return __ret;
}

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::DeclarationContext::getClasses(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getClasses(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)__ret);
}

unsigned int CppSharp::Parser::AST::DeclarationContext::getClassesCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getClassesCount();
    return __ret;
}

CppSharp::Parser::AST::Template^ CppSharp::Parser::AST::DeclarationContext::getTemplates(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getTemplates(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)__ret);
}

unsigned int CppSharp::Parser::AST::DeclarationContext::getTemplatesCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getTemplatesCount();
    return __ret;
}

CppSharp::Parser::AST::TypedefDecl^ CppSharp::Parser::AST::DeclarationContext::getTypedefs(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getTypedefs(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::TypedefDecl((::CppSharp::CppParser::AST::TypedefDecl*)__ret);
}

unsigned int CppSharp::Parser::AST::DeclarationContext::getTypedefsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getTypedefsCount();
    return __ret;
}

CppSharp::Parser::AST::Variable^ CppSharp::Parser::AST::DeclarationContext::getVariables(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getVariables(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Variable((::CppSharp::CppParser::AST::Variable*)__ret);
}

unsigned int CppSharp::Parser::AST::DeclarationContext::getVariablesCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getVariablesCount();
    return __ret;
}

CppSharp::Parser::AST::DeclarationContext::DeclarationContext()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::DeclarationContext();
}

System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^ CppSharp::Parser::AST::DeclarationContext::Namespaces::get()
{
    auto _tmpNamespaces = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>();
    for(auto _element : ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Namespaces)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)_element);
        _tmpNamespaces->Add(_marshalElement);
    }
    return _tmpNamespaces;
}

void CppSharp::Parser::AST::DeclarationContext::Namespaces::set(System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Namespace*>();
    for each(CppSharp::Parser::AST::Namespace^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Namespace*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Namespaces = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration^>^ CppSharp::Parser::AST::DeclarationContext::Enums::get()
{
    auto _tmpEnums = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration^>();
    for(auto _element : ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Enums)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Enumeration((::CppSharp::CppParser::AST::Enumeration*)_element);
        _tmpEnums->Add(_marshalElement);
    }
    return _tmpEnums;
}

void CppSharp::Parser::AST::DeclarationContext::Enums::set(System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Enumeration*>();
    for each(CppSharp::Parser::AST::Enumeration^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Enumeration*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Enums = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Function^>^ CppSharp::Parser::AST::DeclarationContext::Functions::get()
{
    auto _tmpFunctions = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Function^>();
    for(auto _element : ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Functions)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)_element);
        _tmpFunctions->Add(_marshalElement);
    }
    return _tmpFunctions;
}

void CppSharp::Parser::AST::DeclarationContext::Functions::set(System::Collections::Generic::List<CppSharp::Parser::AST::Function^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Function*>();
    for each(CppSharp::Parser::AST::Function^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Function*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Functions = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Class^>^ CppSharp::Parser::AST::DeclarationContext::Classes::get()
{
    auto _tmpClasses = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Class^>();
    for(auto _element : ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Classes)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)_element);
        _tmpClasses->Add(_marshalElement);
    }
    return _tmpClasses;
}

void CppSharp::Parser::AST::DeclarationContext::Classes::set(System::Collections::Generic::List<CppSharp::Parser::AST::Class^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Class*>();
    for each(CppSharp::Parser::AST::Class^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Class*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Classes = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Template^>^ CppSharp::Parser::AST::DeclarationContext::Templates::get()
{
    auto _tmpTemplates = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Template^>();
    for(auto _element : ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Templates)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)_element);
        _tmpTemplates->Add(_marshalElement);
    }
    return _tmpTemplates;
}

void CppSharp::Parser::AST::DeclarationContext::Templates::set(System::Collections::Generic::List<CppSharp::Parser::AST::Template^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Template*>();
    for each(CppSharp::Parser::AST::Template^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Template*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Templates = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::TypedefDecl^>^ CppSharp::Parser::AST::DeclarationContext::Typedefs::get()
{
    auto _tmpTypedefs = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::TypedefDecl^>();
    for(auto _element : ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Typedefs)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::TypedefDecl((::CppSharp::CppParser::AST::TypedefDecl*)_element);
        _tmpTypedefs->Add(_marshalElement);
    }
    return _tmpTypedefs;
}

void CppSharp::Parser::AST::DeclarationContext::Typedefs::set(System::Collections::Generic::List<CppSharp::Parser::AST::TypedefDecl^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::TypedefDecl*>();
    for each(CppSharp::Parser::AST::TypedefDecl^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::TypedefDecl*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Typedefs = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Variable^>^ CppSharp::Parser::AST::DeclarationContext::Variables::get()
{
    auto _tmpVariables = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Variable^>();
    for(auto _element : ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Variables)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Variable((::CppSharp::CppParser::AST::Variable*)_element);
        _tmpVariables->Add(_marshalElement);
    }
    return _tmpVariables;
}

void CppSharp::Parser::AST::DeclarationContext::Variables::set(System::Collections::Generic::List<CppSharp::Parser::AST::Variable^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Variable*>();
    for each(CppSharp::Parser::AST::Variable^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Variable*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->Variables = _tmpvalue;
}

CppSharp::Parser::AST::TypedefDecl::TypedefDecl(::CppSharp::CppParser::AST::TypedefDecl* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::TypedefDecl::TypedefDecl(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::TypedefDecl*)native.ToPointer();
}

CppSharp::Parser::AST::TypedefDecl::TypedefDecl()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TypedefDecl();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::TypedefDecl::QualifiedType::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::TypedefDecl*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::AST::TypedefDecl::QualifiedType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::TypedefDecl*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::Parameter::Parameter(::CppSharp::CppParser::AST::Parameter* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Parameter::Parameter(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::Parameter*)native.ToPointer();
}

CppSharp::Parser::AST::Parameter::Parameter()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Parameter();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Parameter::QualifiedType::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Parameter*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::AST::Parameter::QualifiedType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::Parameter*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

bool CppSharp::Parser::AST::Parameter::IsIndirect::get()
{
    return ((::CppSharp::CppParser::AST::Parameter*)NativePtr)->IsIndirect;
}

void CppSharp::Parser::AST::Parameter::IsIndirect::set(bool value)
{
    ((::CppSharp::CppParser::AST::Parameter*)NativePtr)->IsIndirect = value;
}

bool CppSharp::Parser::AST::Parameter::HasDefaultValue::get()
{
    return ((::CppSharp::CppParser::AST::Parameter*)NativePtr)->HasDefaultValue;
}

void CppSharp::Parser::AST::Parameter::HasDefaultValue::set(bool value)
{
    ((::CppSharp::CppParser::AST::Parameter*)NativePtr)->HasDefaultValue = value;
}

CppSharp::Parser::AST::Function::Function(::CppSharp::CppParser::AST::Function* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Function::Function(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::Function*)native.ToPointer();
}

CppSharp::Parser::AST::Function::Function()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Function();
}

CppSharp::Parser::AST::Parameter^ CppSharp::Parser::AST::Function::getParameters(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Function*)NativePtr)->getParameters(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Parameter((::CppSharp::CppParser::AST::Parameter*)__ret);
}

unsigned int CppSharp::Parser::AST::Function::getParametersCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::Function*)NativePtr)->getParametersCount();
    return __ret;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Function::ReturnType::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Function*)NativePtr)->ReturnType);
}

void CppSharp::Parser::AST::Function::ReturnType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->ReturnType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

bool CppSharp::Parser::AST::Function::IsReturnIndirect::get()
{
    return ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsReturnIndirect;
}

void CppSharp::Parser::AST::Function::IsReturnIndirect::set(bool value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsReturnIndirect = value;
}

bool CppSharp::Parser::AST::Function::IsVariadic::get()
{
    return ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsVariadic;
}

void CppSharp::Parser::AST::Function::IsVariadic::set(bool value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsVariadic = value;
}

bool CppSharp::Parser::AST::Function::IsInline::get()
{
    return ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsInline;
}

void CppSharp::Parser::AST::Function::IsInline::set(bool value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsInline = value;
}

bool CppSharp::Parser::AST::Function::IsPure::get()
{
    return ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsPure;
}

void CppSharp::Parser::AST::Function::IsPure::set(bool value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsPure = value;
}

bool CppSharp::Parser::AST::Function::IsDeleted::get()
{
    return ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsDeleted;
}

void CppSharp::Parser::AST::Function::IsDeleted::set(bool value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->IsDeleted = value;
}

CppSharp::Parser::AST::CXXOperatorKind CppSharp::Parser::AST::Function::OperatorKind::get()
{
    return (CppSharp::Parser::AST::CXXOperatorKind)((::CppSharp::CppParser::AST::Function*)NativePtr)->OperatorKind;
}

void CppSharp::Parser::AST::Function::OperatorKind::set(CppSharp::Parser::AST::CXXOperatorKind value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->OperatorKind = (::CppSharp::CppParser::AST::CXXOperatorKind)value;
}

System::String^ CppSharp::Parser::AST::Function::Mangled::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::Function*)NativePtr)->Mangled);
}

void CppSharp::Parser::AST::Function::Mangled::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->Mangled = clix::marshalString<clix::E_UTF8>(value);
}

System::String^ CppSharp::Parser::AST::Function::Signature::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::Function*)NativePtr)->Signature);
}

void CppSharp::Parser::AST::Function::Signature::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->Signature = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::AST::CallingConvention CppSharp::Parser::AST::Function::CallingConvention::get()
{
    return (CppSharp::Parser::AST::CallingConvention)((::CppSharp::CppParser::AST::Function*)NativePtr)->CallingConvention;
}

void CppSharp::Parser::AST::Function::CallingConvention::set(CppSharp::Parser::AST::CallingConvention value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->CallingConvention = (::CppSharp::CppParser::AST::CallingConvention)value;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^ CppSharp::Parser::AST::Function::Parameters::get()
{
    auto _tmpParameters = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>();
    for(auto _element : ((::CppSharp::CppParser::AST::Function*)NativePtr)->Parameters)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Parameter((::CppSharp::CppParser::AST::Parameter*)_element);
        _tmpParameters->Add(_marshalElement);
    }
    return _tmpParameters;
}

void CppSharp::Parser::AST::Function::Parameters::set(System::Collections::Generic::List<CppSharp::Parser::AST::Parameter^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Parameter*>();
    for each(CppSharp::Parser::AST::Parameter^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Parameter*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->Parameters = _tmpvalue;
}

CppSharp::Parser::AST::Method::Method(::CppSharp::CppParser::AST::Method* native)
    : CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)native)
{
}

CppSharp::Parser::AST::Method::Method(System::IntPtr native)
    : CppSharp::Parser::AST::Function(native)
{
    auto __native = (::CppSharp::CppParser::AST::Method*)native.ToPointer();
}

CppSharp::Parser::AST::Method::Method()
    : CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Method();
}

CppSharp::Parser::AST::AccessSpecifierDecl^ CppSharp::Parser::AST::Method::AccessDecl::get()
{
    return gcnew CppSharp::Parser::AST::AccessSpecifierDecl((::CppSharp::CppParser::AST::AccessSpecifierDecl*)((::CppSharp::CppParser::AST::Method*)NativePtr)->AccessDecl);
}

void CppSharp::Parser::AST::Method::AccessDecl::set(CppSharp::Parser::AST::AccessSpecifierDecl^ value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->AccessDecl = (::CppSharp::CppParser::AST::AccessSpecifierDecl*)value->NativePtr;
}

bool CppSharp::Parser::AST::Method::IsVirtual::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsVirtual;
}

void CppSharp::Parser::AST::Method::IsVirtual::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsVirtual = value;
}

bool CppSharp::Parser::AST::Method::IsStatic::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsStatic;
}

void CppSharp::Parser::AST::Method::IsStatic::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsStatic = value;
}

bool CppSharp::Parser::AST::Method::IsConst::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsConst;
}

void CppSharp::Parser::AST::Method::IsConst::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsConst = value;
}

bool CppSharp::Parser::AST::Method::IsImplicit::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsImplicit;
}

void CppSharp::Parser::AST::Method::IsImplicit::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsImplicit = value;
}

bool CppSharp::Parser::AST::Method::IsOverride::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsOverride;
}

void CppSharp::Parser::AST::Method::IsOverride::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsOverride = value;
}

CppSharp::Parser::AST::CXXMethodKind CppSharp::Parser::AST::Method::Kind::get()
{
    return (CppSharp::Parser::AST::CXXMethodKind)((::CppSharp::CppParser::AST::Method*)NativePtr)->Kind;
}

void CppSharp::Parser::AST::Method::Kind::set(CppSharp::Parser::AST::CXXMethodKind value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->Kind = (::CppSharp::CppParser::AST::CXXMethodKind)value;
}

bool CppSharp::Parser::AST::Method::IsDefaultConstructor::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsDefaultConstructor;
}

void CppSharp::Parser::AST::Method::IsDefaultConstructor::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsDefaultConstructor = value;
}

bool CppSharp::Parser::AST::Method::IsCopyConstructor::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsCopyConstructor;
}

void CppSharp::Parser::AST::Method::IsCopyConstructor::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsCopyConstructor = value;
}

bool CppSharp::Parser::AST::Method::IsMoveConstructor::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsMoveConstructor;
}

void CppSharp::Parser::AST::Method::IsMoveConstructor::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsMoveConstructor = value;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Method::ConversionType::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Method*)NativePtr)->ConversionType);
}

void CppSharp::Parser::AST::Method::ConversionType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->ConversionType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::Enumeration::Item::Item(::CppSharp::CppParser::AST::Enumeration::Item* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Enumeration::Item::Item(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::Enumeration::Item*)native.ToPointer();
}

CppSharp::Parser::AST::Enumeration::Item::Item()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Enumeration::Item();
}

System::String^ CppSharp::Parser::AST::Enumeration::Item::Expression::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::Enumeration::Item*)NativePtr)->Expression);
}

void CppSharp::Parser::AST::Enumeration::Item::Expression::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::Enumeration::Item*)NativePtr)->Expression = clix::marshalString<clix::E_UTF8>(value);
}

unsigned long long CppSharp::Parser::AST::Enumeration::Item::Value::get()
{
    return ((::CppSharp::CppParser::AST::Enumeration::Item*)NativePtr)->Value;
}

void CppSharp::Parser::AST::Enumeration::Item::Value::set(unsigned long long value)
{
    ((::CppSharp::CppParser::AST::Enumeration::Item*)NativePtr)->Value = (::uint64_t)value;
}

CppSharp::Parser::AST::Enumeration::Enumeration(::CppSharp::CppParser::AST::Enumeration* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Enumeration::Enumeration(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::Enumeration*)native.ToPointer();
}

CppSharp::Parser::AST::Enumeration::Item^ CppSharp::Parser::AST::Enumeration::getItems(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->getItems(i);
    auto ____ret = new ::CppSharp::CppParser::AST::Enumeration::Item(__ret);
    return gcnew CppSharp::Parser::AST::Enumeration::Item((::CppSharp::CppParser::AST::Enumeration::Item*)____ret);
}

unsigned int CppSharp::Parser::AST::Enumeration::getItemsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->getItemsCount();
    return __ret;
}

CppSharp::Parser::AST::Enumeration::Enumeration()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Enumeration();
}

CppSharp::Parser::AST::Enumeration::EnumModifiers CppSharp::Parser::AST::Enumeration::Modifiers::get()
{
    return (CppSharp::Parser::AST::Enumeration::EnumModifiers)((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Modifiers;
}

void CppSharp::Parser::AST::Enumeration::Modifiers::set(CppSharp::Parser::AST::Enumeration::EnumModifiers value)
{
    ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Modifiers = (::CppSharp::CppParser::AST::Enumeration::EnumModifiers)value;
}

CppSharp::Parser::AST::Type^ CppSharp::Parser::AST::Enumeration::Type::get()
{
    return gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Type);
}

void CppSharp::Parser::AST::Enumeration::Type::set(CppSharp::Parser::AST::Type^ value)
{
    ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Type = (::CppSharp::CppParser::AST::Type*)value->NativePtr;
}

CppSharp::Parser::AST::BuiltinType^ CppSharp::Parser::AST::Enumeration::BuiltinType::get()
{
    return gcnew CppSharp::Parser::AST::BuiltinType((::CppSharp::CppParser::AST::BuiltinType*)((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->BuiltinType);
}

void CppSharp::Parser::AST::Enumeration::BuiltinType::set(CppSharp::Parser::AST::BuiltinType^ value)
{
    ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->BuiltinType = (::CppSharp::CppParser::AST::BuiltinType*)value->NativePtr;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration::Item^>^ CppSharp::Parser::AST::Enumeration::Items::get()
{
    auto _tmpItems = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration::Item^>();
    for(auto _element : ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Items)
    {
        auto ___element = new ::CppSharp::CppParser::AST::Enumeration::Item(_element);
        auto _marshalElement = gcnew CppSharp::Parser::AST::Enumeration::Item((::CppSharp::CppParser::AST::Enumeration::Item*)___element);
        _tmpItems->Add(_marshalElement);
    }
    return _tmpItems;
}

void CppSharp::Parser::AST::Enumeration::Items::set(System::Collections::Generic::List<CppSharp::Parser::AST::Enumeration::Item^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Enumeration::Item>();
    for each(CppSharp::Parser::AST::Enumeration::Item^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::AST::Enumeration::Item*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Items = _tmpvalue;
}

CppSharp::Parser::AST::Variable::Variable(::CppSharp::CppParser::AST::Variable* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Variable::Variable(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::Variable*)native.ToPointer();
}

CppSharp::Parser::AST::Variable::Variable()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Variable();
}

System::String^ CppSharp::Parser::AST::Variable::Mangled::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::Variable*)NativePtr)->Mangled);
}

void CppSharp::Parser::AST::Variable::Mangled::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::Variable*)NativePtr)->Mangled = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Variable::QualifiedType::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Variable*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::AST::Variable::QualifiedType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::Variable*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::BaseClassSpecifier::BaseClassSpecifier(::CppSharp::CppParser::AST::BaseClassSpecifier* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::BaseClassSpecifier::BaseClassSpecifier(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::BaseClassSpecifier*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::BaseClassSpecifier::BaseClassSpecifier()
{
    NativePtr = new ::CppSharp::CppParser::AST::BaseClassSpecifier();
}

System::IntPtr CppSharp::Parser::AST::BaseClassSpecifier::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::BaseClassSpecifier::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::BaseClassSpecifier*)object.ToPointer();
}

CppSharp::Parser::AST::AccessSpecifier CppSharp::Parser::AST::BaseClassSpecifier::Access::get()
{
    return (CppSharp::Parser::AST::AccessSpecifier)((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Access;
}

void CppSharp::Parser::AST::BaseClassSpecifier::Access::set(CppSharp::Parser::AST::AccessSpecifier value)
{
    ((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Access = (::CppSharp::CppParser::AST::AccessSpecifier)value;
}

bool CppSharp::Parser::AST::BaseClassSpecifier::IsVirtual::get()
{
    return ((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->IsVirtual;
}

void CppSharp::Parser::AST::BaseClassSpecifier::IsVirtual::set(bool value)
{
    ((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->IsVirtual = value;
}

CppSharp::Parser::AST::Type^ CppSharp::Parser::AST::BaseClassSpecifier::Type::get()
{
    return gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Type);
}

void CppSharp::Parser::AST::BaseClassSpecifier::Type::set(CppSharp::Parser::AST::Type^ value)
{
    ((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Type = (::CppSharp::CppParser::AST::Type*)value->NativePtr;
}

CppSharp::Parser::AST::Field::Field(::CppSharp::CppParser::AST::Field* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Field::Field(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::Field*)native.ToPointer();
}

CppSharp::Parser::AST::Field::Field()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Field();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Field::QualifiedType::get()
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Field*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::AST::Field::QualifiedType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::Field*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::AccessSpecifier CppSharp::Parser::AST::Field::Access::get()
{
    return (CppSharp::Parser::AST::AccessSpecifier)((::CppSharp::CppParser::AST::Field*)NativePtr)->Access;
}

void CppSharp::Parser::AST::Field::Access::set(CppSharp::Parser::AST::AccessSpecifier value)
{
    ((::CppSharp::CppParser::AST::Field*)NativePtr)->Access = (::CppSharp::CppParser::AST::AccessSpecifier)value;
}

unsigned int CppSharp::Parser::AST::Field::Offset::get()
{
    return ((::CppSharp::CppParser::AST::Field*)NativePtr)->Offset;
}

void CppSharp::Parser::AST::Field::Offset::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::Field*)NativePtr)->Offset = value;
}

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::Field::Class::get()
{
    return gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)((::CppSharp::CppParser::AST::Field*)NativePtr)->Class);
}

void CppSharp::Parser::AST::Field::Class::set(CppSharp::Parser::AST::Class^ value)
{
    ((::CppSharp::CppParser::AST::Field*)NativePtr)->Class = (::CppSharp::CppParser::AST::Class*)value->NativePtr;
}

CppSharp::Parser::AST::AccessSpecifierDecl::AccessSpecifierDecl(::CppSharp::CppParser::AST::AccessSpecifierDecl* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::AccessSpecifierDecl::AccessSpecifierDecl(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::AccessSpecifierDecl*)native.ToPointer();
}

CppSharp::Parser::AST::AccessSpecifierDecl::AccessSpecifierDecl()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::AccessSpecifierDecl();
}

CppSharp::Parser::AST::Class::Class(::CppSharp::CppParser::AST::Class* native)
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)native)
{
}

CppSharp::Parser::AST::Class::Class(System::IntPtr native)
    : CppSharp::Parser::AST::DeclarationContext(native)
{
    auto __native = (::CppSharp::CppParser::AST::Class*)native.ToPointer();
}

CppSharp::Parser::AST::BaseClassSpecifier^ CppSharp::Parser::AST::Class::getBases(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getBases(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::BaseClassSpecifier((::CppSharp::CppParser::AST::BaseClassSpecifier*)__ret);
}

unsigned int CppSharp::Parser::AST::Class::getBasesCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getBasesCount();
    return __ret;
}

CppSharp::Parser::AST::Field^ CppSharp::Parser::AST::Class::getFields(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getFields(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Field((::CppSharp::CppParser::AST::Field*)__ret);
}

unsigned int CppSharp::Parser::AST::Class::getFieldsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getFieldsCount();
    return __ret;
}

CppSharp::Parser::AST::Method^ CppSharp::Parser::AST::Class::getMethods(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getMethods(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Method((::CppSharp::CppParser::AST::Method*)__ret);
}

unsigned int CppSharp::Parser::AST::Class::getMethodsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getMethodsCount();
    return __ret;
}

CppSharp::Parser::AST::AccessSpecifierDecl^ CppSharp::Parser::AST::Class::getSpecifiers(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getSpecifiers(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::AccessSpecifierDecl((::CppSharp::CppParser::AST::AccessSpecifierDecl*)__ret);
}

unsigned int CppSharp::Parser::AST::Class::getSpecifiersCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getSpecifiersCount();
    return __ret;
}

CppSharp::Parser::AST::Class::Class()
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Class();
}

System::Collections::Generic::List<CppSharp::Parser::AST::BaseClassSpecifier^>^ CppSharp::Parser::AST::Class::Bases::get()
{
    auto _tmpBases = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::BaseClassSpecifier^>();
    for(auto _element : ((::CppSharp::CppParser::AST::Class*)NativePtr)->Bases)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::BaseClassSpecifier((::CppSharp::CppParser::AST::BaseClassSpecifier*)_element);
        _tmpBases->Add(_marshalElement);
    }
    return _tmpBases;
}

void CppSharp::Parser::AST::Class::Bases::set(System::Collections::Generic::List<CppSharp::Parser::AST::BaseClassSpecifier^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::BaseClassSpecifier*>();
    for each(CppSharp::Parser::AST::BaseClassSpecifier^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::BaseClassSpecifier*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->Bases = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Field^>^ CppSharp::Parser::AST::Class::Fields::get()
{
    auto _tmpFields = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Field^>();
    for(auto _element : ((::CppSharp::CppParser::AST::Class*)NativePtr)->Fields)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Field((::CppSharp::CppParser::AST::Field*)_element);
        _tmpFields->Add(_marshalElement);
    }
    return _tmpFields;
}

void CppSharp::Parser::AST::Class::Fields::set(System::Collections::Generic::List<CppSharp::Parser::AST::Field^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Field*>();
    for each(CppSharp::Parser::AST::Field^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Field*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->Fields = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Method^>^ CppSharp::Parser::AST::Class::Methods::get()
{
    auto _tmpMethods = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Method^>();
    for(auto _element : ((::CppSharp::CppParser::AST::Class*)NativePtr)->Methods)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Method((::CppSharp::CppParser::AST::Method*)_element);
        _tmpMethods->Add(_marshalElement);
    }
    return _tmpMethods;
}

void CppSharp::Parser::AST::Class::Methods::set(System::Collections::Generic::List<CppSharp::Parser::AST::Method^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Method*>();
    for each(CppSharp::Parser::AST::Method^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Method*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->Methods = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::AccessSpecifierDecl^>^ CppSharp::Parser::AST::Class::Specifiers::get()
{
    auto _tmpSpecifiers = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::AccessSpecifierDecl^>();
    for(auto _element : ((::CppSharp::CppParser::AST::Class*)NativePtr)->Specifiers)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::AccessSpecifierDecl((::CppSharp::CppParser::AST::AccessSpecifierDecl*)_element);
        _tmpSpecifiers->Add(_marshalElement);
    }
    return _tmpSpecifiers;
}

void CppSharp::Parser::AST::Class::Specifiers::set(System::Collections::Generic::List<CppSharp::Parser::AST::AccessSpecifierDecl^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::AccessSpecifierDecl*>();
    for each(CppSharp::Parser::AST::AccessSpecifierDecl^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::AccessSpecifierDecl*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->Specifiers = _tmpvalue;
}

bool CppSharp::Parser::AST::Class::IsPOD::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsPOD;
}

void CppSharp::Parser::AST::Class::IsPOD::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsPOD = value;
}

bool CppSharp::Parser::AST::Class::IsAbstract::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsAbstract;
}

void CppSharp::Parser::AST::Class::IsAbstract::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsAbstract = value;
}

bool CppSharp::Parser::AST::Class::IsUnion::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsUnion;
}

void CppSharp::Parser::AST::Class::IsUnion::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsUnion = value;
}

bool CppSharp::Parser::AST::Class::IsDynamic::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsDynamic;
}

void CppSharp::Parser::AST::Class::IsDynamic::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsDynamic = value;
}

bool CppSharp::Parser::AST::Class::IsPolymorphic::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsPolymorphic;
}

void CppSharp::Parser::AST::Class::IsPolymorphic::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsPolymorphic = value;
}

bool CppSharp::Parser::AST::Class::HasNonTrivialDefaultConstructor::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->HasNonTrivialDefaultConstructor;
}

void CppSharp::Parser::AST::Class::HasNonTrivialDefaultConstructor::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->HasNonTrivialDefaultConstructor = value;
}

bool CppSharp::Parser::AST::Class::HasNonTrivialCopyConstructor::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->HasNonTrivialCopyConstructor;
}

void CppSharp::Parser::AST::Class::HasNonTrivialCopyConstructor::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->HasNonTrivialCopyConstructor = value;
}

CppSharp::Parser::AST::ClassLayout^ CppSharp::Parser::AST::Class::Layout::get()
{
    return gcnew CppSharp::Parser::AST::ClassLayout((::CppSharp::CppParser::AST::ClassLayout*)&((::CppSharp::CppParser::AST::Class*)NativePtr)->Layout);
}

void CppSharp::Parser::AST::Class::Layout::set(CppSharp::Parser::AST::ClassLayout^ value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->Layout = *(::CppSharp::CppParser::AST::ClassLayout*)value->NativePtr;
}

CppSharp::Parser::AST::Template::Template(::CppSharp::CppParser::AST::Template* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Template::Template(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::Template*)native.ToPointer();
}

CppSharp::Parser::AST::TemplateParameter^ CppSharp::Parser::AST::Template::getParameters(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Template*)NativePtr)->getParameters(i);
    auto ____ret = new ::CppSharp::CppParser::AST::TemplateParameter(__ret);
    return gcnew CppSharp::Parser::AST::TemplateParameter((::CppSharp::CppParser::AST::TemplateParameter*)____ret);
}

unsigned int CppSharp::Parser::AST::Template::getParametersCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::Template*)NativePtr)->getParametersCount();
    return __ret;
}

CppSharp::Parser::AST::Template::Template()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Template();
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::Template::TemplatedDecl::get()
{
    return gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::Template*)NativePtr)->TemplatedDecl);
}

void CppSharp::Parser::AST::Template::TemplatedDecl::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::Template*)NativePtr)->TemplatedDecl = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

System::Collections::Generic::List<CppSharp::Parser::AST::TemplateParameter^>^ CppSharp::Parser::AST::Template::Parameters::get()
{
    auto _tmpParameters = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::TemplateParameter^>();
    for(auto _element : ((::CppSharp::CppParser::AST::Template*)NativePtr)->Parameters)
    {
        auto ___element = new ::CppSharp::CppParser::AST::TemplateParameter(_element);
        auto _marshalElement = gcnew CppSharp::Parser::AST::TemplateParameter((::CppSharp::CppParser::AST::TemplateParameter*)___element);
        _tmpParameters->Add(_marshalElement);
    }
    return _tmpParameters;
}

void CppSharp::Parser::AST::Template::Parameters::set(System::Collections::Generic::List<CppSharp::Parser::AST::TemplateParameter^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::TemplateParameter>();
    for each(CppSharp::Parser::AST::TemplateParameter^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::AST::TemplateParameter*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::Template*)NativePtr)->Parameters = _tmpvalue;
}

CppSharp::Parser::AST::ClassTemplate::ClassTemplate(::CppSharp::CppParser::AST::ClassTemplate* native)
    : CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)native)
{
}

CppSharp::Parser::AST::ClassTemplate::ClassTemplate(System::IntPtr native)
    : CppSharp::Parser::AST::Template(native)
{
    auto __native = (::CppSharp::CppParser::AST::ClassTemplate*)native.ToPointer();
}

CppSharp::Parser::AST::ClassTemplate::ClassTemplate()
    : CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::ClassTemplate();
}

CppSharp::Parser::AST::ClassTemplateSpecialization::ClassTemplateSpecialization(::CppSharp::CppParser::AST::ClassTemplateSpecialization* native)
    : CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)native)
{
}

CppSharp::Parser::AST::ClassTemplateSpecialization::ClassTemplateSpecialization(System::IntPtr native)
    : CppSharp::Parser::AST::Class(native)
{
    auto __native = (::CppSharp::CppParser::AST::ClassTemplateSpecialization*)native.ToPointer();
}

CppSharp::Parser::AST::ClassTemplateSpecialization::ClassTemplateSpecialization()
    : CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::ClassTemplateSpecialization();
}

CppSharp::Parser::AST::ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization(::CppSharp::CppParser::AST::ClassTemplatePartialSpecialization* native)
    : CppSharp::Parser::AST::ClassTemplateSpecialization((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)native)
{
}

CppSharp::Parser::AST::ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization(System::IntPtr native)
    : CppSharp::Parser::AST::ClassTemplateSpecialization(native)
{
    auto __native = (::CppSharp::CppParser::AST::ClassTemplatePartialSpecialization*)native.ToPointer();
}

CppSharp::Parser::AST::ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization()
    : CppSharp::Parser::AST::ClassTemplateSpecialization((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::ClassTemplatePartialSpecialization();
}

CppSharp::Parser::AST::FunctionTemplate::FunctionTemplate(::CppSharp::CppParser::AST::FunctionTemplate* native)
    : CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)native)
{
}

CppSharp::Parser::AST::FunctionTemplate::FunctionTemplate(System::IntPtr native)
    : CppSharp::Parser::AST::Template(native)
{
    auto __native = (::CppSharp::CppParser::AST::FunctionTemplate*)native.ToPointer();
}

CppSharp::Parser::AST::FunctionTemplate::FunctionTemplate()
    : CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::FunctionTemplate();
}

CppSharp::Parser::AST::Namespace::Namespace(::CppSharp::CppParser::AST::Namespace* native)
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)native)
{
}

CppSharp::Parser::AST::Namespace::Namespace(System::IntPtr native)
    : CppSharp::Parser::AST::DeclarationContext(native)
{
    auto __native = (::CppSharp::CppParser::AST::Namespace*)native.ToPointer();
}

CppSharp::Parser::AST::Namespace::Namespace()
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Namespace();
}

CppSharp::Parser::AST::PreprocessedEntity::PreprocessedEntity(::CppSharp::CppParser::AST::PreprocessedEntity* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::PreprocessedEntity::PreprocessedEntity(System::IntPtr native)
    : CppSharp::Parser::AST::Declaration(native)
{
    auto __native = (::CppSharp::CppParser::AST::PreprocessedEntity*)native.ToPointer();
}

CppSharp::Parser::AST::PreprocessedEntity::PreprocessedEntity()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::PreprocessedEntity();
}

CppSharp::Parser::AST::MacroLocation CppSharp::Parser::AST::PreprocessedEntity::Location::get()
{
    return (CppSharp::Parser::AST::MacroLocation)((::CppSharp::CppParser::AST::PreprocessedEntity*)NativePtr)->Location;
}

void CppSharp::Parser::AST::PreprocessedEntity::Location::set(CppSharp::Parser::AST::MacroLocation value)
{
    ((::CppSharp::CppParser::AST::PreprocessedEntity*)NativePtr)->Location = (::CppSharp::CppParser::AST::MacroLocation)value;
}

CppSharp::Parser::AST::MacroDefinition::MacroDefinition(::CppSharp::CppParser::AST::MacroDefinition* native)
    : CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)native)
{
}

CppSharp::Parser::AST::MacroDefinition::MacroDefinition(System::IntPtr native)
    : CppSharp::Parser::AST::PreprocessedEntity(native)
{
    auto __native = (::CppSharp::CppParser::AST::MacroDefinition*)native.ToPointer();
}

CppSharp::Parser::AST::MacroDefinition::MacroDefinition()
    : CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::MacroDefinition();
}

System::String^ CppSharp::Parser::AST::MacroDefinition::Expression::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::MacroDefinition*)NativePtr)->Expression);
}

void CppSharp::Parser::AST::MacroDefinition::Expression::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::MacroDefinition*)NativePtr)->Expression = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::AST::MacroExpansion::MacroExpansion(::CppSharp::CppParser::AST::MacroExpansion* native)
    : CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)native)
{
}

CppSharp::Parser::AST::MacroExpansion::MacroExpansion(System::IntPtr native)
    : CppSharp::Parser::AST::PreprocessedEntity(native)
{
    auto __native = (::CppSharp::CppParser::AST::MacroExpansion*)native.ToPointer();
}

CppSharp::Parser::AST::MacroExpansion::MacroExpansion()
    : CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::MacroExpansion();
}

System::String^ CppSharp::Parser::AST::MacroExpansion::Text::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->Text);
}

void CppSharp::Parser::AST::MacroExpansion::Text::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->Text = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::AST::MacroDefinition^ CppSharp::Parser::AST::MacroExpansion::Definition::get()
{
    return gcnew CppSharp::Parser::AST::MacroDefinition((::CppSharp::CppParser::AST::MacroDefinition*)((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->Definition);
}

void CppSharp::Parser::AST::MacroExpansion::Definition::set(CppSharp::Parser::AST::MacroDefinition^ value)
{
    ((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->Definition = (::CppSharp::CppParser::AST::MacroDefinition*)value->NativePtr;
}

CppSharp::Parser::AST::TranslationUnit::TranslationUnit(::CppSharp::CppParser::AST::TranslationUnit* native)
    : CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)native)
{
}

CppSharp::Parser::AST::TranslationUnit::TranslationUnit(System::IntPtr native)
    : CppSharp::Parser::AST::Namespace(native)
{
    auto __native = (::CppSharp::CppParser::AST::TranslationUnit*)native.ToPointer();
}

CppSharp::Parser::AST::Namespace^ CppSharp::Parser::AST::TranslationUnit::getNamespaces(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->getNamespaces(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)__ret);
}

unsigned int CppSharp::Parser::AST::TranslationUnit::getNamespacesCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->getNamespacesCount();
    return __ret;
}

CppSharp::Parser::AST::MacroDefinition^ CppSharp::Parser::AST::TranslationUnit::getMacros(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->getMacros(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::MacroDefinition((::CppSharp::CppParser::AST::MacroDefinition*)__ret);
}

unsigned int CppSharp::Parser::AST::TranslationUnit::getMacrosCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->getMacrosCount();
    return __ret;
}

CppSharp::Parser::AST::TranslationUnit::TranslationUnit()
    : CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TranslationUnit();
}

System::String^ CppSharp::Parser::AST::TranslationUnit::FileName::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->FileName);
}

void CppSharp::Parser::AST::TranslationUnit::FileName::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->FileName = clix::marshalString<clix::E_UTF8>(value);
}

bool CppSharp::Parser::AST::TranslationUnit::IsSystemHeader::get()
{
    return ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->IsSystemHeader;
}

void CppSharp::Parser::AST::TranslationUnit::IsSystemHeader::set(bool value)
{
    ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->IsSystemHeader = value;
}

System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^ CppSharp::Parser::AST::TranslationUnit::Namespaces::get()
{
    auto _tmpNamespaces = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>();
    for(auto _element : ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->Namespaces)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)_element);
        _tmpNamespaces->Add(_marshalElement);
    }
    return _tmpNamespaces;
}

void CppSharp::Parser::AST::TranslationUnit::Namespaces::set(System::Collections::Generic::List<CppSharp::Parser::AST::Namespace^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::Namespace*>();
    for each(CppSharp::Parser::AST::Namespace^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::Namespace*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->Namespaces = _tmpvalue;
}

System::Collections::Generic::List<CppSharp::Parser::AST::MacroDefinition^>^ CppSharp::Parser::AST::TranslationUnit::Macros::get()
{
    auto _tmpMacros = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::MacroDefinition^>();
    for(auto _element : ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->Macros)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::MacroDefinition((::CppSharp::CppParser::AST::MacroDefinition*)_element);
        _tmpMacros->Add(_marshalElement);
    }
    return _tmpMacros;
}

void CppSharp::Parser::AST::TranslationUnit::Macros::set(System::Collections::Generic::List<CppSharp::Parser::AST::MacroDefinition^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::MacroDefinition*>();
    for each(CppSharp::Parser::AST::MacroDefinition^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::MacroDefinition*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->Macros = _tmpvalue;
}

CppSharp::Parser::AST::NativeLibrary::NativeLibrary(::CppSharp::CppParser::AST::NativeLibrary* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::NativeLibrary::NativeLibrary(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::NativeLibrary*)native.ToPointer();
    NativePtr = __native;
}

System::String^ CppSharp::Parser::AST::NativeLibrary::getSymbols(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->getSymbols(i);
    return clix::marshalString<clix::E_UTF8>(__ret);
}

unsigned int CppSharp::Parser::AST::NativeLibrary::getSymbolsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->getSymbolsCount();
    return __ret;
}

CppSharp::Parser::AST::NativeLibrary::NativeLibrary()
{
    NativePtr = new ::CppSharp::CppParser::AST::NativeLibrary();
}

System::IntPtr CppSharp::Parser::AST::NativeLibrary::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::NativeLibrary::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::NativeLibrary*)object.ToPointer();
}

System::String^ CppSharp::Parser::AST::NativeLibrary::FileName::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->FileName);
}

void CppSharp::Parser::AST::NativeLibrary::FileName::set(System::String^ value)
{
    ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->FileName = clix::marshalString<clix::E_UTF8>(value);
}

System::Collections::Generic::List<System::String^>^ CppSharp::Parser::AST::NativeLibrary::Symbols::get()
{
    auto _tmpSymbols = gcnew System::Collections::Generic::List<System::String^>();
    for(auto _element : ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->Symbols)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpSymbols->Add(_marshalElement);
    }
    return _tmpSymbols;
}

void CppSharp::Parser::AST::NativeLibrary::Symbols::set(System::Collections::Generic::List<System::String^>^ value)
{
    auto _tmpvalue = std::vector<::std::string>();
    for each(System::String^ _element in value)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->Symbols = _tmpvalue;
}

CppSharp::Parser::AST::ASTContext::ASTContext(::CppSharp::CppParser::AST::ASTContext* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::ASTContext::ASTContext(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::AST::ASTContext*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::AST::TranslationUnit^ CppSharp::Parser::AST::ASTContext::FindOrCreateModule(System::String^ File)
{
    auto arg0 = clix::marshalString<clix::E_UTF8>(File);
    auto __ret = ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->FindOrCreateModule(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::TranslationUnit((::CppSharp::CppParser::AST::TranslationUnit*)__ret);
}

CppSharp::Parser::AST::TranslationUnit^ CppSharp::Parser::AST::ASTContext::getTranslationUnits(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->getTranslationUnits(i);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::AST::TranslationUnit((::CppSharp::CppParser::AST::TranslationUnit*)__ret);
}

unsigned int CppSharp::Parser::AST::ASTContext::getTranslationUnitsCount()
{
    auto __ret = ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->getTranslationUnitsCount();
    return __ret;
}

CppSharp::Parser::AST::ASTContext::ASTContext()
{
    NativePtr = new ::CppSharp::CppParser::AST::ASTContext();
}

System::IntPtr CppSharp::Parser::AST::ASTContext::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::ASTContext::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::ASTContext*)object.ToPointer();
}

System::Collections::Generic::List<CppSharp::Parser::AST::TranslationUnit^>^ CppSharp::Parser::AST::ASTContext::TranslationUnits::get()
{
    auto _tmpTranslationUnits = gcnew System::Collections::Generic::List<CppSharp::Parser::AST::TranslationUnit^>();
    for(auto _element : ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->TranslationUnits)
    {
        auto _marshalElement = gcnew CppSharp::Parser::AST::TranslationUnit((::CppSharp::CppParser::AST::TranslationUnit*)_element);
        _tmpTranslationUnits->Add(_marshalElement);
    }
    return _tmpTranslationUnits;
}

void CppSharp::Parser::AST::ASTContext::TranslationUnits::set(System::Collections::Generic::List<CppSharp::Parser::AST::TranslationUnit^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::AST::TranslationUnit*>();
    for each(CppSharp::Parser::AST::TranslationUnit^ _element in value)
    {
        auto _marshalElement = (::CppSharp::CppParser::AST::TranslationUnit*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->TranslationUnits = _tmpvalue;
}

