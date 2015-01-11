#include "AST.h"

using namespace System;
using namespace System::Runtime::InteropServices;

CppSharp::Parser::AST::Type::Type(::CppSharp::CppParser::AST::Type* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::Type^ CppSharp::Parser::AST::Type::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*) native.ToPointer());
}

CppSharp::Parser::AST::Type::Type(CppSharp::Parser::AST::TypeKind kind)
{
    auto arg0 = (::CppSharp::CppParser::AST::TypeKind)kind;
    NativePtr = new ::CppSharp::CppParser::AST::Type(arg0);
}

System::IntPtr CppSharp::Parser::AST::Type::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::Type::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::Type*)object.ToPointer();
}

CppSharp::Parser::AST::TypeKind CppSharp::Parser::AST::Type::Kind::get()
{
    return (CppSharp::Parser::AST::TypeKind)((::CppSharp::CppParser::AST::Type*)NativePtr)->Kind;
}

void CppSharp::Parser::AST::Type::Kind::set(CppSharp::Parser::AST::TypeKind value)
{
    ((::CppSharp::CppParser::AST::Type*)NativePtr)->Kind = (::CppSharp::CppParser::AST::TypeKind)value;
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

CppSharp::Parser::AST::TypeQualifiers^ CppSharp::Parser::AST::TypeQualifiers::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TypeQualifiers((::CppSharp::CppParser::AST::TypeQualifiers*) native.ToPointer());
}

CppSharp::Parser::AST::TypeQualifiers::TypeQualifiers()
{
    NativePtr = new ::CppSharp::CppParser::AST::TypeQualifiers();
}

System::IntPtr CppSharp::Parser::AST::TypeQualifiers::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::TypeQualifiers::__Instance::set(System::IntPtr object)
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

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::QualifiedType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*) native.ToPointer());
}

CppSharp::Parser::AST::QualifiedType::QualifiedType()
{
    NativePtr = new ::CppSharp::CppParser::AST::QualifiedType();
}

System::IntPtr CppSharp::Parser::AST::QualifiedType::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::QualifiedType::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::QualifiedType*)object.ToPointer();
}

