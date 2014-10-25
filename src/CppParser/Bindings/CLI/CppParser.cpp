#include "CppParser.h"
#include "AST.h"
#include "Target.h"

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

System::String^ CppSharp::Parser::ParserOptions::getArguments(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getArguments(i);
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::addArguments(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->addArguments(arg0);
}

void CppSharp::Parser::ParserOptions::clearArguments()
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->clearArguments();
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

void CppSharp::Parser::ParserOptions::clearIncludeDirs()
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->clearIncludeDirs();
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

void CppSharp::Parser::ParserOptions::clearSystemIncludeDirs()
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->clearSystemIncludeDirs();
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

void CppSharp::Parser::ParserOptions::clearDefines()
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->clearDefines();
}

System::String^ CppSharp::Parser::ParserOptions::getUndefines(unsigned int i)
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getUndefines(i);
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::addUndefines(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->addUndefines(arg0);
}

void CppSharp::Parser::ParserOptions::clearUndefines()
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->clearUndefines();
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

void CppSharp::Parser::ParserOptions::clearLibraryDirs()
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->clearLibraryDirs();
}

System::IntPtr CppSharp::Parser::ParserOptions::__Instance::get()
{
    return System::IntPtr(NativePtr);
}

void CppSharp::Parser::ParserOptions::__Instance::set(System::IntPtr object)
{
    NativePtr = (::CppSharp::CppParser::ParserOptions*)object.ToPointer();
}

unsigned int CppSharp::Parser::ParserOptions::ArgumentsCount::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getArgumentsCount();
    return __ret;
}

