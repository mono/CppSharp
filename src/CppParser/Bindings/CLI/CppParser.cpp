#include "CppParser.h"
#include "AST.h"

using namespace System;
using namespace System::Runtime::InteropServices;

CppSharp::Parser::ParserOptions::ParserOptions(::CppSharp::CppParser::ParserOptions* native)
{
    NativePtr = native;
}

CppSharp::Parser::ParserOptions::ParserOptions(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::ParserOptions*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::ParserOptions::ParserOptions()
{
    NativePtr = new ::CppSharp::CppParser::ParserOptions();
}

System::IntPtr CppSharp::Parser::ParserOptions::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ParserOptions::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ParserOptions*)object.ToPointer();
}

System::Collections::Generic::List<System::String^>^ CppSharp::Parser::ParserOptions::IncludeDirs::get()
{
    auto _tmpIncludeDirs = gcnew System::Collections::Generic::List<System::String^>();
    for(auto _element : ((::CppSharp::CppParser::ParserOptions*)NativePtr)->IncludeDirs)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpIncludeDirs->Add(_marshalElement);
    }
    return _tmpIncludeDirs;
}

void CppSharp::Parser::ParserOptions::IncludeDirs::set(System::Collections::Generic::List<System::String^>^ value)
{
    auto _tmpvalue = std::vector<::std::string>();
    for each(System::String^ _element in value)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->IncludeDirs = _tmpvalue;
}

System::Collections::Generic::List<System::String^>^ CppSharp::Parser::ParserOptions::SystemIncludeDirs::get()
{
    auto _tmpSystemIncludeDirs = gcnew System::Collections::Generic::List<System::String^>();
    for(auto _element : ((::CppSharp::CppParser::ParserOptions*)NativePtr)->SystemIncludeDirs)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpSystemIncludeDirs->Add(_marshalElement);
    }
    return _tmpSystemIncludeDirs;
}

void CppSharp::Parser::ParserOptions::SystemIncludeDirs::set(System::Collections::Generic::List<System::String^>^ value)
{
    auto _tmpvalue = std::vector<::std::string>();
    for each(System::String^ _element in value)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->SystemIncludeDirs = _tmpvalue;
}

System::Collections::Generic::List<System::String^>^ CppSharp::Parser::ParserOptions::Defines::get()
{
    auto _tmpDefines = gcnew System::Collections::Generic::List<System::String^>();
    for(auto _element : ((::CppSharp::CppParser::ParserOptions*)NativePtr)->Defines)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpDefines->Add(_marshalElement);
    }
    return _tmpDefines;
}

void CppSharp::Parser::ParserOptions::Defines::set(System::Collections::Generic::List<System::String^>^ value)
{
    auto _tmpvalue = std::vector<::std::string>();
    for each(System::String^ _element in value)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->Defines = _tmpvalue;
}

System::Collections::Generic::List<System::String^>^ CppSharp::Parser::ParserOptions::LibraryDirs::get()
{
    auto _tmpLibraryDirs = gcnew System::Collections::Generic::List<System::String^>();
    for(auto _element : ((::CppSharp::CppParser::ParserOptions*)NativePtr)->LibraryDirs)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpLibraryDirs->Add(_marshalElement);
    }
    return _tmpLibraryDirs;
}

void CppSharp::Parser::ParserOptions::LibraryDirs::set(System::Collections::Generic::List<System::String^>^ value)
{
    auto _tmpvalue = std::vector<::std::string>();
    for each(System::String^ _element in value)
    {
        auto _marshalElement = clix::marshalString<clix::E_UTF8>(_element);
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->LibraryDirs = _tmpvalue;
}

System::String^ CppSharp::Parser::ParserOptions::FileName::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::ParserOptions*)NativePtr)->FileName);
}

void CppSharp::Parser::ParserOptions::FileName::set(System::String^ value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->FileName = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::AST::Library^ CppSharp::Parser::ParserOptions::Library::get()
{
    return gcnew CppSharp::Parser::AST::Library((::CppSharp::CppParser::AST::Library*)((::CppSharp::CppParser::ParserOptions*)NativePtr)->Library);
}

void CppSharp::Parser::ParserOptions::Library::set(CppSharp::Parser::AST::Library^ value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->Library = (::CppSharp::CppParser::AST::Library*)value->NativePtr;
}

int CppSharp::Parser::ParserOptions::ToolSetToUse::get()
{
    return ((::CppSharp::CppParser::ParserOptions*)NativePtr)->ToolSetToUse;
}

void CppSharp::Parser::ParserOptions::ToolSetToUse::set(int value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->ToolSetToUse = value;
}

System::String^ CppSharp::Parser::ParserOptions::TargetTriple::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::ParserOptions*)NativePtr)->TargetTriple);
}