CppSharp::Parser::AST::Type^ CppSharp::Parser::AST::QualifiedType::Type::get()
{
    return (((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Type == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Type);
}

void CppSharp::Parser::AST::QualifiedType::Type::set(CppSharp::Parser::AST::Type^ value)
{
    ((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Type = (::CppSharp::CppParser::AST::Type*)value->NativePtr;
}

CppSharp::Parser::AST::TypeQualifiers^ CppSharp::Parser::AST::QualifiedType::Qualifiers::get()
{
    return (&((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Qualifiers == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TypeQualifiers((::CppSharp::CppParser::AST::TypeQualifiers*)&((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Qualifiers);
}

void CppSharp::Parser::AST::QualifiedType::Qualifiers::set(CppSharp::Parser::AST::TypeQualifiers^ value)
{
    ((::CppSharp::CppParser::AST::QualifiedType*)NativePtr)->Qualifiers = *(::CppSharp::CppParser::AST::TypeQualifiers*)value->NativePtr;
}

CppSharp::Parser::AST::TagType::TagType(::CppSharp::CppParser::AST::TagType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TagType^ CppSharp::Parser::AST::TagType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TagType((::CppSharp::CppParser::AST::TagType*) native.ToPointer());
}

CppSharp::Parser::AST::TagType::TagType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TagType();
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::TagType::Declaration::get()
{
    return (((::CppSharp::CppParser::AST::TagType*)NativePtr)->Declaration == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::TagType*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::TagType::Declaration::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::TagType*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

CppSharp::Parser::AST::ArrayType::ArrayType(::CppSharp::CppParser::AST::ArrayType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::ArrayType^ CppSharp::Parser::AST::ArrayType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::ArrayType((::CppSharp::CppParser::AST::ArrayType*) native.ToPointer());
}

CppSharp::Parser::AST::ArrayType::ArrayType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::ArrayType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::ArrayType::QualifiedType::get()
{
    return (&((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->QualifiedType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->QualifiedType);
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

long CppSharp::Parser::AST::ArrayType::Size::get()
{
    return ((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->Size;
}

void CppSharp::Parser::AST::ArrayType::Size::set(long value)
{
    ((::CppSharp::CppParser::AST::ArrayType*)NativePtr)->Size = value;
}

CppSharp::Parser::AST::FunctionType::FunctionType(::CppSharp::CppParser::AST::FunctionType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::FunctionType^ CppSharp::Parser::AST::FunctionType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::FunctionType((::CppSharp::CppParser::AST::FunctionType*) native.ToPointer());
}

CppSharp::Parser::AST::FunctionType::FunctionType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::FunctionType();
}

CppSharp::Parser::AST::Parameter^ CppSharp::Parser::AST::FunctionType::getParameters(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->getParameters(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Parameter((::CppSharp::CppParser::AST::Parameter*)__ret);
}

void CppSharp::Parser::AST::FunctionType::addParameters(CppSharp::Parser::AST::Parameter^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Parameter*)s->NativePtr;
    ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->addParameters(arg0);
}

void CppSharp::Parser::AST::FunctionType::clearParameters()
{
    ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->clearParameters();
}

unsigned int CppSharp::Parser::AST::FunctionType::ParametersCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->getParametersCount();
    return __ret;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::FunctionType::ReturnType::get()
{
    return (&((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->ReturnType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::FunctionType*)NativePtr)->ReturnType);
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

CppSharp::Parser::AST::PointerType::PointerType(::CppSharp::CppParser::AST::PointerType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::PointerType^ CppSharp::Parser::AST::PointerType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::PointerType((::CppSharp::CppParser::AST::PointerType*) native.ToPointer());
}

CppSharp::Parser::AST::PointerType::PointerType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::PointerType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::PointerType::QualifiedPointee::get()
{
    return (&((::CppSharp::CppParser::AST::PointerType*)NativePtr)->QualifiedPointee == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::PointerType*)NativePtr)->QualifiedPointee);
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

CppSharp::Parser::AST::MemberPointerType^ CppSharp::Parser::AST::MemberPointerType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::MemberPointerType((::CppSharp::CppParser::AST::MemberPointerType*) native.ToPointer());
}

CppSharp::Parser::AST::MemberPointerType::MemberPointerType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::MemberPointerType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::MemberPointerType::Pointee::get()
{
    return (&((::CppSharp::CppParser::AST::MemberPointerType*)NativePtr)->Pointee == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::MemberPointerType*)NativePtr)->Pointee);
}

void CppSharp::Parser::AST::MemberPointerType::Pointee::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::MemberPointerType*)NativePtr)->Pointee = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::TypedefType::TypedefType(::CppSharp::CppParser::AST::TypedefType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TypedefType^ CppSharp::Parser::AST::TypedefType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TypedefType((::CppSharp::CppParser::AST::TypedefType*) native.ToPointer());
}

CppSharp::Parser::AST::TypedefType::TypedefType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TypedefType();
}

CppSharp::Parser::AST::TypedefDecl^ CppSharp::Parser::AST::TypedefType::Declaration::get()
{
    return (((::CppSharp::CppParser::AST::TypedefType*)NativePtr)->Declaration == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TypedefDecl((::CppSharp::CppParser::AST::TypedefDecl*)((::CppSharp::CppParser::AST::TypedefType*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::TypedefType::Declaration::set(CppSharp::Parser::AST::TypedefDecl^ value)
{
    ((::CppSharp::CppParser::AST::TypedefType*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::TypedefDecl*)value->NativePtr;
}

CppSharp::Parser::AST::AttributedType::AttributedType(::CppSharp::CppParser::AST::AttributedType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::AttributedType^ CppSharp::Parser::AST::AttributedType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::AttributedType((::CppSharp::CppParser::AST::AttributedType*) native.ToPointer());
}

CppSharp::Parser::AST::AttributedType::AttributedType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::AttributedType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::AttributedType::Modified::get()
{
    return (&((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Modified == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Modified);
}

void CppSharp::Parser::AST::AttributedType::Modified::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Modified = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::AttributedType::Equivalent::get()
{
    return (&((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Equivalent == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Equivalent);
}

void CppSharp::Parser::AST::AttributedType::Equivalent::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::AttributedType*)NativePtr)->Equivalent = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::DecayedType::DecayedType(::CppSharp::CppParser::AST::DecayedType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::DecayedType^ CppSharp::Parser::AST::DecayedType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::DecayedType((::CppSharp::CppParser::AST::DecayedType*) native.ToPointer());
}

CppSharp::Parser::AST::DecayedType::DecayedType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::DecayedType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::DecayedType::Decayed::get()
{
    return (&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Decayed == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Decayed);
}

void CppSharp::Parser::AST::DecayedType::Decayed::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Decayed = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::DecayedType::Original::get()
{
    return (&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Original == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Original);
}

void CppSharp::Parser::AST::DecayedType::Original::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Original = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::DecayedType::Pointee::get()
{
    return (&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Pointee == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Pointee);
}

void CppSharp::Parser::AST::DecayedType::Pointee::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::DecayedType*)NativePtr)->Pointee = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::TemplateArgument::TemplateArgument(::CppSharp::CppParser::AST::TemplateArgument* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::TemplateArgument^ CppSharp::Parser::AST::TemplateArgument::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TemplateArgument((::CppSharp::CppParser::AST::TemplateArgument*) native.ToPointer());
}

CppSharp::Parser::AST::TemplateArgument::TemplateArgument()
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateArgument();
}

System::IntPtr CppSharp::Parser::AST::TemplateArgument::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::TemplateArgument::__Instance::set(System::IntPtr object)
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
    return (&((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Type == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Type);
}

void CppSharp::Parser::AST::TemplateArgument::Type::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Type = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::TemplateArgument::Declaration::get()
{
    return (((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Declaration == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::TemplateArgument::Declaration::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

long CppSharp::Parser::AST::TemplateArgument::Integral::get()
{
    return ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Integral;
}

void CppSharp::Parser::AST::TemplateArgument::Integral::set(long value)
{
    ((::CppSharp::CppParser::AST::TemplateArgument*)NativePtr)->Integral = value;
}

CppSharp::Parser::AST::TemplateSpecializationType::TemplateSpecializationType(::CppSharp::CppParser::AST::TemplateSpecializationType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TemplateSpecializationType^ CppSharp::Parser::AST::TemplateSpecializationType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TemplateSpecializationType((::CppSharp::CppParser::AST::TemplateSpecializationType*) native.ToPointer());
}

CppSharp::Parser::AST::TemplateSpecializationType::TemplateSpecializationType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateSpecializationType();
}

CppSharp::Parser::AST::TemplateArgument^ CppSharp::Parser::AST::TemplateSpecializationType::getArguments(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->getArguments(i);
    auto ____ret = new ::CppSharp::CppParser::AST::TemplateArgument(__ret);
    return (____ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TemplateArgument((::CppSharp::CppParser::AST::TemplateArgument*)____ret);
}

void CppSharp::Parser::AST::TemplateSpecializationType::addArguments(CppSharp::Parser::AST::TemplateArgument^ s)
{
    auto &arg0 = *(::CppSharp::CppParser::AST::TemplateArgument*)s->NativePtr;
    ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->addArguments(arg0);
}

void CppSharp::Parser::AST::TemplateSpecializationType::clearArguments()
{
    ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->clearArguments();
}

unsigned int CppSharp::Parser::AST::TemplateSpecializationType::ArgumentsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->getArgumentsCount();
    return __ret;
}

CppSharp::Parser::AST::Template^ CppSharp::Parser::AST::TemplateSpecializationType::Template::get()
{
    return (((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Template == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Template);
}

void CppSharp::Parser::AST::TemplateSpecializationType::Template::set(CppSharp::Parser::AST::Template^ value)
{
    ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Template = (::CppSharp::CppParser::AST::Template*)value->NativePtr;
}

CppSharp::Parser::AST::Type^ CppSharp::Parser::AST::TemplateSpecializationType::Desugared::get()
{
    return (((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Desugared == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Desugared);
}

void CppSharp::Parser::AST::TemplateSpecializationType::Desugared::set(CppSharp::Parser::AST::Type^ value)
{
    ((::CppSharp::CppParser::AST::TemplateSpecializationType*)NativePtr)->Desugared = (::CppSharp::CppParser::AST::Type*)value->NativePtr;
}

CppSharp::Parser::AST::TemplateParameter::TemplateParameter(::CppSharp::CppParser::AST::TemplateParameter* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::TemplateParameter^ CppSharp::Parser::AST::TemplateParameter::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TemplateParameter((::CppSharp::CppParser::AST::TemplateParameter*) native.ToPointer());
}

CppSharp::Parser::AST::TemplateParameter::TemplateParameter()
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateParameter();
}

bool CppSharp::Parser::AST::TemplateParameter::operator==(CppSharp::Parser::AST::TemplateParameter^ __op, CppSharp::Parser::AST::TemplateParameter^ param)
{
    auto &arg0 = *(::CppSharp::CppParser::AST::TemplateParameter*)__op->NativePtr;
    auto &arg1 = *(::CppSharp::CppParser::AST::TemplateParameter*)param->NativePtr;
    auto __ret = arg0 == arg1;
    return __ret;
}

bool CppSharp::Parser::AST::TemplateParameter::Equals(::System::Object^ obj)
{
    return this == safe_cast<CppSharp::Parser::AST::TemplateParameter^>(obj);
}

System::IntPtr CppSharp::Parser::AST::TemplateParameter::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::TemplateParameter::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::TemplateParameter*)object.ToPointer();
}

System::String^ CppSharp::Parser::AST::TemplateParameter::Name::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::TemplateParameter*)NativePtr)->getName();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::TemplateParameter::Name::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::TemplateParameter*)NativePtr)->setName(arg0);
}

bool CppSharp::Parser::AST::TemplateParameter::IsTypeParameter::get()
{
    return ((::CppSharp::CppParser::AST::TemplateParameter*)NativePtr)->IsTypeParameter;
}

void CppSharp::Parser::AST::TemplateParameter::IsTypeParameter::set(bool value)
{
    ((::CppSharp::CppParser::AST::TemplateParameter*)NativePtr)->IsTypeParameter = value;
}

CppSharp::Parser::AST::TemplateParameterType::TemplateParameterType(::CppSharp::CppParser::AST::TemplateParameterType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TemplateParameterType^ CppSharp::Parser::AST::TemplateParameterType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TemplateParameterType((::CppSharp::CppParser::AST::TemplateParameterType*) native.ToPointer());
}

CppSharp::Parser::AST::TemplateParameterType::TemplateParameterType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateParameterType();
}

CppSharp::Parser::AST::TemplateParameter^ CppSharp::Parser::AST::TemplateParameterType::Parameter::get()
{
    return (&((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Parameter == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TemplateParameter((::CppSharp::CppParser::AST::TemplateParameter*)&((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Parameter);
}

void CppSharp::Parser::AST::TemplateParameterType::Parameter::set(CppSharp::Parser::AST::TemplateParameter^ value)
{
    ((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Parameter = *(::CppSharp::CppParser::AST::TemplateParameter*)value->NativePtr;
}

unsigned int CppSharp::Parser::AST::TemplateParameterType::Depth::get()
{
    return ((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Depth;
}

void CppSharp::Parser::AST::TemplateParameterType::Depth::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Depth = value;
}

unsigned int CppSharp::Parser::AST::TemplateParameterType::Index::get()
{
    return ((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Index;
}

void CppSharp::Parser::AST::TemplateParameterType::Index::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->Index = value;
}

bool CppSharp::Parser::AST::TemplateParameterType::IsParameterPack::get()
{
    return ((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->IsParameterPack;
}

void CppSharp::Parser::AST::TemplateParameterType::IsParameterPack::set(bool value)
{
    ((::CppSharp::CppParser::AST::TemplateParameterType*)NativePtr)->IsParameterPack = value;
}

CppSharp::Parser::AST::TemplateParameterSubstitutionType::TemplateParameterSubstitutionType(::CppSharp::CppParser::AST::TemplateParameterSubstitutionType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::TemplateParameterSubstitutionType^ CppSharp::Parser::AST::TemplateParameterSubstitutionType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TemplateParameterSubstitutionType((::CppSharp::CppParser::AST::TemplateParameterSubstitutionType*) native.ToPointer());
}

CppSharp::Parser::AST::TemplateParameterSubstitutionType::TemplateParameterSubstitutionType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TemplateParameterSubstitutionType();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::TemplateParameterSubstitutionType::Replacement::get()
{
    return (&((::CppSharp::CppParser::AST::TemplateParameterSubstitutionType*)NativePtr)->Replacement == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::TemplateParameterSubstitutionType*)NativePtr)->Replacement);
}

void CppSharp::Parser::AST::TemplateParameterSubstitutionType::Replacement::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::TemplateParameterSubstitutionType*)NativePtr)->Replacement = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::InjectedClassNameType::InjectedClassNameType(::CppSharp::CppParser::AST::InjectedClassNameType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::InjectedClassNameType^ CppSharp::Parser::AST::InjectedClassNameType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::InjectedClassNameType((::CppSharp::CppParser::AST::InjectedClassNameType*) native.ToPointer());
}

CppSharp::Parser::AST::InjectedClassNameType::InjectedClassNameType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::InjectedClassNameType();
}

CppSharp::Parser::AST::TemplateSpecializationType^ CppSharp::Parser::AST::InjectedClassNameType::TemplateSpecialization::get()
{
    return (((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->TemplateSpecialization == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TemplateSpecializationType((::CppSharp::CppParser::AST::TemplateSpecializationType*)((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->TemplateSpecialization);
}

void CppSharp::Parser::AST::InjectedClassNameType::TemplateSpecialization::set(CppSharp::Parser::AST::TemplateSpecializationType^ value)
{
    ((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->TemplateSpecialization = (::CppSharp::CppParser::AST::TemplateSpecializationType*)value->NativePtr;
}

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::InjectedClassNameType::Class::get()
{
    return (((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->Class == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->Class);
}

void CppSharp::Parser::AST::InjectedClassNameType::Class::set(CppSharp::Parser::AST::Class^ value)
{
    ((::CppSharp::CppParser::AST::InjectedClassNameType*)NativePtr)->Class = (::CppSharp::CppParser::AST::Class*)value->NativePtr;
}

CppSharp::Parser::AST::DependentNameType::DependentNameType(::CppSharp::CppParser::AST::DependentNameType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::DependentNameType^ CppSharp::Parser::AST::DependentNameType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::DependentNameType((::CppSharp::CppParser::AST::DependentNameType*) native.ToPointer());
}

CppSharp::Parser::AST::DependentNameType::DependentNameType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::DependentNameType();
}

CppSharp::Parser::AST::PackExpansionType::PackExpansionType(::CppSharp::CppParser::AST::PackExpansionType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::PackExpansionType^ CppSharp::Parser::AST::PackExpansionType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::PackExpansionType((::CppSharp::CppParser::AST::PackExpansionType*) native.ToPointer());
}

CppSharp::Parser::AST::PackExpansionType::PackExpansionType()
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::PackExpansionType();
}

CppSharp::Parser::AST::BuiltinType::BuiltinType(::CppSharp::CppParser::AST::BuiltinType* native)
    : CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)native)
{
}

CppSharp::Parser::AST::BuiltinType^ CppSharp::Parser::AST::BuiltinType::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::BuiltinType((::CppSharp::CppParser::AST::BuiltinType*) native.ToPointer());
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

CppSharp::Parser::AST::VTableComponent::VTableComponent(::CppSharp::CppParser::AST::VTableComponent* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::VTableComponent^ CppSharp::Parser::AST::VTableComponent::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::VTableComponent((::CppSharp::CppParser::AST::VTableComponent*) native.ToPointer());
}

CppSharp::Parser::AST::VTableComponent::VTableComponent()
{
    NativePtr = new ::CppSharp::CppParser::AST::VTableComponent();
}

System::IntPtr CppSharp::Parser::AST::VTableComponent::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::VTableComponent::__Instance::set(System::IntPtr object)
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
    return (((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Declaration == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::VTableComponent::Declaration::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::VTableComponent*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

CppSharp::Parser::AST::VTableLayout::VTableLayout(::CppSharp::CppParser::AST::VTableLayout* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::VTableLayout^ CppSharp::Parser::AST::VTableLayout::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::VTableLayout((::CppSharp::CppParser::AST::VTableLayout*) native.ToPointer());
}

CppSharp::Parser::AST::VTableLayout::VTableLayout()
{
    NativePtr = new ::CppSharp::CppParser::AST::VTableLayout();
}

CppSharp::Parser::AST::VTableComponent^ CppSharp::Parser::AST::VTableLayout::getComponents(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::VTableLayout*)NativePtr)->getComponents(i);
    auto ____ret = new ::CppSharp::CppParser::AST::VTableComponent(__ret);
    return (____ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::VTableComponent((::CppSharp::CppParser::AST::VTableComponent*)____ret);
}

void CppSharp::Parser::AST::VTableLayout::addComponents(CppSharp::Parser::AST::VTableComponent^ s)
{
    auto &arg0 = *(::CppSharp::CppParser::AST::VTableComponent*)s->NativePtr;
    ((::CppSharp::CppParser::AST::VTableLayout*)NativePtr)->addComponents(arg0);
}

void CppSharp::Parser::AST::VTableLayout::clearComponents()
{
    ((::CppSharp::CppParser::AST::VTableLayout*)NativePtr)->clearComponents();
}

System::IntPtr CppSharp::Parser::AST::VTableLayout::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::VTableLayout::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::VTableLayout*)object.ToPointer();
}

unsigned int CppSharp::Parser::AST::VTableLayout::ComponentsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::VTableLayout*)NativePtr)->getComponentsCount();
    return __ret;
}

CppSharp::Parser::AST::VFTableInfo::VFTableInfo(::CppSharp::CppParser::AST::VFTableInfo* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::VFTableInfo^ CppSharp::Parser::AST::VFTableInfo::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::VFTableInfo((::CppSharp::CppParser::AST::VFTableInfo*) native.ToPointer());
}

CppSharp::Parser::AST::VFTableInfo::VFTableInfo()
{
    NativePtr = new ::CppSharp::CppParser::AST::VFTableInfo();
}

System::IntPtr CppSharp::Parser::AST::VFTableInfo::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::VFTableInfo::__Instance::set(System::IntPtr object)
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
    return (&((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->Layout == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::VTableLayout((::CppSharp::CppParser::AST::VTableLayout*)&((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->Layout);
}

void CppSharp::Parser::AST::VFTableInfo::Layout::set(CppSharp::Parser::AST::VTableLayout^ value)
{
    ((::CppSharp::CppParser::AST::VFTableInfo*)NativePtr)->Layout = *(::CppSharp::CppParser::AST::VTableLayout*)value->NativePtr;
}

CppSharp::Parser::AST::ClassLayout::ClassLayout(::CppSharp::CppParser::AST::ClassLayout* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::ClassLayout^ CppSharp::Parser::AST::ClassLayout::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::ClassLayout((::CppSharp::CppParser::AST::ClassLayout*) native.ToPointer());
}

CppSharp::Parser::AST::ClassLayout::ClassLayout()
{
    NativePtr = new ::CppSharp::CppParser::AST::ClassLayout();
}

CppSharp::Parser::AST::VFTableInfo^ CppSharp::Parser::AST::ClassLayout::getVFTables(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->getVFTables(i);
    auto ____ret = new ::CppSharp::CppParser::AST::VFTableInfo(__ret);
    return (____ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::VFTableInfo((::CppSharp::CppParser::AST::VFTableInfo*)____ret);
}

void CppSharp::Parser::AST::ClassLayout::addVFTables(CppSharp::Parser::AST::VFTableInfo^ s)
{
    auto &arg0 = *(::CppSharp::CppParser::AST::VFTableInfo*)s->NativePtr;
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->addVFTables(arg0);
}

void CppSharp::Parser::AST::ClassLayout::clearVFTables()
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->clearVFTables();
}

System::IntPtr CppSharp::Parser::AST::ClassLayout::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::ClassLayout::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::ClassLayout*)object.ToPointer();
}

unsigned int CppSharp::Parser::AST::ClassLayout::VFTablesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->getVFTablesCount();
    return __ret;
}

CppSharp::Parser::AST::CppAbi CppSharp::Parser::AST::ClassLayout::ABI::get()
{
    return (CppSharp::Parser::AST::CppAbi)((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->ABI;
}

void CppSharp::Parser::AST::ClassLayout::ABI::set(CppSharp::Parser::AST::CppAbi value)
{
    ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->ABI = (::CppSharp::CppParser::AST::CppAbi)value;
}

CppSharp::Parser::AST::VTableLayout^ CppSharp::Parser::AST::ClassLayout::Layout::get()
{
    return (&((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->Layout == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::VTableLayout((::CppSharp::CppParser::AST::VTableLayout*)&((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->Layout);
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

long CppSharp::Parser::AST::ClassLayout::VBPtrOffset::get()
{
    return ((::CppSharp::CppParser::AST::ClassLayout*)NativePtr)->VBPtrOffset;
}

void CppSharp::Parser::AST::ClassLayout::VBPtrOffset::set(long value)
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

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::Declaration::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*) native.ToPointer());
}

CppSharp::Parser::AST::Declaration::Declaration(CppSharp::Parser::AST::DeclarationKind kind)
{
    auto arg0 = (::CppSharp::CppParser::AST::DeclarationKind)kind;
    NativePtr = new ::CppSharp::CppParser::AST::Declaration(arg0);
}

CppSharp::Parser::AST::PreprocessedEntity^ CppSharp::Parser::AST::Declaration::getPreprocessedEntities(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->getPreprocessedEntities(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)__ret);
}

void CppSharp::Parser::AST::Declaration::addPreprocessedEntities(CppSharp::Parser::AST::PreprocessedEntity^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::PreprocessedEntity*)s->NativePtr;
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->addPreprocessedEntities(arg0);
}

void CppSharp::Parser::AST::Declaration::clearPreprocessedEntities()
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->clearPreprocessedEntities();
}

System::IntPtr CppSharp::Parser::AST::Declaration::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::Declaration::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::Declaration*)object.ToPointer();
}

System::String^ CppSharp::Parser::AST::Declaration::Name::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->getName();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::Declaration::Name::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->setName(arg0);
}

System::String^ CppSharp::Parser::AST::Declaration::DebugText::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->getDebugText();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::Declaration::DebugText::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->setDebugText(arg0);
}

unsigned int CppSharp::Parser::AST::Declaration::PreprocessedEntitiesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->getPreprocessedEntitiesCount();
    return __ret;
}

CppSharp::Parser::AST::DeclarationKind CppSharp::Parser::AST::Declaration::Kind::get()
{
    return (CppSharp::Parser::AST::DeclarationKind)((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Kind;
}

void CppSharp::Parser::AST::Declaration::Kind::set(CppSharp::Parser::AST::DeclarationKind value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Kind = (::CppSharp::CppParser::AST::DeclarationKind)value;
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
    return (((::CppSharp::CppParser::AST::Declaration*)NativePtr)->_Namespace == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)((::CppSharp::CppParser::AST::Declaration*)NativePtr)->_Namespace);
}

void CppSharp::Parser::AST::Declaration::_Namespace::set(CppSharp::Parser::AST::DeclarationContext^ value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->_Namespace = (::CppSharp::CppParser::AST::DeclarationContext*)value->NativePtr;
}

CppSharp::Parser::SourceLocation CppSharp::Parser::AST::Declaration::Location::get()
{
    return CppSharp::Parser::SourceLocation((::CppSharp::CppParser::SourceLocation*)&((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Location);
}

void CppSharp::Parser::AST::Declaration::Location::set(CppSharp::Parser::SourceLocation value)
{
    auto _marshal0 = ::CppSharp::CppParser::SourceLocation();
    _marshal0.ID = value.ID;
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Location = _marshal0;
}

CppSharp::Parser::AST::RawComment^ CppSharp::Parser::AST::Declaration::Comment::get()
{
    return (((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Comment == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::RawComment((::CppSharp::CppParser::AST::RawComment*)((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Comment);
}

void CppSharp::Parser::AST::Declaration::Comment::set(CppSharp::Parser::AST::RawComment^ value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->Comment = (::CppSharp::CppParser::AST::RawComment*)value->NativePtr;
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
    return (((::CppSharp::CppParser::AST::Declaration*)NativePtr)->CompleteDeclaration == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::Declaration*)NativePtr)->CompleteDeclaration);
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

::System::IntPtr CppSharp::Parser::AST::Declaration::OriginalPtr::get()
{
    return ::System::IntPtr(((::CppSharp::CppParser::AST::Declaration*)NativePtr)->OriginalPtr);
}

void CppSharp::Parser::AST::Declaration::OriginalPtr::set(::System::IntPtr value)
{
    ((::CppSharp::CppParser::AST::Declaration*)NativePtr)->OriginalPtr = (void*)value;
}

CppSharp::Parser::AST::DeclarationContext::DeclarationContext(::CppSharp::CppParser::AST::DeclarationContext* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::DeclarationContext^ CppSharp::Parser::AST::DeclarationContext::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*) native.ToPointer());
}

CppSharp::Parser::AST::DeclarationContext::DeclarationContext(CppSharp::Parser::AST::DeclarationKind kind)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    auto arg0 = (::CppSharp::CppParser::AST::DeclarationKind)kind;
    NativePtr = new ::CppSharp::CppParser::AST::DeclarationContext(arg0);
}

CppSharp::Parser::AST::Namespace^ CppSharp::Parser::AST::DeclarationContext::getNamespaces(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getNamespaces(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)__ret);
}

void CppSharp::Parser::AST::DeclarationContext::addNamespaces(CppSharp::Parser::AST::Namespace^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Namespace*)s->NativePtr;
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->addNamespaces(arg0);
}

void CppSharp::Parser::AST::DeclarationContext::clearNamespaces()
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->clearNamespaces();
}

CppSharp::Parser::AST::Enumeration^ CppSharp::Parser::AST::DeclarationContext::getEnums(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getEnums(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Enumeration((::CppSharp::CppParser::AST::Enumeration*)__ret);
}

void CppSharp::Parser::AST::DeclarationContext::addEnums(CppSharp::Parser::AST::Enumeration^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Enumeration*)s->NativePtr;
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->addEnums(arg0);
}

void CppSharp::Parser::AST::DeclarationContext::clearEnums()
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->clearEnums();
}

CppSharp::Parser::AST::Function^ CppSharp::Parser::AST::DeclarationContext::getFunctions(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getFunctions(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)__ret);
}

void CppSharp::Parser::AST::DeclarationContext::addFunctions(CppSharp::Parser::AST::Function^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Function*)s->NativePtr;
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->addFunctions(arg0);
}

void CppSharp::Parser::AST::DeclarationContext::clearFunctions()
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->clearFunctions();
}

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::DeclarationContext::getClasses(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getClasses(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)__ret);
}

void CppSharp::Parser::AST::DeclarationContext::addClasses(CppSharp::Parser::AST::Class^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Class*)s->NativePtr;
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->addClasses(arg0);
}

void CppSharp::Parser::AST::DeclarationContext::clearClasses()
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->clearClasses();
}

CppSharp::Parser::AST::Template^ CppSharp::Parser::AST::DeclarationContext::getTemplates(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getTemplates(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)__ret);
}

void CppSharp::Parser::AST::DeclarationContext::addTemplates(CppSharp::Parser::AST::Template^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Template*)s->NativePtr;
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->addTemplates(arg0);
}

void CppSharp::Parser::AST::DeclarationContext::clearTemplates()
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->clearTemplates();
}

CppSharp::Parser::AST::TypedefDecl^ CppSharp::Parser::AST::DeclarationContext::getTypedefs(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getTypedefs(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TypedefDecl((::CppSharp::CppParser::AST::TypedefDecl*)__ret);
}

void CppSharp::Parser::AST::DeclarationContext::addTypedefs(CppSharp::Parser::AST::TypedefDecl^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::TypedefDecl*)s->NativePtr;
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->addTypedefs(arg0);
}

void CppSharp::Parser::AST::DeclarationContext::clearTypedefs()
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->clearTypedefs();
}

CppSharp::Parser::AST::Variable^ CppSharp::Parser::AST::DeclarationContext::getVariables(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getVariables(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Variable((::CppSharp::CppParser::AST::Variable*)__ret);
}

void CppSharp::Parser::AST::DeclarationContext::addVariables(CppSharp::Parser::AST::Variable^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Variable*)s->NativePtr;
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->addVariables(arg0);
}

void CppSharp::Parser::AST::DeclarationContext::clearVariables()
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->clearVariables();
}

CppSharp::Parser::AST::Friend^ CppSharp::Parser::AST::DeclarationContext::getFriends(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getFriends(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Friend((::CppSharp::CppParser::AST::Friend*)__ret);
}

void CppSharp::Parser::AST::DeclarationContext::addFriends(CppSharp::Parser::AST::Friend^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Friend*)s->NativePtr;
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->addFriends(arg0);
}

void CppSharp::Parser::AST::DeclarationContext::clearFriends()
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->clearFriends();
}

unsigned int CppSharp::Parser::AST::DeclarationContext::NamespacesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getNamespacesCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::DeclarationContext::EnumsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getEnumsCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::DeclarationContext::FunctionsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getFunctionsCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::DeclarationContext::ClassesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getClassesCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::DeclarationContext::TemplatesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getTemplatesCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::DeclarationContext::TypedefsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getTypedefsCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::DeclarationContext::VariablesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getVariablesCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::DeclarationContext::FriendsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->getFriendsCount();
    return __ret;
}

bool CppSharp::Parser::AST::DeclarationContext::IsAnonymous::get()
{
    return ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->IsAnonymous;
}

void CppSharp::Parser::AST::DeclarationContext::IsAnonymous::set(bool value)
{
    ((::CppSharp::CppParser::AST::DeclarationContext*)NativePtr)->IsAnonymous = value;
}

CppSharp::Parser::AST::TypedefDecl::TypedefDecl(::CppSharp::CppParser::AST::TypedefDecl* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::TypedefDecl^ CppSharp::Parser::AST::TypedefDecl::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TypedefDecl((::CppSharp::CppParser::AST::TypedefDecl*) native.ToPointer());
}

CppSharp::Parser::AST::TypedefDecl::TypedefDecl()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TypedefDecl();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::TypedefDecl::QualifiedType::get()
{
    return (&((::CppSharp::CppParser::AST::TypedefDecl*)NativePtr)->QualifiedType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::TypedefDecl*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::AST::TypedefDecl::QualifiedType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::TypedefDecl*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::Friend::Friend(::CppSharp::CppParser::AST::Friend* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Friend^ CppSharp::Parser::AST::Friend::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Friend((::CppSharp::CppParser::AST::Friend*) native.ToPointer());
}

CppSharp::Parser::AST::Friend::Friend()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Friend();
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::Friend::Declaration::get()
{
    return (((::CppSharp::CppParser::AST::Friend*)NativePtr)->Declaration == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::Friend*)NativePtr)->Declaration);
}

void CppSharp::Parser::AST::Friend::Declaration::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::Friend*)NativePtr)->Declaration = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

CppSharp::Parser::AST::Statement::Statement(::CppSharp::CppParser::AST::Statement* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::Statement^ CppSharp::Parser::AST::Statement::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Statement((::CppSharp::CppParser::AST::Statement*) native.ToPointer());
}

System::IntPtr CppSharp::Parser::AST::Statement::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::Statement::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::Statement*)object.ToPointer();
}

System::String^ CppSharp::Parser::AST::Statement::String::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Statement*)NativePtr)->getString();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::Statement::String::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::Statement*)NativePtr)->setString(arg0);
}