System::String^ CppSharp::Parser::ParserOptions::FileName::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getFileName();
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserOptions::FileName::set(System::String^ s)
{
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

unsigned int CppSharp::Parser::ParserOptions::UndefinesCount::get()
{
    auto __ret = ((::CppSharp::CppParser::ParserOptions*)NativePtr)->getUndefinesCount();
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

void CppSharp::Parser::ParserOptions::TargetTriple::set(System::String^ s)
{
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->setTargetTriple(arg0);
}

CppSharp::Parser::AST::ASTContext^ CppSharp::Parser::ParserOptions::ASTContext::get()
{
    return (((::CppSharp::CppParser::ParserOptions*)NativePtr)->ASTContext == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)((::CppSharp::CppParser::ParserOptions*)NativePtr)->ASTContext);
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

CppSharp::Parser::LanguageVersion CppSharp::Parser::ParserOptions::LanguageVersion::get()
{
    return (CppSharp::Parser::LanguageVersion)((::CppSharp::CppParser::ParserOptions*)NativePtr)->LanguageVersion;
}

void CppSharp::Parser::ParserOptions::LanguageVersion::set(CppSharp::Parser::LanguageVersion value)
{
    ((::CppSharp::CppParser::ParserOptions*)NativePtr)->LanguageVersion = (::CppSharp::CppParser::LanguageVersion)value;
}

CppSharp::Parser::ParserDiagnostic::ParserDiagnostic(::CppSharp::CppParser::ParserDiagnostic* native)
{
    __Level = (CppSharp::Parser::ParserDiagnosticLevel)native->Level;
    __LineNumber = native->LineNumber;
    __ColumnNumber = native->ColumnNumber;
}

CppSharp::Parser::ParserDiagnostic::ParserDiagnostic(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::ParserDiagnostic*)native.ToPointer();
    __Level = (CppSharp::Parser::ParserDiagnosticLevel)__native->Level;
    __LineNumber = __native->LineNumber;
    __ColumnNumber = __native->ColumnNumber;
}

System::String^ CppSharp::Parser::ParserDiagnostic::FileName::get()
{
    auto _this0 = ::CppSharp::CppParser::ParserDiagnostic();
    _this0.Level = (::CppSharp::CppParser::ParserDiagnosticLevel)(*this).Level;
    _this0.LineNumber = (*this).LineNumber;
    _this0.ColumnNumber = (*this).ColumnNumber;
    auto __ret = _this0.getFileName();
    __Level = (CppSharp::Parser::ParserDiagnosticLevel)_this0.Level;
    __LineNumber = _this0.LineNumber;
    __ColumnNumber = _this0.ColumnNumber;
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserDiagnostic::FileName::set(System::String^ s)
{
    auto _this0 = ::CppSharp::CppParser::ParserDiagnostic();
    _this0.Level = (::CppSharp::CppParser::ParserDiagnosticLevel)(*this).Level;
    _this0.LineNumber = (*this).LineNumber;
    _this0.ColumnNumber = (*this).ColumnNumber;
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    _this0.setFileName(arg0);
    __Level = (CppSharp::Parser::ParserDiagnosticLevel)_this0.Level;
    __LineNumber = _this0.LineNumber;
    __ColumnNumber = _this0.ColumnNumber;
}

System::String^ CppSharp::Parser::ParserDiagnostic::Message::get()
{
    auto _this0 = ::CppSharp::CppParser::ParserDiagnostic();
    _this0.Level = (::CppSharp::CppParser::ParserDiagnosticLevel)(*this).Level;
    _this0.LineNumber = (*this).LineNumber;
    _this0.ColumnNumber = (*this).ColumnNumber;
    auto __ret = _this0.getMessage();
    __Level = (CppSharp::Parser::ParserDiagnosticLevel)_this0.Level;
    __LineNumber = _this0.LineNumber;
    __ColumnNumber = _this0.ColumnNumber;
    if (__ret == nullptr) return nullptr;
    return clix::marshalString<clix::E_UTF8>(__ret);
}

void CppSharp::Parser::ParserDiagnostic::Message::set(System::String^ s)
{
    auto _this0 = ::CppSharp::CppParser::ParserDiagnostic();
    _this0.Level = (::CppSharp::CppParser::ParserDiagnosticLevel)(*this).Level;
    _this0.LineNumber = (*this).LineNumber;
    _this0.ColumnNumber = (*this).ColumnNumber;
    auto _arg0 = clix::marshalString<clix::E_UTF8>(s);
    auto arg0 = _arg0.c_str();
    _this0.setMessage(arg0);
    __Level = (CppSharp::Parser::ParserDiagnosticLevel)_this0.Level;
    __LineNumber = _this0.LineNumber;
    __ColumnNumber = _this0.ColumnNumber;
}

CppSharp::Parser::ParserDiagnosticLevel CppSharp::Parser::ParserDiagnostic::Level::get()
{
    return __Level;
}

void CppSharp::Parser::ParserDiagnostic::Level::set(CppSharp::Parser::ParserDiagnosticLevel value)
{
    __Level = value;
}

int CppSharp::Parser::ParserDiagnostic::LineNumber::get()
{
    return __LineNumber;
}

void CppSharp::Parser::ParserDiagnostic::LineNumber::set(int value)
{
    __LineNumber = value;
}

int CppSharp::Parser::ParserDiagnostic::ColumnNumber::get()
{
    return __ColumnNumber;
}

void CppSharp::Parser::ParserDiagnostic::ColumnNumber::set(int value)
{
    __ColumnNumber = value;
}

CppSharp::Parser::ParserResult::ParserResult(::CppSharp::CppParser::ParserResult* native)
{
    __Kind = (CppSharp::Parser::ParserResultKind)native->Kind;
    __ASTContext = (native->ASTContext == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)native->ASTContext);
    __Library = (native->Library == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::NativeLibrary((::CppSharp::CppParser::AST::NativeLibrary*)native->Library);
}

CppSharp::Parser::ParserResult::ParserResult(System::IntPtr native)
{
    auto __native = (::CppSharp::CppParser::ParserResult*)native.ToPointer();
    __Kind = (CppSharp::Parser::ParserResultKind)__native->Kind;
    __ASTContext = (__native->ASTContext == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)__native->ASTContext);
    __Library = (__native->Library == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::NativeLibrary((::CppSharp::CppParser::AST::NativeLibrary*)__native->Library);
}

CppSharp::Parser::ParserDiagnostic CppSharp::Parser::ParserResult::getDiagnostics(unsigned int i)
{
    auto _this0 = ::CppSharp::CppParser::ParserResult();
    _this0.Kind = (::CppSharp::CppParser::ParserResultKind)(*this).Kind;
    if ((*this).ASTContext != nullptr)
        _this0.ASTContext = (::CppSharp::CppParser::AST::ASTContext*)(*this).ASTContext->NativePtr;
    if ((*this).Library != nullptr)
        _this0.Library = (::CppSharp::CppParser::AST::NativeLibrary*)(*this).Library->NativePtr;
    auto __ret = _this0.getDiagnostics(i);
    __Kind = (CppSharp::Parser::ParserResultKind)_this0.Kind;
    __ASTContext = (_this0.ASTContext == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)_this0.ASTContext);
    __Library = (_this0.Library == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::NativeLibrary((::CppSharp::CppParser::AST::NativeLibrary*)_this0.Library);
    return CppSharp::Parser::ParserDiagnostic((::CppSharp::CppParser::ParserDiagnostic*)&__ret);
}

void CppSharp::Parser::ParserResult::addDiagnostics(CppSharp::Parser::ParserDiagnostic s)
{
    auto _this0 = ::CppSharp::CppParser::ParserResult();
    _this0.Kind = (::CppSharp::CppParser::ParserResultKind)(*this).Kind;
    if ((*this).ASTContext != nullptr)
        _this0.ASTContext = (::CppSharp::CppParser::AST::ASTContext*)(*this).ASTContext->NativePtr;
    if ((*this).Library != nullptr)
        _this0.Library = (::CppSharp::CppParser::AST::NativeLibrary*)(*this).Library->NativePtr;
    auto _marshal0 = ::CppSharp::CppParser::ParserDiagnostic();
    _marshal0.Level = (::CppSharp::CppParser::ParserDiagnosticLevel)s.Level;
    _marshal0.LineNumber = s.LineNumber;
    _marshal0.ColumnNumber = s.ColumnNumber;
    auto arg0 = _marshal0;
    _this0.addDiagnostics(arg0);
    __Kind = (CppSharp::Parser::ParserResultKind)_this0.Kind;
    __ASTContext = (_this0.ASTContext == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)_this0.ASTContext);
    __Library = (_this0.Library == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::NativeLibrary((::CppSharp::CppParser::AST::NativeLibrary*)_this0.Library);
}

void CppSharp::Parser::ParserResult::clearDiagnostics()
{
    auto _this0 = ::CppSharp::CppParser::ParserResult();
    _this0.Kind = (::CppSharp::CppParser::ParserResultKind)(*this).Kind;
    if ((*this).ASTContext != nullptr)
        _this0.ASTContext = (::CppSharp::CppParser::AST::ASTContext*)(*this).ASTContext->NativePtr;
    if ((*this).Library != nullptr)
        _this0.Library = (::CppSharp::CppParser::AST::NativeLibrary*)(*this).Library->NativePtr;
    _this0.clearDiagnostics();
    __Kind = (CppSharp::Parser::ParserResultKind)_this0.Kind;
    __ASTContext = (_this0.ASTContext == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)_this0.ASTContext);
    __Library = (_this0.Library == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::NativeLibrary((::CppSharp::CppParser::AST::NativeLibrary*)_this0.Library);
}

unsigned int CppSharp::Parser::ParserResult::DiagnosticsCount::get()
{
    auto _this0 = ::CppSharp::CppParser::ParserResult();
    _this0.Kind = (::CppSharp::CppParser::ParserResultKind)(*this).Kind;
    if ((*this).ASTContext != nullptr)
        _this0.ASTContext = (::CppSharp::CppParser::AST::ASTContext*)(*this).ASTContext->NativePtr;
    if ((*this).Library != nullptr)
        _this0.Library = (::CppSharp::CppParser::AST::NativeLibrary*)(*this).Library->NativePtr;
    auto __ret = _this0.getDiagnosticsCount();
    __Kind = (CppSharp::Parser::ParserResultKind)_this0.Kind;
    __ASTContext = (_this0.ASTContext == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::ASTContext((::CppSharp::CppParser::AST::ASTContext*)_this0.ASTContext);
    __Library = (_this0.Library == nullptr) ? nullptr : gcnew CppSharp::Parser::AST::NativeLibrary((::CppSharp::CppParser::AST::NativeLibrary*)_this0.Library);
    return __ret;
}

CppSharp::Parser::ParserResultKind CppSharp::Parser::ParserResult::Kind::get()
{
    return __Kind;
}

void CppSharp::Parser::ParserResult::Kind::set(CppSharp::Parser::ParserResultKind value)
{
    __Kind = value;
}

CppSharp::Parser::AST::ASTContext^ CppSharp::Parser::ParserResult::ASTContext::get()
{
    return __ASTContext;
}

void CppSharp::Parser::ParserResult::ASTContext::set(CppSharp::Parser::AST::ASTContext^ value)
{
    __ASTContext = value;
}

CppSharp::Parser::AST::NativeLibrary^ CppSharp::Parser::ParserResult::Library::get()
{
    return __Library;
}

void CppSharp::Parser::ParserResult::Library::set(CppSharp::Parser::AST::NativeLibrary^ value)
{
    __Library = value;
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

CppSharp::Parser::ParserResult CppSharp::Parser::ClangParser::ParseHeader(CppSharp::Parser::ParserOptions^ Opts)
{
    auto arg0 = (::CppSharp::CppParser::ParserOptions*)Opts->NativePtr;
    auto __ret = ::CppSharp::CppParser::ClangParser::ParseHeader(arg0);
    return CppSharp::Parser::ParserResult((::CppSharp::CppParser::ParserResult*)__ret);
}

CppSharp::Parser::ParserResult CppSharp::Parser::ClangParser::ParseLibrary(CppSharp::Parser::ParserOptions^ Opts)
{
    auto arg0 = (::CppSharp::CppParser::ParserOptions*)Opts->NativePtr;
    auto __ret = ::CppSharp::CppParser::ClangParser::ParseLibrary(arg0);
    return CppSharp::Parser::ParserResult((::CppSharp::CppParser::ParserResult*)__ret);
}

CppSharp::Parser::ParserTargetInfo^ CppSharp::Parser::ClangParser::GetTargetInfo(CppSharp::Parser::ParserOptions^ Opts)
{
    auto arg0 = (::CppSharp::CppParser::ParserOptions*)Opts->NativePtr;
    auto __ret = ::CppSharp::CppParser::ClangParser::GetTargetInfo(arg0);
    if (__ret == nullptr) return nullptr;
    return (__ret == nullptr) ? nullptr : gcnew CppSharp::Parser::ParserTargetInfo((::CppSharp::CppParser::ParserTargetInfo*)__ret);
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