void CppSharp::Parser::ParserOptions::TargetTriple::set(System::String^ value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->TargetTriple = clix::marshalString<clix::E_UTF8>(value);
}

bool CppSharp::Parser::ParserOptions::NoStandardIncludes::get()
{
    return ((::CppSharp::CppParser::ParserOptions*)NativePtr)->NoStandardIncludes;
}

void CppSharp::Parser::ParserOptions::NoStandardIncludes::set(bool value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->NoStandardIncludes = value;
}

bool CppSharp::Parser::ParserOptions::NoBuiltinIncludes::get()
{
    return ((::CppSharp::CppParser::ParserOptions*)NativePtr)->NoBuiltinIncludes;
}

void CppSharp::Parser::ParserOptions::NoBuiltinIncludes::set(bool value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->NoBuiltinIncludes = value;
}

bool CppSharp::Parser::ParserOptions::MicrosoftMode::get()
{
    return ((::CppSharp::CppParser::ParserOptions*)NativePtr)->MicrosoftMode;
}

void CppSharp::Parser::ParserOptions::MicrosoftMode::set(bool value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->MicrosoftMode = value;
}

CppSharp::Parser::AST::CppAbi CppSharp::Parser::ParserOptions::Abi::get()
{
    return (CppSharp::Parser::AST::CppAbi)((::CppSharp::CppParser::ParserOptions*)NativePtr)->Abi;
}

void CppSharp::Parser::ParserOptions::Abi::set(CppSharp::Parser::AST::CppAbi value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->Abi = (::CppSharp::CppParser::AST::CppAbi)value;
}

bool CppSharp::Parser::ParserOptions::Verbose::get()
{
    return ((::CppSharp::CppParser::ParserOptions*)NativePtr)->Verbose;
}

void CppSharp::Parser::ParserOptions::Verbose::set(bool value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->Verbose = value;
}

CppSharp::Parser::ParserDiagnostic::ParserDiagnostic(::CppSharp::CppParser::ParserDiagnostic* native)
{
    NativePtr = native;
}

CppSharp::Parser::ParserDiagnostic::ParserDiagnostic(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::ParserDiagnostic*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::ParserDiagnostic::ParserDiagnostic()
{
    NativePtr = new ::CppSharp::CppParser::ParserDiagnostic();
}

System::IntPtr CppSharp::Parser::ParserDiagnostic::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ParserDiagnostic::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ParserDiagnostic*)object.ToPointer();
}

System::String^ CppSharp::Parser::ParserDiagnostic::FileName::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->FileName);
}

void CppSharp::Parser::ParserDiagnostic::FileName::set(System::String^ value)
{
    ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->FileName = clix::marshalString<clix::E_UTF8>(value);
}