CppSharp::Parser::AST::StatementClass CppSharp::Parser::AST::Statement::Class::get()
{
    return (CppSharp::Parser::AST::StatementClass)((::CppSharp::CppParser::AST::Statement*)NativePtr)->Class;
}

void CppSharp::Parser::AST::Statement::Class::set(CppSharp::Parser::AST::StatementClass value)
{
    ((::CppSharp::CppParser::AST::Statement*)NativePtr)->Class = (::CppSharp::CppParser::AST::StatementClass)value;
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::Statement::Decl::get()
{
    return (((::CppSharp::CppParser::AST::Statement*)NativePtr)->Decl == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::Statement*)NativePtr)->Decl);
}

void CppSharp::Parser::AST::Statement::Decl::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::Statement*)NativePtr)->Decl = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

CppSharp::Parser::AST::Expression::Expression(::CppSharp::CppParser::AST::Expression* native)
    : CppSharp::Parser::AST::Statement((::CppSharp::CppParser::AST::Statement*)native)
{
}

CppSharp::Parser::AST::Expression^ CppSharp::Parser::AST::Expression::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Expression((::CppSharp::CppParser::AST::Expression*) native.ToPointer());
}

CppSharp::Parser::AST::Expression^ CppSharp::Parser::AST::Expression::Subexpression::get()
{
    return (((::CppSharp::CppParser::AST::Expression*)NativePtr)->Subexpression == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Expression((::CppSharp::CppParser::AST::Expression*)((::CppSharp::CppParser::AST::Expression*)NativePtr)->Subexpression);
}

void CppSharp::Parser::AST::Expression::Subexpression::set(CppSharp::Parser::AST::Expression^ value)
{
    ((::CppSharp::CppParser::AST::Expression*)NativePtr)->Subexpression = (::CppSharp::CppParser::AST::Expression*)value->NativePtr;
}

CppSharp::Parser::AST::Parameter::Parameter(::CppSharp::CppParser::AST::Parameter* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Parameter^ CppSharp::Parser::AST::Parameter::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Parameter((::CppSharp::CppParser::AST::Parameter*) native.ToPointer());
}

CppSharp::Parser::AST::Parameter::Parameter()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Parameter();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Parameter::QualifiedType::get()
{
    return (&((::CppSharp::CppParser::AST::Parameter*)NativePtr)->QualifiedType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Parameter*)NativePtr)->QualifiedType);
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

