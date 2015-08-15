#include "Target.h"

using namespace System;
using namespace System::Runtime::InteropServices;

CppSharp::Parser::ParserTargetInfo::ParserTargetInfo(::CppSharp::CppParser::ParserTargetInfo* native)
    : __ownsNativeInstance(false)
{
    NativePtr = native;
}

CppSharp::Parser::ParserTargetInfo^ CppSharp::Parser::ParserTargetInfo::__CreateInstance(::System::IntPtr native)
{
    return ::CppSharp::Parser::ParserTargetInfo::__CreateInstance(native, false);
}

CppSharp::Parser::ParserTargetInfo^ CppSharp::Parser::ParserTargetInfo::__CreateInstance(::System::IntPtr native, bool __ownsNativeInstance)
{
    ::CppSharp::Parser::ParserTargetInfo^ result = gcnew ::CppSharp::Parser::ParserTargetInfo((::CppSharp::CppParser::ParserTargetInfo*) native.ToPointer());
    result->__ownsNativeInstance = __ownsNativeInstance;
    return result;
}

CppSharp::Parser::ParserTargetInfo::~ParserTargetInfo()
{
    if (__ownsNativeInstance)
        delete NativePtr;
}

CppSharp::Parser::ParserTargetInfo::ParserTargetInfo()
{
    __ownsNativeInstance = true;
    NativePtr = new ::CppSharp::CppParser::ParserTargetInfo();
}

CppSharp::Parser::ParserTargetInfo::ParserTargetInfo(CppSharp::Parser::ParserTargetInfo^ _0)
{
    __ownsNativeInstance = true;
    auto &arg0 = *(::CppSharp::CppParser::ParserTargetInfo*)_0->NativePtr;
    NativePtr = new ::CppSharp::CppParser::ParserTargetInfo(arg0);
}

System::IntPtr CppSharp::Parser::ParserTargetInfo::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ParserTargetInfo::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ParserTargetInfo*)object.ToPointer();
}

System::String^ CppSharp::Parser::ParserTargetInfo::ABI::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->getABI();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserTargetInfo::ABI::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->setABI(arg0);
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::Char16Type::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char16Type;
}

void CppSharp::Parser::ParserTargetInfo::Char16Type::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char16Type = (::CppSharp::CppParser::ParserIntType)value;
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::Char32Type::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char32Type;
}

void CppSharp::Parser::ParserTargetInfo::Char32Type::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char32Type = (::CppSharp::CppParser::ParserIntType)value;
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::Int64Type::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Int64Type;
}

void CppSharp::Parser::ParserTargetInfo::Int64Type::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Int64Type = (::CppSharp::CppParser::ParserIntType)value;
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::IntMaxType::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntMaxType;
}

void CppSharp::Parser::ParserTargetInfo::IntMaxType::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntMaxType = (::CppSharp::CppParser::ParserIntType)value;
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::IntPtrType::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntPtrType;
}

void CppSharp::Parser::ParserTargetInfo::IntPtrType::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntPtrType = (::CppSharp::CppParser::ParserIntType)value;
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::SizeType::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->SizeType;
}

void CppSharp::Parser::ParserTargetInfo::SizeType::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->SizeType = (::CppSharp::CppParser::ParserIntType)value;
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::UIntMaxType::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->UIntMaxType;
}

void CppSharp::Parser::ParserTargetInfo::UIntMaxType::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->UIntMaxType = (::CppSharp::CppParser::ParserIntType)value;
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::WCharType::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->WCharType;
}

void CppSharp::Parser::ParserTargetInfo::WCharType::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->WCharType = (::CppSharp::CppParser::ParserIntType)value;
}

