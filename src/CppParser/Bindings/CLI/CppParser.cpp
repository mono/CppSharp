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

System::String^ CppSharp::Parser::ParserOptions::getIncludeDirs(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getIncludeDirs(i);
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::addIncludeDirs(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->addIncludeDirs(arg0);
}

System::String^ CppSharp::Parser::ParserOptions::getSystemIncludeDirs(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getSystemIncludeDirs(i);
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::addSystemIncludeDirs(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->addSystemIncludeDirs(arg0);
}

System::String^ CppSharp::Parser::ParserOptions::getDefines(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getDefines(i);
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::addDefines(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->addDefines(arg0);
}

System::String^ CppSharp::Parser::ParserOptions::getLibraryDirs(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getLibraryDirs(i);
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::addLibraryDirs(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->addLibraryDirs(arg0);
}

System::IntPtr CppSharp::Parser::ParserOptions::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ParserOptions::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ParserOptions*)object.ToPointer();
}

System::String^ CppSharp::Parser::ParserOptions::FileName::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getFileName();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::FileName::set(System::String^ value)
{
    auto s = value;
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->setFileName(arg0);
}

unsigned int CppSharp::Parser::ParserOptions::IncludeDirsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getIncludeDirsCount();
    return __ret;
}

unsigned int CppSharp::Parser::ParserOptions::SystemIncludeDirsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getSystemIncludeDirsCount();
    return __ret;
}

unsigned int CppSharp::Parser::ParserOptions::DefinesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getDefinesCount();
    return __ret;
}

unsigned int CppSharp::Parser::ParserOptions::LibraryDirsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getLibraryDirsCount();
    return __ret;
}

System::String^ CppSharp::Parser::ParserOptions::TargetTriple::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getTargetTriple();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::TargetTriple::set(System::String^ value)
{
    auto s = value;
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->setTargetTriple(arg0);
}

CppSharp::Parser::AST::ASTContext^ CppSharp::Parser::ParserOptions::ASTContext::get()
{
    return gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)((::CppSharp::CppParser::ParserOptions*)NativePtr)->ASTContext);
}

void CppSharp::Parser::ParserOptions::ASTContext::set(CppSharp::Parser::AST::ASTContext^ value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->ASTContext = (::CppSharp::CppParser::AST::ASTContext*)value->NativePtr;
}

int CppSharp::Parser::ParserOptions::ToolSetToUse::get()
{
    return ((::CppSharp::CppParser::ParserOptions*)NativePtr)->ToolSetToUse;
}

void CppSharp::Parser::ParserOptions::ToolSetToUse::set(int value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->ToolSetToUse = value;
}

CppSharp::Parser::AST::CppAbi CppSharp::Parser::ParserOptions::Abi::get()
{
    return (CppSharp::Parser::AST::CppAbi)((::CppSharp::CppParser::ParserOptions*)NativePtr)->Abi;
}

void CppSharp::Parser::ParserOptions::Abi::set(CppSharp::Parser::AST::CppAbi value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->Abi = (::CppSharp::CppParser::AST::CppAbi)value;
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

System::IntPtr CppSharp::Parser::ParserDiagnostic::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ParserDiagnostic::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ParserDiagnostic*)object.ToPointer();
}

System::String^ CppSharp::Parser::ParserDiagnostic::FileName::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->getFileName();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserDiagnostic::FileName::set(System::String^ value)
{
    auto s = value;
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->setFileName(arg0);
}

System::String^ CppSharp::Parser::ParserDiagnostic::Message::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->getMessage();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserDiagnostic::Message::set(System::String^ value)
{
    auto s = value;
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserDiagnostic*)NativePtr)->setMessage(arg0);
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

CppSharp::Parser::ParserDiagnostic^ CppSharp::Parser::ParserResult::getDiagnostics(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::ParserResult*)NativePtr)->getDiagnostics(i);
    auto ____ret = new ::CppSharp::CppParser::ParserDiagnostic(__ret);
    return gcnew CppSharp::Parser::ParserDiagnostic((::CppSharp::CppParser::ParserDiagnostic*)____ret);
}

void CppSharp::Parser::ParserResult::addDiagnostics(CppSharp::Parser::ParserDiagnostic^ s)
{
    auto &arg0 = *(::CppSharp::CppParser::ParserDiagnostic*)s->NativePtr;
    ((::CppSharp::CppParser::ParserResult*)NativePtr)->addDiagnostics(arg0);
}

CppSharp::Parser::ParserResult::ParserResult()
{
    NativePtr = new ::CppSharp::CppParser::ParserResult();
}

System::IntPtr CppSharp::Parser::ParserResult::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ParserResult::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ParserResult*)object.ToPointer();
}

unsigned int CppSharp::Parser::ParserResult::DiagnosticsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserResult*)NativePtr)->getDiagnosticsCount();
    return __ret;
}

CppSharp::Parser::ParserResultKind CppSharp::Parser::ParserResult::Kind::get()
{
    return (CppSharp::Parser::ParserResultKind)((::CppSharp::CppParser::ParserResult*)NativePtr)->Kind;
}

void CppSharp::Parser::ParserResult::Kind::set(CppSharp::Parser::ParserResultKind value)
{
    ((::CppSharp::CppParser::ParserResult*)NativePtr)->Kind = (::CppSharp::CppParser::ParserResultKind)value;
}

CppSharp::Parser::AST::ASTContext^ CppSharp::Parser::ParserResult::ASTContext::get()
{
    return gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)((::CppSharp::CppParser::ParserResult*)NativePtr)->ASTContext);
}

void CppSharp::Parser::ParserResult::ASTContext::set(CppSharp::Parser::AST::ASTContext^ value)
{
    ((::CppSharp::CppParser::ParserResult*)NativePtr)->ASTContext = (::CppSharp::CppParser::AST::ASTContext*)value->NativePtr;
}

CppSharp::Parser::AST::NativeLibrary^ CppSharp::Parser::ParserResult::Library::get()
{
    return gcnew CppSharp::Parser::AST::NativeLibrary((::CppSharp::CppParser::AST::NativeLibrary*)((::CppSharp::CppParser::ParserResult*)NativePtr)->Library);
}

void CppSharp::Parser::ParserResult::Library::set(CppSharp::Parser::AST::NativeLibrary^ value)
{
    ((::CppSharp::CppParser::ParserResult*)NativePtr)->Library = (::CppSharp::CppParser::AST::NativeLibrary*)value->NativePtr;
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

System::IntPtr CppSharp::Parser::ClangParser::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ClangParser::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ClangParser*)object.ToPointer();
}