unsigned int CppSharp::Parser::AST::Parameter::Index::get()
{
    return ((::CppSharp::CppParser::AST::Parameter*)NativePtr)->Index;
}

void CppSharp::Parser::AST::Parameter::Index::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::Parameter*)NativePtr)->Index = value;
}

CppSharp::Parser::AST::Expression^ CppSharp::Parser::AST::Parameter::DefaultArgument::get()
{
    return (((::CppSharp::CppParser::AST::Parameter*)NativePtr)->DefaultArgument == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Expression((::CppSharp::CppParser::AST::Expression*)((::CppSharp::CppParser::AST::Parameter*)NativePtr)->DefaultArgument);
}

void CppSharp::Parser::AST::Parameter::DefaultArgument::set(CppSharp::Parser::AST::Expression^ value)
{
    ((::CppSharp::CppParser::AST::Parameter*)NativePtr)->DefaultArgument = (::CppSharp::CppParser::AST::Expression*)value->NativePtr;
}

CppSharp::Parser::AST::Function::Function(::CppSharp::CppParser::AST::Function* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Function^ CppSharp::Parser::AST::Function::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*) native.ToPointer());
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
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Parameter((::CppSharp::CppParser::AST::Parameter*)__ret);
}

void CppSharp::Parser::AST::Function::addParameters(CppSharp::Parser::AST::Parameter^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Parameter*)s->NativePtr;
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->addParameters(arg0);
}