CppSharp::Parser::ParserIntType CppSharp::Parser::ParserTargetInfo::WIntType::get()
{
    return (CppSharp::Parser::ParserIntType)((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->WIntType;
}

void CppSharp::Parser::ParserTargetInfo::WIntType::set(CppSharp::Parser::ParserIntType value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->WIntType = (::CppSharp::CppParser::ParserIntType)value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::BoolAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->BoolAlign;
}

void CppSharp::Parser::ParserTargetInfo::BoolAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->BoolAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::BoolWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->BoolWidth;
}

void CppSharp::Parser::ParserTargetInfo::BoolWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->BoolWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::CharAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->CharAlign;
}

void CppSharp::Parser::ParserTargetInfo::CharAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->CharAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::CharWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->CharWidth;
}

void CppSharp::Parser::ParserTargetInfo::CharWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->CharWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::Char16Align::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char16Align;
}

void CppSharp::Parser::ParserTargetInfo::Char16Align::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char16Align = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::Char16Width::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char16Width;
}

void CppSharp::Parser::ParserTargetInfo::Char16Width::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char16Width = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::Char32Align::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char32Align;
}

void CppSharp::Parser::ParserTargetInfo::Char32Align::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char32Align = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::Char32Width::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char32Width;
}

void CppSharp::Parser::ParserTargetInfo::Char32Width::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->Char32Width = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::HalfAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->HalfAlign;
}

void CppSharp::Parser::ParserTargetInfo::HalfAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->HalfAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::HalfWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->HalfWidth;
}

void CppSharp::Parser::ParserTargetInfo::HalfWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->HalfWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::FloatAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->FloatAlign;
}

void CppSharp::Parser::ParserTargetInfo::FloatAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->FloatAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::FloatWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->FloatWidth;
}

void CppSharp::Parser::ParserTargetInfo::FloatWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->FloatWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::DoubleAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->DoubleAlign;
}

void CppSharp::Parser::ParserTargetInfo::DoubleAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->DoubleAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::DoubleWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->DoubleWidth;
}

void CppSharp::Parser::ParserTargetInfo::DoubleWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->DoubleWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::ShortAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->ShortAlign;
}

void CppSharp::Parser::ParserTargetInfo::ShortAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->ShortAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::ShortWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->ShortWidth;
}

void CppSharp::Parser::ParserTargetInfo::ShortWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->ShortWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::IntAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntAlign;
}

void CppSharp::Parser::ParserTargetInfo::IntAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::IntWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntWidth;
}

void CppSharp::Parser::ParserTargetInfo::IntWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::IntMaxTWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntMaxTWidth;
}

void CppSharp::Parser::ParserTargetInfo::IntMaxTWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->IntMaxTWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::LongAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongAlign;
}

void CppSharp::Parser::ParserTargetInfo::LongAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::LongWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongWidth;
}

void CppSharp::Parser::ParserTargetInfo::LongWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::LongDoubleAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongDoubleAlign;
}

void CppSharp::Parser::ParserTargetInfo::LongDoubleAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongDoubleAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::LongDoubleWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongDoubleWidth;
}

void CppSharp::Parser::ParserTargetInfo::LongDoubleWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongDoubleWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::LongLongAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongLongAlign;
}

void CppSharp::Parser::ParserTargetInfo::LongLongAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongLongAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::LongLongWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongLongWidth;
}

void CppSharp::Parser::ParserTargetInfo::LongLongWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->LongLongWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::PointerAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->PointerAlign;
}

void CppSharp::Parser::ParserTargetInfo::PointerAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->PointerAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::PointerWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->PointerWidth;
}

void CppSharp::Parser::ParserTargetInfo::PointerWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->PointerWidth = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::WCharAlign::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->WCharAlign;
}

void CppSharp::Parser::ParserTargetInfo::WCharAlign::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->WCharAlign = value;
}

unsigned int CppSharp::Parser::ParserTargetInfo::WCharWidth::get()
{
    return ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->WCharWidth;
}

void CppSharp::Parser::ParserTargetInfo::WCharWidth::set(unsigned int value)
{
    ((::CppSharp::CppParser::ParserTargetInfo*)NativePtr)->WCharWidth = value;
}