System::String^ CppSharp::Parser::ParserDiagnostic::Message::get()
{
    return clix::marshalString<clix::E_UTF8>(((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->Message);
}

void CppSharp::Parser::ParserDiagnostic::Message::set(System::String^ value)
{
    ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->Message = clix::marshalString<clix::E_UTF8>(value);
}

CppSharp::Parser::ParserDiagnosticLevel CppSharp::Parser::ParserDiagnostic::Level::get()
{
    return (CppSharp::Parser::ParserDiagnosticLevel)((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->Level;
}

void CppSharp::Parser::ParserDiagnostic::Level::set(CppSharp::Parser::ParserDiagnosticLevel value)
{
    ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->Level = (::CppSharp::CppParser::ParserDiagnosticLevel)value;
}

int CppSharp::Parser::ParserDiagnostic::LineNumber::get()
{
    return ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->LineNumber;
}

void CppSharp::Parser::ParserDiagnostic::LineNumber::set(int value)
{
    ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->LineNumber = value;
}

int CppSharp::Parser::ParserDiagnostic::ColumnNumber::get()
{
    return ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->ColumnNumber;
}

void CppSharp::Parser::ParserDiagnostic::ColumnNumber::set(int value)
{
    ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->ColumnNumber = value;
}

CppSharp::Parser::ParserResult::ParserResult(::CppSharp::CppParser::ParserResult* native)
{
    NativePtr = native;
}

CppSharp::Parser::ParserResult::ParserResult(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::ParserResult*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::ParserResult::ParserResult()
{
    NativePtr = new ::CppSharp::CppParser::ParserResult();
}

System::IntPtr CppSharp::Parser::ParserResult::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ParserResult::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ParserResult*)object.ToPointer();
}

CppSharp::Parser::ParserResultKind CppSharp::Parser::ParserResult::Kind::get()
{
    return (CppSharp::Parser::ParserResultKind)((::CppSharp::CppParser::ParserResult*)NativePtr)->Kind;
}

void CppSharp::Parser::ParserResult::Kind::set(CppSharp::Parser::ParserResultKind value)
{
    ((::CppSharp::CppParser::ParserResult*)NativePtr)->Kind = (::CppSharp::CppParser::ParserResultKind)value;
}

CppSharp::Parser::AST::Library^ CppSharp::Parser::ParserResult::Library::get()
{
    return gcnew CppSharp::Parser::AST::Library((::CppSharp::CppParser::AST::Library*)((::CppSharp::CppParser::ParserResult*)NativePtr)->Library);
}

void CppSharp::Parser::ParserResult::Library::set(CppSharp::Parser::AST::Library^ value)
{
    ((::CppSharp::CppParser::ParserResult*)NativePtr)->Library = (::CppSharp::CppParser::AST::Library*)value->NativePtr;
}

System::Collections::Generic::List<CppSharp::Parser::ParserDiagnostic^>^ CppSharp::Parser::ParserResult::Diagnostics::get()
{
    auto _tmpDiagnostics = gcnew System::Collections::Generic::List<CppSharp::Parser::ParserDiagnostic^>();
    for(auto _element : ((::CppSharp::CppParser::ParserResult*)NativePtr)->Diagnostics)
    {
        auto ___element = new ::CppSharp::CppParser::ParserDiagnostic(_element);
        auto _marshalElement = gcnew CppSharp::Parser::ParserDiagnostic((::CppSharp::CppParser::ParserDiagnostic*)___element);
        _tmpDiagnostics->Add(_marshalElement);
    }
    return _tmpDiagnostics;
}

void CppSharp::Parser::ParserResult::Diagnostics::set(System::Collections::Generic::List<CppSharp::Parser::ParserDiagnostic^>^ value)
{
    auto _tmpvalue = std::vector<::CppSharp::CppParser::ParserDiagnostic>();
    for each(CppSharp::Parser::ParserDiagnostic^ _element in value)
    {
        auto _marshalElement = *(::CppSharp::CppParser::ParserDiagnostic*)_element->NativePtr;
        _tmpvalue.push_back(_marshalElement);
    }
    ((::CppSharp::CppParser::ParserResult*)NativePtr)->Diagnostics = _tmpvalue;
}

CppSharp::Parser::ClangParser::ClangParser(::CppSharp::CppParser::ClangParser* native)
{
    NativePtr = native;
}

CppSharp::Parser::ClangParser::ClangParser(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::ClangParser*)native.ToPointer();
    NativePtr = __native;
}

CppSharp::Parser::ParserResult^ CppSharp::Parser::ClangParser::ParseHeader(CppSharp::Parser::ParserOptions^ Opts)
{
    auto arg0 = (::CppSharp::CppParser::ParserOptions*)Opts->NativePtr;
    auto __ret = ::CppSharp::CppParser::ClangParser::ParseHeader(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::ParserResult((::CppSharp::CppParser::ParserResult*)__ret);
}

CppSharp::Parser::ParserResult^ CppSharp::Parser::ClangParser::ParseLibrary(CppSharp::Parser::ParserOptions^ Opts)
{
    auto arg0 = (::CppSharp::CppParser::ParserOptions*)Opts->NativePtr;
    auto __ret = ::CppSharp::CppParser::ClangParser::ParseLibrary(arg0);
    if (__ret == nullptr) return nullptr;
    return gcnew CppSharp::Parser::ParserResult((::CppSharp::CppParser::ParserResult*)__ret);
}

CppSharp::Parser::ClangParser::ClangParser()
{
    NativePtr = new ::CppSharp::CppParser::ClangParser();
}

System::IntPtr CppSharp::Parser::ClangParser::Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ClangParser::Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ClangParser*)object.ToPointer();
}