void CppSharp::Parser::AST::Function::clearParameters()
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->clearParameters();
}

System::String^ CppSharp::Parser::AST::Function::Mangled::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Function*)NativePtr)->getMangled();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::Function::Mangled::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->setMangled(arg0);
}

System::String^ CppSharp::Parser::AST::Function::Signature::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Function*)NativePtr)->getSignature();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::Function::Signature::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->setSignature(arg0);
}

unsigned int CppSharp::Parser::AST::Function::ParametersCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Function*)NativePtr)->getParametersCount();
    return __ret;
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Function::ReturnType::get()
{
    return (&((::CppSharp::CppParser::AST::Function*)NativePtr)->ReturnType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Function*)NativePtr)->ReturnType);
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

bool CppSharp::Parser::AST::Function::HasThisReturn::get()
{
    return ((::CppSharp::CppParser::AST::Function*)NativePtr)->HasThisReturn;
}

void CppSharp::Parser::AST::Function::HasThisReturn::set(bool value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->HasThisReturn = value;
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

CppSharp::Parser::AST::CallingConvention CppSharp::Parser::AST::Function::CallingConvention::get()
{
    return (CppSharp::Parser::AST::CallingConvention)((::CppSharp::CppParser::AST::Function*)NativePtr)->CallingConvention;
}

void CppSharp::Parser::AST::Function::CallingConvention::set(CppSharp::Parser::AST::CallingConvention value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->CallingConvention = (::CppSharp::CppParser::AST::CallingConvention)value;
}

CppSharp::Parser::AST::FunctionTemplateSpecialization^ CppSharp::Parser::AST::Function::SpecializationInfo::get()
{
    return (((::CppSharp::CppParser::AST::Function*)NativePtr)->SpecializationInfo == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::FunctionTemplateSpecialization((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)((::CppSharp::CppParser::AST::Function*)NativePtr)->SpecializationInfo);
}

void CppSharp::Parser::AST::Function::SpecializationInfo::set(CppSharp::Parser::AST::FunctionTemplateSpecialization^ value)
{
    ((::CppSharp::CppParser::AST::Function*)NativePtr)->SpecializationInfo = (::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)value->NativePtr;
}

CppSharp::Parser::AST::Method::Method(::CppSharp::CppParser::AST::Method* native)
    : CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)native)
{
}

CppSharp::Parser::AST::Method^ CppSharp::Parser::AST::Method::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Method((::CppSharp::CppParser::AST::Method*) native.ToPointer());
}

CppSharp::Parser::AST::Method::Method()
    : CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Method();
}

CppSharp::Parser::AST::AccessSpecifierDecl^ CppSharp::Parser::AST::Method::AccessDecl::get()
{
    return (((::CppSharp::CppParser::AST::Method*)NativePtr)->AccessDecl == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::AccessSpecifierDecl((::CppSharp::CppParser::AST::AccessSpecifierDecl*)((::CppSharp::CppParser::AST::Method*)NativePtr)->AccessDecl);
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

bool CppSharp::Parser::AST::Method::IsExplicit::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsExplicit;
}

void CppSharp::Parser::AST::Method::IsExplicit::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsExplicit = value;
}

bool CppSharp::Parser::AST::Method::IsOverride::get()
{
    return ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsOverride;
}

void CppSharp::Parser::AST::Method::IsOverride::set(bool value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->IsOverride = value;
}

CppSharp::Parser::AST::CXXMethodKind CppSharp::Parser::AST::Method::MethodKind::get()
{
    return (CppSharp::Parser::AST::CXXMethodKind)((::CppSharp::CppParser::AST::Method*)NativePtr)->MethodKind;
}

void CppSharp::Parser::AST::Method::MethodKind::set(CppSharp::Parser::AST::CXXMethodKind value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->MethodKind = (::CppSharp::CppParser::AST::CXXMethodKind)value;
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
    return (&((::CppSharp::CppParser::AST::Method*)NativePtr)->ConversionType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Method*)NativePtr)->ConversionType);
}

void CppSharp::Parser::AST::Method::ConversionType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::Method*)NativePtr)->ConversionType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::Enumeration::Item::Item(::CppSharp::CppParser::AST::Enumeration::Item* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Enumeration::Item^ CppSharp::Parser::AST::Enumeration::Item::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Enumeration::Item((::CppSharp::CppParser::AST::Enumeration::Item*) native.ToPointer());
}

CppSharp::Parser::AST::Enumeration::Item::Item()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Enumeration::Item();
}

System::String^ CppSharp::Parser::AST::Enumeration::Item::Expression::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Enumeration::Item*)NativePtr)->getExpression();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::Enumeration::Item::Expression::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::Enumeration::Item*)NativePtr)->setExpression(arg0);
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
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)native)
{
}

CppSharp::Parser::AST::Enumeration^ CppSharp::Parser::AST::Enumeration::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Enumeration((::CppSharp::CppParser::AST::Enumeration*) native.ToPointer());
}

CppSharp::Parser::AST::Enumeration::Enumeration()
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Enumeration();
}

CppSharp::Parser::AST::Enumeration::Item^ CppSharp::Parser::AST::Enumeration::getItems(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->getItems(i);
    auto ____ret = new ::CppSharp::CppParser::AST::Enumeration::Item(__ret);
    return (____ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Enumeration::Item((::CppSharp::CppParser::AST::Enumeration::Item*)____ret);
}

void CppSharp::Parser::AST::Enumeration::addItems(CppSharp::Parser::AST::Enumeration::Item^ s)
{
    auto &arg0 = *(::CppSharp::CppParser::AST::Enumeration::Item*)s->NativePtr;
    ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->addItems(arg0);
}

void CppSharp::Parser::AST::Enumeration::clearItems()
{
    ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->clearItems();
}

unsigned int CppSharp::Parser::AST::Enumeration::ItemsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->getItemsCount();
    return __ret;
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
    return (((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Type == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Type);
}

void CppSharp::Parser::AST::Enumeration::Type::set(CppSharp::Parser::AST::Type^ value)
{
    ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->Type = (::CppSharp::CppParser::AST::Type*)value->NativePtr;
}

CppSharp::Parser::AST::BuiltinType^ CppSharp::Parser::AST::Enumeration::BuiltinType::get()
{
    return (((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->BuiltinType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::BuiltinType((::CppSharp::CppParser::AST::BuiltinType*)((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->BuiltinType);
}

void CppSharp::Parser::AST::Enumeration::BuiltinType::set(CppSharp::Parser::AST::BuiltinType^ value)
{
    ((::CppSharp::CppParser::AST::Enumeration*)NativePtr)->BuiltinType = (::CppSharp::CppParser::AST::BuiltinType*)value->NativePtr;
}

CppSharp::Parser::AST::Variable::Variable(::CppSharp::CppParser::AST::Variable* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Variable^ CppSharp::Parser::AST::Variable::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Variable((::CppSharp::CppParser::AST::Variable*) native.ToPointer());
}

CppSharp::Parser::AST::Variable::Variable()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Variable();
}

System::String^ CppSharp::Parser::AST::Variable::Mangled::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Variable*)NativePtr)->getMangled();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::Variable::Mangled::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::Variable*)NativePtr)->setMangled(arg0);
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Variable::QualifiedType::get()
{
    return (&((::CppSharp::CppParser::AST::Variable*)NativePtr)->QualifiedType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Variable*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::AST::Variable::QualifiedType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::Variable*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
}

CppSharp::Parser::AST::BaseClassSpecifier::BaseClassSpecifier(::CppSharp::CppParser::AST::BaseClassSpecifier* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::BaseClassSpecifier^ CppSharp::Parser::AST::BaseClassSpecifier::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::BaseClassSpecifier((::CppSharp::CppParser::AST::BaseClassSpecifier*) native.ToPointer());
}

CppSharp::Parser::AST::BaseClassSpecifier::BaseClassSpecifier()
{
    NativePtr = new ::CppSharp::CppParser::AST::BaseClassSpecifier();
}

System::IntPtr CppSharp::Parser::AST::BaseClassSpecifier::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::BaseClassSpecifier::__Instance::set(System::IntPtr object)
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
    return (((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Type == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Type((::CppSharp::CppParser::AST::Type*)((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Type);
}

void CppSharp::Parser::AST::BaseClassSpecifier::Type::set(CppSharp::Parser::AST::Type^ value)
{
    ((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Type = (::CppSharp::CppParser::AST::Type*)value->NativePtr;
}

int CppSharp::Parser::AST::BaseClassSpecifier::Offset::get()
{
    return ((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Offset;
}

void CppSharp::Parser::AST::BaseClassSpecifier::Offset::set(int value)
{
    ((::CppSharp::CppParser::AST::BaseClassSpecifier*)NativePtr)->Offset = value;
}

CppSharp::Parser::AST::Field::Field(::CppSharp::CppParser::AST::Field* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Field^ CppSharp::Parser::AST::Field::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Field((::CppSharp::CppParser::AST::Field*) native.ToPointer());
}

CppSharp::Parser::AST::Field::Field()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Field();
}

CppSharp::Parser::AST::QualifiedType^ CppSharp::Parser::AST::Field::QualifiedType::get()
{
    return (&((::CppSharp::CppParser::AST::Field*)NativePtr)->QualifiedType == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::QualifiedType((::CppSharp::CppParser::AST::QualifiedType*)&((::CppSharp::CppParser::AST::Field*)NativePtr)->QualifiedType);
}

void CppSharp::Parser::AST::Field::QualifiedType::set(CppSharp::Parser::AST::QualifiedType^ value)
{
    ((::CppSharp::CppParser::AST::Field*)NativePtr)->QualifiedType = *(::CppSharp::CppParser::AST::QualifiedType*)value->NativePtr;
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
    return (((::CppSharp::CppParser::AST::Field*)NativePtr)->Class == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)((::CppSharp::CppParser::AST::Field*)NativePtr)->Class);
}

void CppSharp::Parser::AST::Field::Class::set(CppSharp::Parser::AST::Class^ value)
{
    ((::CppSharp::CppParser::AST::Field*)NativePtr)->Class = (::CppSharp::CppParser::AST::Class*)value->NativePtr;
}

bool CppSharp::Parser::AST::Field::IsBitField::get()
{
    return ((::CppSharp::CppParser::AST::Field*)NativePtr)->IsBitField;
}

void CppSharp::Parser::AST::Field::IsBitField::set(bool value)
{
    ((::CppSharp::CppParser::AST::Field*)NativePtr)->IsBitField = value;
}

unsigned int CppSharp::Parser::AST::Field::BitWidth::get()
{
    return ((::CppSharp::CppParser::AST::Field*)NativePtr)->BitWidth;
}

void CppSharp::Parser::AST::Field::BitWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::AST::Field*)NativePtr)->BitWidth = value;
}

CppSharp::Parser::AST::AccessSpecifierDecl::AccessSpecifierDecl(::CppSharp::CppParser::AST::AccessSpecifierDecl* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::AccessSpecifierDecl^ CppSharp::Parser::AST::AccessSpecifierDecl::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::AccessSpecifierDecl((::CppSharp::CppParser::AST::AccessSpecifierDecl*) native.ToPointer());
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

CppSharp::Parser::AST::Class^ CppSharp::Parser::AST::Class::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*) native.ToPointer());
}

CppSharp::Parser::AST::Class::Class()
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Class();
}

CppSharp::Parser::AST::BaseClassSpecifier^ CppSharp::Parser::AST::Class::getBases(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getBases(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::BaseClassSpecifier((::CppSharp::CppParser::AST::BaseClassSpecifier*)__ret);
}

void CppSharp::Parser::AST::Class::addBases(CppSharp::Parser::AST::BaseClassSpecifier^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::BaseClassSpecifier*)s->NativePtr;
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->addBases(arg0);
}

void CppSharp::Parser::AST::Class::clearBases()
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->clearBases();
}

CppSharp::Parser::AST::Field^ CppSharp::Parser::AST::Class::getFields(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getFields(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Field((::CppSharp::CppParser::AST::Field*)__ret);
}

void CppSharp::Parser::AST::Class::addFields(CppSharp::Parser::AST::Field^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Field*)s->NativePtr;
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->addFields(arg0);
}

void CppSharp::Parser::AST::Class::clearFields()
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->clearFields();
}

CppSharp::Parser::AST::Method^ CppSharp::Parser::AST::Class::getMethods(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getMethods(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Method((::CppSharp::CppParser::AST::Method*)__ret);
}

void CppSharp::Parser::AST::Class::addMethods(CppSharp::Parser::AST::Method^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::Method*)s->NativePtr;
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->addMethods(arg0);
}

void CppSharp::Parser::AST::Class::clearMethods()
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->clearMethods();
}

CppSharp::Parser::AST::AccessSpecifierDecl^ CppSharp::Parser::AST::Class::getSpecifiers(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getSpecifiers(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::AccessSpecifierDecl((::CppSharp::CppParser::AST::AccessSpecifierDecl*)__ret);
}

void CppSharp::Parser::AST::Class::addSpecifiers(CppSharp::Parser::AST::AccessSpecifierDecl^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::AccessSpecifierDecl*)s->NativePtr;
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->addSpecifiers(arg0);
}

void CppSharp::Parser::AST::Class::clearSpecifiers()
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->clearSpecifiers();
}

unsigned int CppSharp::Parser::AST::Class::BasesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getBasesCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::Class::FieldsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getFieldsCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::Class::MethodsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getMethodsCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::Class::SpecifiersCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Class*)NativePtr)->getSpecifiersCount();
    return __ret;
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

bool CppSharp::Parser::AST::Class::HasNonTrivialDestructor::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->HasNonTrivialDestructor;
}

void CppSharp::Parser::AST::Class::HasNonTrivialDestructor::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->HasNonTrivialDestructor = value;
}

bool CppSharp::Parser::AST::Class::IsExternCContext::get()
{
    return ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsExternCContext;
}

void CppSharp::Parser::AST::Class::IsExternCContext::set(bool value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->IsExternCContext = value;
}

CppSharp::Parser::AST::ClassLayout^ CppSharp::Parser::AST::Class::Layout::get()
{
    return (((::CppSharp::CppParser::AST::Class*)NativePtr)->Layout == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ClassLayout((::CppSharp::CppParser::AST::ClassLayout*)((::CppSharp::CppParser::AST::Class*)NativePtr)->Layout);
}

void CppSharp::Parser::AST::Class::Layout::set(CppSharp::Parser::AST::ClassLayout^ value)
{
    ((::CppSharp::CppParser::AST::Class*)NativePtr)->Layout = (::CppSharp::CppParser::AST::ClassLayout*)value->NativePtr;
}

CppSharp::Parser::AST::Template::Template(::CppSharp::CppParser::AST::Template* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::Template^ CppSharp::Parser::AST::Template::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*) native.ToPointer());
}

CppSharp::Parser::AST::Template::Template(CppSharp::Parser::AST::DeclarationKind kind)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    auto arg0 = (::CppSharp::CppParser::AST::DeclarationKind)kind;
    NativePtr = new ::CppSharp::CppParser::AST::Template(arg0);
}

CppSharp::Parser::AST::Template::Template()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Template();
}

CppSharp::Parser::AST::TemplateParameter^ CppSharp::Parser::AST::Template::getParameters(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::Template*)NativePtr)->getParameters(i);
    auto ____ret = new ::CppSharp::CppParser::AST::TemplateParameter(__ret);
    return (____ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TemplateParameter((::CppSharp::CppParser::AST::TemplateParameter*)____ret);
}

void CppSharp::Parser::AST::Template::addParameters(CppSharp::Parser::AST::TemplateParameter^ s)
{
    auto &arg0 = *(::CppSharp::CppParser::AST::TemplateParameter*)s->NativePtr;
    ((::CppSharp::CppParser::AST::Template*)NativePtr)->addParameters(arg0);
}

void CppSharp::Parser::AST::Template::clearParameters()
{
    ((::CppSharp::CppParser::AST::Template*)NativePtr)->clearParameters();
}

unsigned int CppSharp::Parser::AST::Template::ParametersCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::Template*)NativePtr)->getParametersCount();
    return __ret;
}

CppSharp::Parser::AST::Declaration^ CppSharp::Parser::AST::Template::TemplatedDecl::get()
{
    return (((::CppSharp::CppParser::AST::Template*)NativePtr)->TemplatedDecl == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)((::CppSharp::CppParser::AST::Template*)NativePtr)->TemplatedDecl);
}

void CppSharp::Parser::AST::Template::TemplatedDecl::set(CppSharp::Parser::AST::Declaration^ value)
{
    ((::CppSharp::CppParser::AST::Template*)NativePtr)->TemplatedDecl = (::CppSharp::CppParser::AST::Declaration*)value->NativePtr;
}

CppSharp::Parser::AST::ClassTemplate::ClassTemplate(::CppSharp::CppParser::AST::ClassTemplate* native)
    : CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)native)
{
}

CppSharp::Parser::AST::ClassTemplate^ CppSharp::Parser::AST::ClassTemplate::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::ClassTemplate((::CppSharp::CppParser::AST::ClassTemplate*) native.ToPointer());
}

CppSharp::Parser::AST::ClassTemplate::ClassTemplate()
    : CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::ClassTemplate();
}

CppSharp::Parser::AST::ClassTemplateSpecialization^ CppSharp::Parser::AST::ClassTemplate::getSpecializations(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::ClassTemplate*)NativePtr)->getSpecializations(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ClassTemplateSpecialization((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)__ret);
}

void CppSharp::Parser::AST::ClassTemplate::addSpecializations(CppSharp::Parser::AST::ClassTemplateSpecialization^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::ClassTemplateSpecialization*)s->NativePtr;
    ((::CppSharp::CppParser::AST::ClassTemplate*)NativePtr)->addSpecializations(arg0);
}

void CppSharp::Parser::AST::ClassTemplate::clearSpecializations()
{
    ((::CppSharp::CppParser::AST::ClassTemplate*)NativePtr)->clearSpecializations();
}

unsigned int CppSharp::Parser::AST::ClassTemplate::SpecializationsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::ClassTemplate*)NativePtr)->getSpecializationsCount();
    return __ret;
}

CppSharp::Parser::AST::ClassTemplateSpecialization::ClassTemplateSpecialization(::CppSharp::CppParser::AST::ClassTemplateSpecialization* native)
    : CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)native)
{
}

CppSharp::Parser::AST::ClassTemplateSpecialization^ CppSharp::Parser::AST::ClassTemplateSpecialization::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::ClassTemplateSpecialization((::CppSharp::CppParser::AST::ClassTemplateSpecialization*) native.ToPointer());
}

CppSharp::Parser::AST::ClassTemplateSpecialization::ClassTemplateSpecialization()
    : CppSharp::Parser::AST::Class((::CppSharp::CppParser::AST::Class*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::ClassTemplateSpecialization();
}

CppSharp::Parser::AST::TemplateArgument^ CppSharp::Parser::AST::ClassTemplateSpecialization::getArguments(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->getArguments(i);
    auto ____ret = new ::CppSharp::CppParser::AST::TemplateArgument(__ret);
    return (____ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TemplateArgument((::CppSharp::CppParser::AST::TemplateArgument*)____ret);
}

void CppSharp::Parser::AST::ClassTemplateSpecialization::addArguments(CppSharp::Parser::AST::TemplateArgument^ s)
{
    auto &arg0 = *(::CppSharp::CppParser::AST::TemplateArgument*)s->NativePtr;
    ((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->addArguments(arg0);
}

void CppSharp::Parser::AST::ClassTemplateSpecialization::clearArguments()
{
    ((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->clearArguments();
}

unsigned int CppSharp::Parser::AST::ClassTemplateSpecialization::ArgumentsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->getArgumentsCount();
    return __ret;
}

CppSharp::Parser::AST::ClassTemplate^ CppSharp::Parser::AST::ClassTemplateSpecialization::TemplatedDecl::get()
{
    return (((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->TemplatedDecl == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ClassTemplate((::CppSharp::CppParser::AST::ClassTemplate*)((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->TemplatedDecl);
}

void CppSharp::Parser::AST::ClassTemplateSpecialization::TemplatedDecl::set(CppSharp::Parser::AST::ClassTemplate^ value)
{
    ((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->TemplatedDecl = (::CppSharp::CppParser::AST::ClassTemplate*)value->NativePtr;
}

CppSharp::Parser::AST::TemplateSpecializationKind CppSharp::Parser::AST::ClassTemplateSpecialization::SpecializationKind::get()
{
    return (CppSharp::Parser::AST::TemplateSpecializationKind)((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->SpecializationKind;
}

void CppSharp::Parser::AST::ClassTemplateSpecialization::SpecializationKind::set(CppSharp::Parser::AST::TemplateSpecializationKind value)
{
    ((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)NativePtr)->SpecializationKind = (::CppSharp::CppParser::AST::TemplateSpecializationKind)value;
}

CppSharp::Parser::AST::ClassTemplatePartialSpecialization::ClassTemplatePartialSpecialization(::CppSharp::CppParser::AST::ClassTemplatePartialSpecialization* native)
    : CppSharp::Parser::AST::ClassTemplateSpecialization((::CppSharp::CppParser::AST::ClassTemplateSpecialization*)native)
{
}

CppSharp::Parser::AST::ClassTemplatePartialSpecialization^ CppSharp::Parser::AST::ClassTemplatePartialSpecialization::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::ClassTemplatePartialSpecialization((::CppSharp::CppParser::AST::ClassTemplatePartialSpecialization*) native.ToPointer());
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

CppSharp::Parser::AST::FunctionTemplate^ CppSharp::Parser::AST::FunctionTemplate::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::FunctionTemplate((::CppSharp::CppParser::AST::FunctionTemplate*) native.ToPointer());
}

CppSharp::Parser::AST::FunctionTemplate::FunctionTemplate()
    : CppSharp::Parser::AST::Template((::CppSharp::CppParser::AST::Template*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::FunctionTemplate();
}

CppSharp::Parser::AST::FunctionTemplateSpecialization^ CppSharp::Parser::AST::FunctionTemplate::getSpecializations(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::FunctionTemplate*)NativePtr)->getSpecializations(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::FunctionTemplateSpecialization((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)__ret);
}

void CppSharp::Parser::AST::FunctionTemplate::addSpecializations(CppSharp::Parser::AST::FunctionTemplateSpecialization^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)s->NativePtr;
    ((::CppSharp::CppParser::AST::FunctionTemplate*)NativePtr)->addSpecializations(arg0);
}

void CppSharp::Parser::AST::FunctionTemplate::clearSpecializations()
{
    ((::CppSharp::CppParser::AST::FunctionTemplate*)NativePtr)->clearSpecializations();
}

unsigned int CppSharp::Parser::AST::FunctionTemplate::SpecializationsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::FunctionTemplate*)NativePtr)->getSpecializationsCount();
    return __ret;
}

CppSharp::Parser::AST::FunctionTemplateSpecialization::FunctionTemplateSpecialization(::CppSharp::CppParser::AST::FunctionTemplateSpecialization* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::FunctionTemplateSpecialization^ CppSharp::Parser::AST::FunctionTemplateSpecialization::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::FunctionTemplateSpecialization((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*) native.ToPointer());
}

CppSharp::Parser::AST::FunctionTemplateSpecialization::FunctionTemplateSpecialization()
{
    NativePtr = new ::CppSharp::CppParser::AST::FunctionTemplateSpecialization();
}

CppSharp::Parser::AST::TemplateArgument^ CppSharp::Parser::AST::FunctionTemplateSpecialization::getArguments(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->getArguments(i);
    auto ____ret = new ::CppSharp::CppParser::AST::TemplateArgument(__ret);
    return (____ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TemplateArgument((::CppSharp::CppParser::AST::TemplateArgument*)____ret);
}

void CppSharp::Parser::AST::FunctionTemplateSpecialization::addArguments(CppSharp::Parser::AST::TemplateArgument^ s)
{
    auto &arg0 = *(::CppSharp::CppParser::AST::TemplateArgument*)s->NativePtr;
    ((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->addArguments(arg0);
}

void CppSharp::Parser::AST::FunctionTemplateSpecialization::clearArguments()
{
    ((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->clearArguments();
}

System::IntPtr CppSharp::Parser::AST::FunctionTemplateSpecialization::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::FunctionTemplateSpecialization::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)object.ToPointer();
}

unsigned int CppSharp::Parser::AST::FunctionTemplateSpecialization::ArgumentsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->getArgumentsCount();
    return __ret;
}

CppSharp::Parser::AST::FunctionTemplate^ CppSharp::Parser::AST::FunctionTemplateSpecialization::Template::get()
{
    return (((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->Template == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::FunctionTemplate((::CppSharp::CppParser::AST::FunctionTemplate*)((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->Template);
}

void CppSharp::Parser::AST::FunctionTemplateSpecialization::Template::set(CppSharp::Parser::AST::FunctionTemplate^ value)
{
    ((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->Template = (::CppSharp::CppParser::AST::FunctionTemplate*)value->NativePtr;
}

CppSharp::Parser::AST::Function^ CppSharp::Parser::AST::FunctionTemplateSpecialization::SpecializedFunction::get()
{
    return (((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->SpecializedFunction == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::Function((::CppSharp::CppParser::AST::Function*)((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->SpecializedFunction);
}

void CppSharp::Parser::AST::FunctionTemplateSpecialization::SpecializedFunction::set(CppSharp::Parser::AST::Function^ value)
{
    ((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->SpecializedFunction = (::CppSharp::CppParser::AST::Function*)value->NativePtr;
}

CppSharp::Parser::AST::TemplateSpecializationKind CppSharp::Parser::AST::FunctionTemplateSpecialization::SpecializationKind::get()
{
    return (CppSharp::Parser::AST::TemplateSpecializationKind)((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->SpecializationKind;
}

void CppSharp::Parser::AST::FunctionTemplateSpecialization::SpecializationKind::set(CppSharp::Parser::AST::TemplateSpecializationKind value)
{
    ((::CppSharp::CppParser::AST::FunctionTemplateSpecialization*)NativePtr)->SpecializationKind = (::CppSharp::CppParser::AST::TemplateSpecializationKind)value;
}

CppSharp::Parser::AST::Namespace::Namespace(::CppSharp::CppParser::AST::Namespace* native)
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)native)
{
}

CppSharp::Parser::AST::Namespace^ CppSharp::Parser::AST::Namespace::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*) native.ToPointer());
}

CppSharp::Parser::AST::Namespace::Namespace()
    : CppSharp::Parser::AST::DeclarationContext((::CppSharp::CppParser::AST::DeclarationContext*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::Namespace();
}

bool CppSharp::Parser::AST::Namespace::IsInline::get()
{
    return ((::CppSharp::CppParser::AST::Namespace*)NativePtr)->IsInline;
}

void CppSharp::Parser::AST::Namespace::IsInline::set(bool value)
{
    ((::CppSharp::CppParser::AST::Namespace*)NativePtr)->IsInline = value;
}

CppSharp::Parser::AST::PreprocessedEntity::PreprocessedEntity(::CppSharp::CppParser::AST::PreprocessedEntity* native)
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)native)
{
}

CppSharp::Parser::AST::PreprocessedEntity^ CppSharp::Parser::AST::PreprocessedEntity::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*) native.ToPointer());
}

CppSharp::Parser::AST::PreprocessedEntity::PreprocessedEntity()
    : CppSharp::Parser::AST::Declaration((::CppSharp::CppParser::AST::Declaration*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::PreprocessedEntity();
}

CppSharp::Parser::AST::MacroLocation CppSharp::Parser::AST::PreprocessedEntity::MacroLocation::get()
{
    return (CppSharp::Parser::AST::MacroLocation)((::CppSharp::CppParser::AST::PreprocessedEntity*)NativePtr)->MacroLocation;
}

void CppSharp::Parser::AST::PreprocessedEntity::MacroLocation::set(CppSharp::Parser::AST::MacroLocation value)
{
    ((::CppSharp::CppParser::AST::PreprocessedEntity*)NativePtr)->MacroLocation = (::CppSharp::CppParser::AST::MacroLocation)value;
}

CppSharp::Parser::AST::MacroDefinition::MacroDefinition(::CppSharp::CppParser::AST::MacroDefinition* native)
    : CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)native)
{
}

CppSharp::Parser::AST::MacroDefinition^ CppSharp::Parser::AST::MacroDefinition::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::MacroDefinition((::CppSharp::CppParser::AST::MacroDefinition*) native.ToPointer());
}

CppSharp::Parser::AST::MacroDefinition::MacroDefinition()
    : CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::MacroDefinition();
}

System::String^ CppSharp::Parser::AST::MacroDefinition::Expression::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::MacroDefinition*)NativePtr)->getExpression();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::MacroDefinition::Expression::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::MacroDefinition*)NativePtr)->setExpression(arg0);
}

CppSharp::Parser::AST::MacroExpansion::MacroExpansion(::CppSharp::CppParser::AST::MacroExpansion* native)
    : CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)native)
{
}

CppSharp::Parser::AST::MacroExpansion^ CppSharp::Parser::AST::MacroExpansion::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::MacroExpansion((::CppSharp::CppParser::AST::MacroExpansion*) native.ToPointer());
}

CppSharp::Parser::AST::MacroExpansion::MacroExpansion()
    : CppSharp::Parser::AST::PreprocessedEntity((::CppSharp::CppParser::AST::PreprocessedEntity*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::MacroExpansion();
}

System::String^ CppSharp::Parser::AST::MacroExpansion::Text::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->getText();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::MacroExpansion::Text::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->setText(arg0);
}

CppSharp::Parser::AST::MacroDefinition^ CppSharp::Parser::AST::MacroExpansion::Definition::get()
{
    return (((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->Definition == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::MacroDefinition((::CppSharp::CppParser::AST::MacroDefinition*)((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->Definition);
}

void CppSharp::Parser::AST::MacroExpansion::Definition::set(CppSharp::Parser::AST::MacroDefinition^ value)
{
    ((::CppSharp::CppParser::AST::MacroExpansion*)NativePtr)->Definition = (::CppSharp::CppParser::AST::MacroDefinition*)value->NativePtr;
}

CppSharp::Parser::AST::TranslationUnit::TranslationUnit(::CppSharp::CppParser::AST::TranslationUnit* native)
    : CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)native)
{
}

CppSharp::Parser::AST::TranslationUnit^ CppSharp::Parser::AST::TranslationUnit::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::TranslationUnit((::CppSharp::CppParser::AST::TranslationUnit*) native.ToPointer());
}

CppSharp::Parser::AST::TranslationUnit::TranslationUnit()
    : CppSharp::Parser::AST::Namespace((::CppSharp::CppParser::AST::Namespace*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::TranslationUnit();
}

CppSharp::Parser::AST::MacroDefinition^ CppSharp::Parser::AST::TranslationUnit::getMacros(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->getMacros(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::MacroDefinition((::CppSharp::CppParser::AST::MacroDefinition*)__ret);
}

void CppSharp::Parser::AST::TranslationUnit::addMacros(CppSharp::Parser::AST::MacroDefinition^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::MacroDefinition*)s->NativePtr;
    ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->addMacros(arg0);
}

void CppSharp::Parser::AST::TranslationUnit::clearMacros()
{
    ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->clearMacros();
}

System::String^ CppSharp::Parser::AST::TranslationUnit::FileName::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->getFileName();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::TranslationUnit::FileName::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->setFileName(arg0);
}

unsigned int CppSharp::Parser::AST::TranslationUnit::MacrosCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->getMacrosCount();
    return __ret;
}

bool CppSharp::Parser::AST::TranslationUnit::IsSystemHeader::get()
{
    return ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->IsSystemHeader;
}

void CppSharp::Parser::AST::TranslationUnit::IsSystemHeader::set(bool value)
{
    ((::CppSharp::CppParser::AST::TranslationUnit*)NativePtr)->IsSystemHeader = value;
}

CppSharp::Parser::AST::NativeLibrary::NativeLibrary(::CppSharp::CppParser::AST::NativeLibrary* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::NativeLibrary^ CppSharp::Parser::AST::NativeLibrary::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::NativeLibrary((::CppSharp::CppParser::AST::NativeLibrary*) native.ToPointer());
}

CppSharp::Parser::AST::NativeLibrary::NativeLibrary()
{
    NativePtr = new ::CppSharp::CppParser::AST::NativeLibrary();
}

System::String^ CppSharp::Parser::AST::NativeLibrary::getSymbols(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->getSymbols(i);
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::NativeLibrary::addSymbols(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->addSymbols(arg0);
}

void CppSharp::Parser::AST::NativeLibrary::clearSymbols()
{
    ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->clearSymbols();
}

System::String^ CppSharp::Parser::AST::NativeLibrary::getDependencies(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->getDependencies(i);
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::NativeLibrary::addDependencies(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->addDependencies(arg0);
}

void CppSharp::Parser::AST::NativeLibrary::clearDependencies()
{
    ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->clearDependencies();
}

System::IntPtr CppSharp::Parser::AST::NativeLibrary::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::NativeLibrary::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::NativeLibrary*)object.ToPointer();
}

System::String^ CppSharp::Parser::AST::NativeLibrary::FileName::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->getFileName();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::NativeLibrary::FileName::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->setFileName(arg0);
}

unsigned int CppSharp::Parser::AST::NativeLibrary::SymbolsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->getSymbolsCount();
    return __ret;
}

unsigned int CppSharp::Parser::AST::NativeLibrary::DependenciesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->getDependenciesCount();
    return __ret;
}

CppSharp::Parser::AST::ArchType CppSharp::Parser::AST::NativeLibrary::ArchType::get()
{
    return (CppSharp::Parser::AST::ArchType)((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->ArchType;
}

void CppSharp::Parser::AST::NativeLibrary::ArchType::set(CppSharp::Parser::AST::ArchType value)
{
    ((::CppSharp::CppParser::AST::NativeLibrary*)NativePtr)->ArchType = (::CppSharp::CppParser::AST::ArchType)value;
}

CppSharp::Parser::AST::ASTContext::ASTContext(::CppSharp::CppParser::AST::ASTContext* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::ASTContext^ CppSharp::Parser::AST::ASTContext::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*) native.ToPointer());
}

CppSharp::Parser::AST::ASTContext::ASTContext()
{
    NativePtr = new ::CppSharp::CppParser::AST::ASTContext();
}

CppSharp::Parser::AST::TranslationUnit^ CppSharp::Parser::AST::ASTContext::getTranslationUnits(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->getTranslationUnits(i);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::TranslationUnit((::CppSharp::CppParser::AST::TranslationUnit*)__ret);
}

void CppSharp::Parser::AST::ASTContext::addTranslationUnits(CppSharp::Parser::AST::TranslationUnit^ s)
{
    auto arg0 = (::CppSharp::CppParser::AST::TranslationUnit*)s->NativePtr;
    ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->addTranslationUnits(arg0);
}

void CppSharp::Parser::AST::ASTContext::clearTranslationUnits()
{
    ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->clearTranslationUnits();
}

System::IntPtr CppSharp::Parser::AST::ASTContext::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::ASTContext::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::ASTContext*)object.ToPointer();
}

unsigned int CppSharp::Parser::AST::ASTContext::TranslationUnitsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::ASTContext*)NativePtr)->getTranslationUnitsCount();
    return __ret;
}

CppSharp::Parser::AST::Comment::Comment(::CppSharp::CppParser::AST::Comment* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::Comment^ CppSharp::Parser::AST::Comment::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::Comment((::CppSharp::CppParser::AST::Comment*) native.ToPointer());
}

CppSharp::Parser::AST::Comment::Comment(CppSharp::Parser::AST::CommentKind kind)
{
    auto arg0 = (::CppSharp::CppParser::AST::CommentKind)kind;
    NativePtr = new ::CppSharp::CppParser::AST::Comment(arg0);
}

System::IntPtr CppSharp::Parser::AST::Comment::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::Comment::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::Comment*)object.ToPointer();
}

CppSharp::Parser::AST::CommentKind CppSharp::Parser::AST::Comment::Kind::get()
{
    return (CppSharp::Parser::AST::CommentKind)((::CppSharp::CppParser::AST::Comment*)NativePtr)->Kind;
}

void CppSharp::Parser::AST::Comment::Kind::set(CppSharp::Parser::AST::CommentKind value)
{
    ((::CppSharp::CppParser::AST::Comment*)NativePtr)->Kind = (::CppSharp::CppParser::AST::CommentKind)value;
}

CppSharp::Parser::AST::FullComment::FullComment(::CppSharp::CppParser::AST::FullComment* native)
    : CppSharp::Parser::AST::Comment((::CppSharp::CppParser::AST::Comment*)native)
{
}

CppSharp::Parser::AST::FullComment^ CppSharp::Parser::AST::FullComment::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::FullComment((::CppSharp::CppParser::AST::FullComment*) native.ToPointer());
}

CppSharp::Parser::AST::FullComment::FullComment()
    : CppSharp::Parser::AST::Comment((::CppSharp::CppParser::AST::Comment*)nullptr)
{
    NativePtr = new ::CppSharp::CppParser::AST::FullComment();
}

CppSharp::Parser::AST::RawComment::RawComment(::CppSharp::CppParser::AST::RawComment* native)
{
    NativePtr = native;
}

CppSharp::Parser::AST::RawComment^ CppSharp::Parser::AST::RawComment::__CreateInstance(::System::IntPtr native)
{
    return gcnew CppSharp::Parser::AST::RawComment((::CppSharp::CppParser::AST::RawComment*) native.ToPointer());
}

CppSharp::Parser::AST::RawComment::RawComment()
{
    NativePtr = new ::CppSharp::CppParser::AST::RawComment();
}

System::IntPtr CppSharp::Parser::AST::RawComment::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::AST::RawComment::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::AST::RawComment*)object.ToPointer();
}

System::String^ CppSharp::Parser::AST::RawComment::Text::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->getText();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::RawComment::Text::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->setText(arg0);
}

System::String^ CppSharp::Parser::AST::RawComment::BriefText::get()
{
    auto __ret = ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->getBriefText();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::AST::RawComment::BriefText::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->setBriefText(arg0);
}

CppSharp::Parser::AST::RawCommentKind CppSharp::Parser::AST::RawComment::Kind::get()
{
    return (CppSharp::Parser::AST::RawCommentKind)((::CppSharp::CppParser::AST::RawComment*)NativePtr)->Kind;
}

void CppSharp::Parser::AST::RawComment::Kind::set(CppSharp::Parser::AST::RawCommentKind value)
{
    ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->Kind = (::CppSharp::CppParser::AST::RawCommentKind)value;
}

CppSharp::Parser::AST::FullComment^ CppSharp::Parser::AST::RawComment::FullCommentBlock::get()
{
    return (((::CppSharp::CppParser::AST::RawComment*)NativePtr)->FullCommentBlock == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::FullComment((::CppSharp::CppParser::AST::FullComment*)((::CppSharp::CppParser::AST::RawComment*)NativePtr)->FullCommentBlock);
}

void CppSharp::Parser::AST::RawComment::FullCommentBlock::set(CppSharp::Parser::AST::FullComment^ value)
{
    ((::CppSharp::CppParser::AST::RawComment*)NativePtr)->FullCommentBlock = (::CppSharp::CppParser::AST::FullComment*)value->NativePtr;
}

